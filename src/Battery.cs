using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace LaserGun
{
    // Battery firing for the Laser Rifle, plus the battery pack drawn on the gun.
    // The game keeps the loaded battery's charge on the weapon as the "WeaponBatteryDurability" property (0-100).
    // Its own CanFire refuses the rifle even with a full battery, so the rifle is allowed to fire while that
    // charge is above zero and it isn't reloading, and every shot takes 100 / ShotsPerBattery percent.
    internal static class Battery
    {
        const string Key = "WeaponBatteryDurability";

        internal static bool TryGet(ItemData d, out float pct)
        {
            pct = 0f;
            var p = d?.itemPropertyPairs;
            if (p == null) return false;
            for (int i = 0; i < p.Count; i++)
            {
                var kv = p[i];
                if (kv != null && kv.key == Key)
                    return float.TryParse(kv.value, NumberStyles.Float, CultureInfo.InvariantCulture, out pct);
            }
            return false;
        }

        static void Set(ItemData d, float pct)
        {
            var p = d?.itemPropertyPairs;
            if (p == null) return;
            for (int i = 0; i < p.Count; i++)
            {
                var kv = p[i];
                if (kv != null && kv.key == Key) { kv.value = pct.ToString("0.###", CultureInfo.InvariantCulture); return; }
            }
        }

        static bool Mine(BasicRangedWeapon w)
        {
            var a = w?.weaponAttr;
            return a != null && a.feedingType == ItemAttr_RangedWeapon.FeedingType.Battery && Weapons.IsLaser(a.itemId);
        }

        internal static void CanFire(BasicRangedWeapon w, ref bool result)
        {
            if (result || !Settings.Enabled.Value || !Mine(w)) return;
            if (w.isReloading) return;
            if (TryGet(w.weaponData, out var pct) && pct > 0.01f) result = true;
        }

        // Fire prefix/postfix: if the game itself didn't take charge for this shot, take it here.
        internal static float Before(BasicRangedWeapon w)
        {
            if (!Mine(w)) return -1f;
            return TryGet(w.weaponData, out var pct) ? pct : -1f;
        }

        internal static void After(BasicRangedWeapon w, float before)
        {
            if (before < 0f || !Mine(w)) return;
            var d = w.weaponData;
            if (!TryGet(d, out var now) || now < before - 0.0001f) return;
            float per = 100f / Math.Max(1, Settings.ShotsPerBattery.Value);
            Set(d, Math.Max(0f, before - per));
            if (Settings.VerboseLog.Value) PLog.Debug($"Battery {before:0.##}% -> {Math.Max(0f, before - per):0.##}%");
        }

        // ---- the pack on the gun
        static Sprite _sprite;
        static bool _spriteFailed;
        const string ChildName = "LaserGun_Pack";
        // Where the pack sits, in rifle sprite pixels from the sprite's centre (ppu 230): under the body, at the slot.
        static readonly Vector2 PackCentrePx = new Vector2(-38.5f, -36f);
        const float PackWidthPx = 26f;

        static Sprite PackSprite()
        {
            if (_sprite != null || _spriteFailed) return _sprite;
            try
            {
                using var s = typeof(Battery).Assembly.GetManifestResourceStream("LaserGun.pack.png");
                if (s == null) { _spriteFailed = true; PLog.Warn("Pack sprite resource missing"); return null; }
                var ms = new MemoryStream(); s.CopyTo(ms);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(tex, ms.ToArray())) { _spriteFailed = true; return null; }
                tex.filterMode = FilterMode.Bilinear;
                tex.hideFlags = HideFlags.HideAndDontSave;
                var bb = new Rect(8, 2, 112, 140); // opaque part of the 128x144 canvas
                float ppu = 230f * bb.width / PackWidthPx;
                _sprite = Sprite.Create(tex, new Rect(bb.x, tex.height - bb.y - bb.height, bb.width, bb.height), new Vector2(0.5f, 0.5f), ppu);
                _sprite.hideFlags = HideFlags.HideAndDontSave;
            }
            catch (Exception e) { _spriteFailed = true; PLog.Warn("Pack sprite failed: " + e.Message); }
            return _sprite;
        }

        internal static void Draw(BasicRangedWeapon w)
        {
            if (!Settings.ShowPack.Value || !Mine(w)) return;
            var body = w.weaponSpriteRenderer;
            if (body == null) return;
            var t = body.transform.Find(ChildName);
            SpriteRenderer sr = t != null ? t.GetComponent<SpriteRenderer>() : null;
            bool loaded = TryGet(w.weaponData, out var pct) && pct > 0.01f && !w.isReloading;
            if (sr == null)
            {
                if (!loaded) return;
                var sp = PackSprite(); if (sp == null) return;
                var go = new GameObject(ChildName);
                go.transform.SetParent(body.transform, false);
                sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
            }
            sr.enabled = loaded && body.enabled;
            if (!sr.enabled) return;
            sr.sortingLayerID = body.sortingLayerID;
            sr.sortingOrder = body.sortingOrder - 1;
            sr.flipX = body.flipX; sr.flipY = body.flipY;
            sr.color = body.color;
            var c = PackCentrePx / 230f;
            sr.transform.localPosition = new Vector3(body.flipX ? -c.x : c.x, body.flipY ? -c.y : c.y, 0f);
            sr.transform.localRotation = Quaternion.identity;
            sr.transform.localScale = Vector3.one;
        }
    }
}

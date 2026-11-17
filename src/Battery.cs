using System;
using System.IO;
using UnityEngine;

namespace LaserGun
{
    // Battery firing for the Laser Rifle, plus the battery pack drawn on the gun.
    internal static class Battery
    {
        const string Key = "WeaponBatteryDurability";
        const string CapKey = "LaserGunBatteryWh";

        // How the game's own battery weapons (Mining Laser, Tesla Gun) keep a loaded battery, read from the game code
        // (ItemAttr_RangedWeapon.SwapBatteryIntoWeapon): on the weapon's ItemData.properties float array,
        //   [1] = the loaded battery's item id, [2] = its remaining energy in Wh, [3] = loaded rounds (0 for them).
        // "WeaponBatteryDurability" only carries the battery's wear and is left alone.
        // Their beam code drains [2] over time. The rifle fires single shots instead, so each shot takes
        // EnergyPerShot Wh from [2], and [3] is kept at "shots left" so the game's normal firing code runs.
        const int IdSlot = 1, EnergySlot = 2, AmmoSlot = 3;
        static float _lastShot = -10f;

        internal static bool IsMine(BasicRangedWeapon w) => Settings.Enabled.Value && Mine(w);

        static bool Mine(BasicRangedWeapon w)
        {
            var a = w?.weaponAttr;
            return a != null && a.feedingType == ItemAttr_RangedWeapon.FeedingType.Battery && Weapons.IsLaser(a.itemId);
        }

        static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<float> Props(ItemData d)
        {
            var p = d?.properties;
            return p != null && p.Length > AmmoSlot ? p : null;
        }

        internal static bool HasBattery(ItemData weapon) { var p = Props(weapon); return p != null && p[IdSlot] >= 1f; }
        internal static float Energy(ItemData weapon) { var p = Props(weapon); return p != null && p[IdSlot] >= 1f ? p[EnergySlot] : 0f; }

        static int ShotsLeft(float energy) => (int)Math.Floor((energy + 0.001f) / Math.Max(0.1f, Settings.EnergyPerShot.Value));

        // Keeps "loaded rounds" equal to the shots the battery still has.
        internal static void Sync(ItemData weapon)
        {
            var p = Props(weapon); if (p == null) return;
            float want = p[IdSlot] >= 1f ? ShotsLeft(p[EnergySlot]) : 0f;
            if (p[AmmoSlot] != want) p[AmmoSlot] = want;
        }

        internal static void Swapped(ItemData weapon)
        {
            if (weapon == null || !Weapons.IsLaser(weapon.itemId)) return;
            Sync(weapon);
            var p = Props(weapon);
            if (p != null) PLog.Info($"Battery loaded: item {p[IdSlot]:0}, {p[EnergySlot]:0.#} Wh, {p[AmmoSlot]:0} shots");
        }

        // The game is about to hand back "loaded ammo" from the rifle: it has none, only battery energy.
        internal static void BeforeClearAmmo(ItemData weapon)
        {
            if (weapon == null || !Weapons.IsLaser(weapon.itemId)) return;
            var p = Props(weapon); if (p != null) p[AmmoSlot] = 0f;
        }

        internal static void CanFire(BasicRangedWeapon w, ref bool result)
        {
            if (!Settings.Enabled.Value || !Mine(w)) return;
            var d = w.weaponData;
            Sync(d);
            // The game doesn't hold battery-fed cartridge fire to the weapon's fire interval (it fired every frame),
            // so the pause between shots is enforced here.
            if (!Ready()) { result = false; return; }
            if (result || w.isReloading) return;
            if (ShotsLeft(Energy(d)) >= 1) result = true;
        }

        // Fire prefix/postfix: one shot = EnergyPerShot Wh.
        // ---- the local player's rifle, checked every frame (LaserHost.LateUpdate)
        static IntPtr _heldPtr; static float _heldId = -1f, _heldEnergy = -1f;
        static float _reloadStart = -10f, _reloadEnd = -10f; static bool _reloadEndSound;
        const float SwapTime = 0.9f;
        internal static Vector3 Muzzle; internal static int MuzzleFrame = -1;

        internal static void Frame()
        {
            var gc = GameController.instance; if (gc == null) return;
            var pc = gc.playerCharacter; if (pc == null) return;
            var w = pc.rangedWeapon;
            if (w == null || !Mine(w)) { _heldPtr = IntPtr.Zero; return; }
            var d = w.weaponData; var p = Props(d); if (p == null) return;
            if (d.Pointer != _heldPtr) { _heldPtr = d.Pointer; _heldId = p[IdSlot]; _heldEnergy = p[EnergySlot]; }
            else if (p[IdSlot] >= 1f && (_heldId < 1f || p[EnergySlot] > _heldEnergy + 1f))
            {
                // a battery just went in: short swap pause with the reload sounds, pack pops in at the end
                _reloadStart = Time.time; _reloadEnd = Time.time + SwapTime; _reloadEndSound = true;
                try { w.PlayReloadStartSoundOnly(); } catch { }
                PLog.Info($"Battery loaded: item {p[IdSlot]:0}, {p[EnergySlot]:0.#} Wh");
            }
            _heldId = p[IdSlot]; _heldEnergy = p[EnergySlot];
            if (_reloadEndSound && Time.time >= _reloadEnd) { _reloadEndSound = false; try { w.PlayReloadFinishSoundOnly(); } catch { } }
            Sync(d);
            Draw(w);
        }

        static bool Swapping => Time.time < _reloadEnd;

        static bool Ready() => !Swapping && UnityEngine.Time.time - _lastShot >= 60f / Math.Max(1f, Settings.FireRate.Value);

        // Fire prefix: false skips a shot that comes before the pause is over or with no energy left.
        internal static bool Allow(BasicRangedWeapon w)
        {
            if (!Settings.Enabled.Value || !Mine(w)) return true;
            return Ready() && ShotsLeft(Energy(w.weaponData)) >= 1;
        }

        internal static float Before(BasicRangedWeapon w)
        {
            if (!Mine(w)) return -1f;
            _lastShot = UnityEngine.Time.time;
            try
            {
                if (MuzzleOf(w, out var m)) { Muzzle = m; MuzzleFrame = Time.frameCount; }
                else { var gp = w.gunPoint; if (gp != null) { Muzzle = gp.position; MuzzleFrame = Time.frameCount; } }
            }
            catch { }
            var d = w.weaponData;
            return HasBattery(d) ? Energy(d) : -1f;
        }

        internal static void After(BasicRangedWeapon w, float before)
        {
            if (before < 0f || !Mine(w)) return;
            var d = w.weaponData; var p = Props(d); if (p == null) return;
            float after = Math.Max(0f, before - Settings.EnergyPerShot.Value);
            p[EnergySlot] = after;
            Sync(d);
            if (Settings.VerboseLog.Value) PLog.Debug($"Battery {before:0.#} Wh -> {after:0.#} Wh ({p[AmmoSlot]:0} shots left)");
        }

        // ---- the pack on the gun
        static Sprite _sprite;
        static bool _spriteFailed;
        const string ChildName = "LaserGun_Pack";
        // Where the pack sits, in rifle sprite pixels from the sprite's centre (ppu 230): under the body, at the slot.
        static readonly Vector2 PackCentrePx = new Vector2(-38.5f, -38f);
        const float PackWidthPx = 26f;

        // Rifle sprite geometry in its own pixels (Main region 299x118, see tools/build_items.py), measured from the
        // region's centre, y up. The game builds the weapon sprite at its own pixels-per-unit, so everything here is
        // converted with the live sprite's pixelsPerUnit instead of assuming the mod.json value.
        static readonly Vector2 MuzzlePx = new Vector2(119f, 10.5f);
        static Texture2D _tex; static float _spritePpu;

        static Sprite PackSprite(float gunPpu)
        {
            float ppu = gunPpu * 112f / PackWidthPx;
            if (_sprite != null && Mathf.Abs(_spritePpu - ppu) < 0.01f) return _sprite;
            if (_spriteFailed) return null;
            try
            {
                if (_tex == null)
                {
                    using var s = typeof(Battery).Assembly.GetManifestResourceStream("LaserGun.pack.png");
                    if (s == null) { _spriteFailed = true; PLog.Warn("Pack sprite resource missing"); return null; }
                    var ms = new MemoryStream(); s.CopyTo(ms);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!ImageConversion.LoadImage(tex, ms.ToArray())) { _spriteFailed = true; return null; }
                    tex.filterMode = FilterMode.Bilinear;
                    tex.hideFlags = HideFlags.HideAndDontSave;
                    _tex = tex;
                }
                var bb = new Rect(8, 2, 112, 140); // opaque part of the 128x144 canvas
                _sprite = Sprite.Create(_tex, new Rect(bb.x, _tex.height - bb.y - bb.height, bb.width, bb.height), new Vector2(0.5f, 0.5f), ppu);
                _sprite.hideFlags = HideFlags.HideAndDontSave;
                _spritePpu = ppu;
                PLog.Info($"Pack sprite built for a rifle sprite at {gunPpu:0.#} px/unit");
            }
            catch (Exception e) { _spriteFailed = true; PLog.Warn("Pack sprite failed: " + e.Message); }
            return _sprite;
        }

        // A point given in rifle sprite pixels -> local position on the weapon sprite renderer.
        static bool Local(SpriteRenderer body, Vector2 px, out Vector3 local, out float ppu)
        {
            local = Vector3.zero; ppu = 230f;
            var sp = body != null ? body.sprite : null; if (sp == null) return false;
            ppu = sp.pixelsPerUnit; if (ppu < 1f) return false;
            // if the game made the sprite at another size than the 299 px region, scale with it
            float k = sp.rect.width > 1f ? sp.rect.width / 299f : 1f;
            var c = sp.bounds.center;
            float x = px.x * k / ppu, y = px.y * k / ppu;
            local = new Vector3(c.x + (body.flipX ? -x : x), c.y + (body.flipY ? -y : y), 0f);
            ppu /= k;
            return true;
        }

        internal static bool MuzzleOf(BasicRangedWeapon w, out Vector3 world)
        {
            world = Vector3.zero;
            var body = w.weaponSpriteRenderer;
            if (!Local(body, MuzzlePx + new Vector2(Settings.MuzzleX.Value, Settings.MuzzleY.Value), out var l, out _)) return false;
            world = body.transform.TransformPoint(l);
            return true;
        }

        internal static void Draw(BasicRangedWeapon w)
        {
            if (!Settings.ShowPack.Value || !Mine(w)) return;
            var body = w.weaponSpriteRenderer;
            if (body == null) return;
            var t = body.transform.Find(ChildName);
            SpriteRenderer sr = t != null ? t.GetComponent<SpriteRenderer>() : null;
            var wd = w.weaponData;
            Sync(wd);
            bool loaded = HasBattery(wd) && !w.isReloading && !(Swapping && wd.Pointer == _heldPtr);
            // The game also draws the loaded battery itself as a "magazine", oversized and off the gun: hide that one.
            try { var gm = w.magazineSpriteRenderer; if (gm != null && gm.enabled) gm.enabled = false; } catch { }
            if (!Local(body, PackCentrePx + new Vector2(Settings.PackX.Value, Settings.PackY.Value), out var pos, out var gunPpu)) return;
            if (sr == null)
            {
                if (!loaded) return;
                var go = new GameObject(ChildName);
                go.transform.SetParent(body.transform, false);
                sr = go.AddComponent<SpriteRenderer>();
                try { sr.sharedMaterial = body.sharedMaterial; } catch { }
            }
            sr.enabled = loaded && body.enabled;
            if (!sr.enabled) return;
            var sp = PackSprite(gunPpu); if (sp == null) { sr.enabled = false; return; }
            if (sr.sprite != sp) sr.sprite = sp;
            sr.sortingLayerID = body.sortingLayerID;
            sr.sortingOrder = body.sortingOrder - 1; // behind the gun body, so the well covers the top of the pack
            sr.flipX = body.flipX; sr.flipY = body.flipY;
            sr.color = body.color;
            sr.transform.localPosition = pos;
            sr.transform.localRotation = Quaternion.identity;
            float ps = Settings.PackSize.Value; sr.transform.localScale = new Vector3(ps, ps, 1f);
        }
    }
}

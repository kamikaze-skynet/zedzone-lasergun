using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace LaserGun
{
    // Inventory pictures: while a battery pack is loaded, the rifle's picture is swapped for one with the pack
    // fitted (Resources/rifle_loaded.png, the same 299x118 box as the bare rifle so nothing moves or resizes).
    // The swap uses Image.overrideSprite, so the game's own picture stays in place underneath and comes back
    // by itself when the pack is taken out.
    static class PackIcon
    {
        static readonly List<BasicItemUI> _tracked = new List<BasicItemUI>();
        static Sprite _loaded;
        static bool _failed, _warned, _logged, _diag;
        static float _nextScan;

        static Sprite Loaded()
        {
            if (_loaded != null) return _loaded;
            if (_failed) return null;
            try
            {
                using var s = typeof(PackIcon).Assembly.GetManifestResourceStream("LaserGun.rifle_loaded.png");
                if (s == null) { _failed = true; PLog.Warn("Loaded-rifle picture missing"); return null; }
                var ms = new MemoryStream(); s.CopyTo(ms);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(tex, ms.ToArray())) { _failed = true; return null; }
                tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Clamp;
                tex.hideFlags = HideFlags.HideAndDontSave;
                _loaded = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 230f);
                _loaded.hideFlags = HideFlags.HideAndDontSave;
            }
            catch (Exception e) { _failed = true; PLog.Warn("Loaded-rifle picture failed: " + e.Message); }
            return _loaded;
        }

        // Puts the loaded picture on (or takes it off) one image showing the given item.
        internal static void Apply(Image icon, ItemData d, string where)
        {
            if (icon == null) return;
            var ld = Loaded(); if (ld == null) return;
            bool want = d != null && Weapons.IsLaser(d.itemId) && Settings.ShowPack.Value && Battery.HasBattery(d);
            var cur = icon.overrideSprite; // returns the normal sprite when no override is set
            bool has = cur != null && cur.Pointer == ld.Pointer;
            if (want && !has)
            {
                var gs = icon.sprite;
                if (!_logged)
                {
                    _logged = true;
                    PLog.Info($"Inventory picture swapped to the loaded rifle ({where}); the game's picture is {(gs == null ? "none" : $"{gs.rect.width:0}x{gs.rect.height:0}")}, ours {ld.rect.width:0}x{ld.rect.height:0}");
                }
                icon.overrideSprite = ld;
            }
            else if (!want && has) icon.overrideSprite = null;
        }

        internal static void Refreshed(BasicItemUI ui)
        {
            try
            {
                if (ui == null) return;
                var d = ui.itemdata;
                if (d == null || !Weapons.IsLaser(d.itemId)) return;
                bool known = false;
                for (int i = 0; i < _tracked.Count; i++) if (_tracked[i].Pointer == ui.Pointer) { known = true; break; }
                if (!known) _tracked.Add(ui);
                Apply(ui.iconImage, d, "bag slot");
            }
            catch (Exception e) { Warn(e); }
        }

        // The game fills item pictures in from a background task that does not go through the refresh calls,
        // so open item slots are also looked over once a second.
        static void Scan()
        {
            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + 1f;
            var all = UnityEngine.Object.FindObjectsOfType<BasicItemUI>();
            if (all == null) return;
            for (int k = 0; k < all.Length; k++)
            {
                var ui = all[k]; if (ui == null) continue;
                var d = ui.itemdata;
                if (d == null || !Weapons.IsLaser(d.itemId)) continue;
                bool known = false;
                for (int i = 0; i < _tracked.Count; i++) if (_tracked[i].Pointer == ui.Pointer) { known = true; break; }
                if (!known) _tracked.Add(ui);
                if (!_diag) { _diag = true; PLog.Info("Inventory: rifle found in a bag slot"); }
            }
        }

        internal static void Tick()
        {
            try { Scan(); } catch (Exception e) { Warn(e); }
            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                try
                {
                    var ui = _tracked[i];
                    if (ui == null) { _tracked.RemoveAt(i); continue; }
                    var d = ui.itemdata;
                    Apply(ui.iconImage, d, "bag slot");
                    if (d == null || !Weapons.IsLaser(d.itemId)) _tracked.RemoveAt(i); // slot reused for another item
                }
                catch (Exception e) { _tracked.RemoveAt(i); Warn(e); }
            }
        }

        // The weapon slots on the character screen are their own kind of slot.
        internal static void WeaponSlot(CharacterWeaponSlotUI slot)
        {
            try { if (slot != null) Apply(slot.weaponIcon, slot.currentWeaponData, "weapon slot"); }
            catch (Exception e) { Warn(e); }
        }

        static void Warn(Exception e) { if (_warned) return; _warned = true; PLog.Warn("Inventory picture failed: " + e); }
    }
}

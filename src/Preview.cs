using System;

namespace LaserGun
{
    // The game's mod editor previews an item under a temporary item id and with the game's own ammo rules,
    // which know nothing about the rifle's battery pack. While the editor is previewing the Laser Rifle, the
    // temporary id counts as the rifle and a full pack is put in whenever the preview loads or reloads.
    static class Preview
    {
        static bool _warned;

        internal static void Setup(PreviewPanel panel, BaseModData data)
        {
            try
            {
                string guid = data != null ? data.modGuid : null;
                bool ours = false;
                if (!string.IsNullOrEmpty(guid))
                    foreach (var raw in Settings.WeaponModGuids.Value.Split(','))
                        if (string.Equals(raw.Trim(), guid, StringComparison.OrdinalIgnoreCase)) ours = true;
                Weapons.PreviewOurs = ours;
                if (ours) { PLog.Info("Mod editor is previewing the Laser Rifle"); Load(panel, "preview opened"); }
            }
            catch (Exception e) { Warn(e); }
        }

        internal static void Closed() { Weapons.PreviewOurs = false; }

        internal static void Load(PreviewPanel panel, string why)
        {
            try
            {
                if (!Weapons.PreviewOurs || panel == null) return;
                var ch = panel.currentCharacter;
                var w = ch != null ? ch.rangedWeapon : null;
                var d = w != null ? w.weaponData : null;
                if (d == null) { PLog.Info($"Preview ({why}): no weapon in hand yet"); return; }
                if (!Battery.Fill(d, Power.PackId(), Settings.PackCapacity.Value))
                    PLog.Info($"Preview ({why}): the preview weapon has no battery slots (item {d.itemId})");
                else PLog.Info($"Preview ({why}): full battery pack loaded (item {d.itemId})");
            }
            catch (Exception e) { Warn(e); }
        }

        static void Warn(Exception e) { if (_warned) return; _warned = true; PLog.Warn("Mod editor preview support failed: " + e); }
    }
}

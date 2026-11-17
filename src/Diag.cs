using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LaserGun
{
    // Battery diagnostics: why a loaded Laser Rifle won't fire. Logs (at most every 2 s) what the game sees
    // when it asks the rifle CanFire(), and lists the game's own battery weapons at start-up for comparison.
    internal static class Diag
    {
        static float _next;
        static bool _last = true;

        internal static void CanFire(BasicRangedWeapon w, bool result)
        {
            if (w == null) return;
            var attr = w.weaponAttr;
            if (attr == null || !Weapons.IsLaser(attr.itemId)) return;
            if (result == _last && Time.unscaledTime < _next) return;
            _last = result; _next = Time.unscaledTime + 2f;
            var sb = new StringBuilder($"CanFire={result}");
            try
            {
                var d = w.weaponData;
                if (d != null)
                {
                    sb.Append($" | loaded ammo {BasicRangedWeapon.GetLoadedAmmo(d)} | weapon durability {d.durability:0.###}");
                    sb.Append(" | props " + Props(d));
                    var inv = d.inventoryData;
                    var items = inv?.itemList;
                    if (items != null)
                        for (int i = 0; i < items.Count; i++)
                        {
                            var it = items[i]; if (it == null) continue;
                            float e = -1f; try { e = ItemData.GetBatteryEnergy(it); } catch { }
                            sb.Append($" | inside: item {it.itemId} x{it.itemNumberFloat:0.##} dur {it.durability:0.###} energy {e:0.##} props {Props(it)}");
                        }
                }
                sb.Append($" | energy/s {attr.batteryEnergyPerSec:0.###} feeding {attr.feedingType} ammoId {attr.ammoId} mag {attr.magazineSize} action {attr.gunActionType}");
            }
            catch (Exception e) { sb.Append(" | read failed: " + e.Message); }
            PLog.Info(sb.ToString());
        }

        static string Props(ItemData d)
        {
            try
            {
                var p = d.itemPropertyPairs;
                if (p == null || p.Count == 0) return "-";
                var parts = new List<string>();
                for (int i = 0; i < p.Count; i++) { var kv = p[i]; if (kv != null) parts.Add(kv.key + "=" + kv.value); }
                return string.Join(", ", parts);
            }
            catch { return "?"; }
        }

        internal static void ListBatteryWeapons(ItemManager im)
        {
            var list = im?.itemList; if (list == null) return;
            var parts = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                var g = list[i]?.TryCast<ItemAttr_RangedWeapon>();
                if (g == null || g.feedingType != ItemAttr_RangedWeapon.FeedingType.Battery || Weapons.IsLaser(g.itemId)) continue;
                parts.Add($"{g.itemId}={g.itemName} energy/s {g.batteryEnergyPerSec:0.###} interval {g.fireInterval:0.###} ammoId {g.ammoId} mag {g.magazineSize} action {g.gunActionType} type {g.weaponType} mech {g.isMechWeapon}");
            }
            PLog.Info("Game battery weapons: " + (parts.Count > 0 ? string.Join(" ; ", parts) : "none"));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace LaserGun
{
    // After the mod loader runs: Laser Charges are crafted from the game's own battery if it has one
    // (1 battery -> 25 charges). Found by name, or set General / BatteryItemId in the config.
    internal static class Recipes
    {
        const string CellGuid = "b0286f7b-0596-4c17-a7ed-aab63338872a";
        const string BatteryGuid = "cef9bff0-579f-401b-a5a2-890dc6fd3ec8";

        internal static int IdOf(string guid)
        {
            try
            {
                var mods = ModLoader.loadedMods;
                if (mods != null && mods.ContainsKey(guid)) { var m = mods[guid]; if (m != null) return m.resolvedRuntimeId; }
            }
            catch { }
            return -1;
        }

        internal static void Check()
        {
            var im = ItemManager.instance;
            if (im == null) return;
            int cell = IdOf(CellGuid), ours = IdOf(BatteryGuid);
            var sb = new StringBuilder($"Items: Laser Rifle {IdOf("93b81951-6391-4707-beb4-0ea971889dc7")}, Laser Battery {ours}, Laser Charge {cell}");
            if (cell <= 0) { PLog.Warn(sb + " - the Laser Charge item isn't loaded."); return; }

            int battery = Settings.BatteryItemId.Value > 0 ? Settings.BatteryItemId.Value : FindBattery(im, ours);
            var attr = im.GetItemAttrById(cell);
            if (battery > 0 && attr != null && im.GetItemAttrById(battery) != null)
            {
                var recipes = attr.recipeDataAry;
                if (recipes != null && recipes.Length > 0 && recipes[0]?.recipeItems != null)
                {
                    var items = recipes[0].recipeItems;
                    while (items.Count > 1) items.RemoveAt(items.Count - 1);
                    if (items.Count == 1) { items[0].itemId = battery; items[0].itemNumber = 1f; }
                    sb.Append($" | charge recipe: 1 x {Name(im.GetItemAttrById(battery))} ({battery}) -> {recipes[0].outputItemNumber}");
                }
            }
            else sb.Append(" | no game battery found: charges use the fallback recipe (set General / BatteryItemId to change it)");
            PLog.Info(sb.ToString());
        }

        static string Name(ItemAttr a)
        {
            if (a == null) return "";
            try { var n = a.ItemName_EN; if (!string.IsNullOrEmpty(n)) return n; } catch { }
            try { var n = a.itemName; if (!string.IsNullOrEmpty(n)) return n; } catch { }
            try { return a.ItemName ?? ""; } catch { return ""; }
        }

        // The game's battery: an item named exactly "Battery", else the shortest name containing "battery"
        // (not our own Laser Battery), preferring crafting materials.
        static int FindBattery(ItemManager im, int ours)
        {
            var list = im.itemList;
            if (list == null) return -1;
            var mats = new HashSet<int>();
            try { var m = im.materialList; if (m != null) for (int i = 0; i < m.Count; i++) if (m[i] != null) mats.Add(m[i].itemId); } catch { }
            int best = -1, bestScore = int.MaxValue;
            var seen = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                if (a == null || a.itemId == ours) continue;
                string n = Name(a).Trim(), l = n.ToLowerInvariant();
                if (!l.Contains("batter") || l.Contains("laser")) continue;
                seen.Add($"{a.itemId}={n}");
                int score = l == "battery" ? 0 : l.Length + (l.StartsWith("battery") ? 0 : 20);
                if (!mats.Contains(a.itemId)) score += 50;
                if (score < bestScore) { bestScore = score; best = a.itemId; }
            }
            if (seen.Count > 0) PLog.Info("Battery items in the game: " + string.Join(", ", seen));
            return best;
        }
    }
}

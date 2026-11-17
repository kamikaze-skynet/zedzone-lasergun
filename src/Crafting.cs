using System;
using System.Collections.Generic;
using System.Text;

namespace LaserGun
{
    // Crafting recipes for the Laser Rifle and the Laser Battery Pack, set from [Crafting] in the config.
    // Ingredients are written by the game's English item names ("Copper Wire x8"), or by item id ("34 x8"),
    // and looked up in the game's item list at start-up, so they keep working when the game renumbers items.
    internal static class Crafting
    {
        const string RifleGuid = "93b81951-6391-4707-beb4-0ea971889dc7";
        const string PackGuid = "cef9bff0-579f-401b-a5a2-890dc6fd3ec8";

        internal static void Apply()
        {
            var im = ItemManager.instance;
            if (im == null) return;
            var names = Names(im);
            Set(im, names, Power.IdOf(RifleGuid), "Laser Rifle", Settings.RifleRecipe.Value);
            Set(im, names, Power.IdOf(PackGuid), "Laser Battery Pack", Settings.PackRecipe.Value);
        }

        static List<(int id, string name)> Names(ItemManager im)
        {
            var r = new List<(int, string)>();
            var list = im.itemList;
            if (list == null) return r;
            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i]; if (a == null) continue;
                string n = null;
                try { n = a.ItemName_EN; } catch { }
                if (string.IsNullOrWhiteSpace(n)) try { n = a.itemName; } catch { }
                if (string.IsNullOrWhiteSpace(n)) try { n = a.ItemName; } catch { }
                if (!string.IsNullOrWhiteSpace(n)) r.Add((a.itemId, n.Replace("\n", " ").Trim()));
            }
            return r;
        }

        static string Norm(string s)
        {
            var sb = new StringBuilder();
            foreach (char ch in s.ToLowerInvariant()) if (char.IsLetterOrDigit(ch) || ch == ' ') sb.Append(ch);
            var words = sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                var w = words[i];
                if (w.Length > 3 && w.EndsWith("es") && (w.EndsWith("ches") || w.EndsWith("shes") || w.EndsWith("xes"))) w = w.Substring(0, w.Length - 2);
                else if (w.Length > 3 && w.EndsWith("s") && !w.EndsWith("ss")) w = w.Substring(0, w.Length - 1);
                words[i] = w;
            }
            return string.Join(" ", words);
        }

        // Exact name (ignoring case and plurals) first, else the shortest game item name that has every word.
        static int Find(List<(int id, string name)> names, string want, out string found, int self)
        {
            found = null;
            if (int.TryParse(want.Trim(), out int id)) { foreach (var n in names) if (n.id == id) { found = n.name; return id; } return -1; }
            string w = Norm(want);
            foreach (var n in names) if (n.id != self && Norm(n.name) == w) { found = n.name; return n.id; }
            var parts = w.Split(' ');
            int best = -1; int bestLen = int.MaxValue;
            foreach (var n in names)
            {
                if (n.id == self) continue;
                string l = " " + Norm(n.name) + " ";
                bool all = true;
                foreach (var p in parts) if (!l.Contains(" " + p + " ")) { all = false; break; }
                if (all && n.name.Length < bestLen) { best = n.id; bestLen = n.name.Length; found = n.name; }
            }
            return best;
        }

        static void Set(ItemManager im, List<(int id, string name)> names, int itemId, string label, string spec)
        {
            if (itemId <= 0 || string.IsNullOrWhiteSpace(spec)) return;
            var attr = im.GetItemAttrById(itemId);
            var recipes = attr?.recipeDataAry;
            if (recipes == null || recipes.Length == 0 || recipes[0] == null || recipes[0].recipeItems == null)
            { PLog.Warn($"Crafting: the {label} has no recipe to change."); return; }

            var want = new List<(int id, float n, string name)>();
            var missing = new List<string>();
            foreach (var raw in spec.Split(','))
            {
                var part = raw.Trim(); if (part.Length == 0) continue;
                float count = 1f; string item = part;
                int x = part.LastIndexOfAny(new[] { 'x', 'X', '*' });
                if (x > 0 && float.TryParse(part.Substring(x + 1).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var c))
                { count = c; item = part.Substring(0, x).Trim(); }
                int id = Find(names, item, out var found, itemId);
                if (id <= 0) missing.Add(item); else want.Add((id, count, found));
            }
            if (missing.Count > 0)
            {
                PLog.Warn($"Crafting: couldn't find {string.Join(", ", missing)} in the game for the {label}; its recipe is unchanged. Fix [Crafting] in the config (names as the game shows them, or item ids).");
                return;
            }

            var items = recipes[0].recipeItems;
            items.Clear();
            foreach (var w in want) { var r = new RecipeItemData(); r.itemId = w.id; r.itemNumber = w.n; items.Add(r); }
            recipes[0].outputItemNumber = 1;
            var sb = new StringBuilder($"Crafting: {label} = ");
            for (int i = 0; i < want.Count; i++) sb.Append(i > 0 ? ", " : "").Append($"{want[i].n:0.##} x {want[i].name} ({want[i].id})");
            sb.Append($" at {recipes[0].craftPlatform}");
            PLog.Info(sb.ToString());
        }
    }
}

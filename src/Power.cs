using System;
using System.Collections.Generic;
using System.Text;

namespace LaserGun
{
    // The Laser Rifle runs on the game's own batteries (weapon feedingType = Battery): R swaps in a
    // battery from your pack, the shots drain it, and it is recharged with the game's chargers.
    // The game drains batteryEnergyPerSec (Wh) for every second of firing, so after the mod loader
    // runs this sets it so one reference battery gives about [Battery] ShotsPerBattery shots.
    internal static class Power
    {
        const string RifleGuid = "93b81951-6391-4707-beb4-0ea971889dc7";

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

        static string Name(ItemAttr a)
        {
            if (a == null) return "";
            try { var n = a.ItemName_EN; if (!string.IsNullOrEmpty(n)) return n; } catch { }
            try { var n = a.itemName; if (!string.IsNullOrEmpty(n)) return n; } catch { }
            try { return a.ItemName ?? ""; } catch { return ""; }
        }

        static float Capacity(ItemAttr a, out string model)
        {
            model = "";
            try
            {
                var f = a.GetItemFeatureByFeatureType(ItemFeatureType.Battery)?.TryCast<ItemFeature_Battery>();
                if (f == null) return 0f;
                try { model = f.batteryModel ?? ""; } catch { }
                return f.batteryCapacityFloat;
            }
            catch { return 0f; }
        }

        internal static void Check()
        {
            var im = ItemManager.instance;
            if (im == null) return;
            int rifleId = IdOf(RifleGuid);
            var gun = rifleId > 0 ? im.GetItemAttrById(rifleId)?.TryCast<ItemAttr_RangedWeapon>() : null;
            if (gun == null) { PLog.Warn("Power: the Laser Rifle item isn't loaded."); return; }

            // Every battery the game knows, with its capacity.
            var caps = new List<(int id, string name, string model, float cap)>();
            var list = im.itemList;
            if (list != null)
                for (int i = 0; i < list.Count; i++)
                {
                    var a = list[i];
                    if (a == null) continue;
                    float cap = Capacity(a, out var model);
                    if (cap > 0f) caps.Add((a.itemId, Name(a), model, cap));
                }
            var sb = new StringBuilder($"Power: Laser Rifle {rifleId}, feeding {gun.feedingType}");
            if (caps.Count > 0)
            {
                var parts = new List<string>();
                foreach (var c in caps) parts.Add($"{c.id}={c.name} [{c.model}] {c.cap:0.##}Wh");
                PLog.Info("Batteries in the game: " + string.Join(", ", parts));
            }

            try { gun.feedingType = ItemAttr_RangedWeapon.FeedingType.Battery; } catch (Exception e) { PLog.Warn("Couldn't set battery feeding: " + e.Message); }

            // Reference battery: the configured one, else the smallest that isn't a vehicle battery.
            float refCap = 0f; string refName = "";
            int want = Settings.BatteryItemId.Value;
            foreach (var c in caps)
            {
                if (want > 0) { if (c.id == want) { refCap = c.cap; refName = c.name; } continue; }
                string l = (c.name + " " + c.model).ToLowerInvariant();
                if (l.Contains("car") || l.Contains("vehicle") || l.Contains("truck")) continue;
                if (refCap <= 0f || c.cap < refCap) { refCap = c.cap; refName = c.name; }
            }
            float interval = gun.fireInterval > 0.001f ? gun.fireInterval : 0.15f;
            int shots = Math.Max(1, Settings.ShotsPerBattery.Value);
            if (Settings.EnergyPerSecond.Value > 0f) gun.batteryEnergyPerSec = Settings.EnergyPerSecond.Value;
            else if (refCap > 0f) gun.batteryEnergyPerSec = refCap / (shots * interval);
            sb.Append($" | reference battery: {(refCap > 0 ? $"{refName} {refCap:0.##}Wh" : "none found")}");
            sb.Append($" | energy {gun.batteryEnergyPerSec:0.###} Wh/s of firing (~{shots} shots per reference battery at {interval:0.###}s/shot)");
            PLog.Info(sb.ToString());
        }
    }
}

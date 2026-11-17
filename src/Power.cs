using System;
using System.Collections.Generic;
using System.Text;

namespace LaserGun
{
    // The Laser Rifle runs on the game's own batteries (weapon feedingType = Battery): R swaps in a
    // battery from your pack, the shots drain it, and it is recharged with the game's chargers.
    // The game drains batteryEnergyPerSec (Wh) for every second of firing, so after the mod loader
    // runs this sets it so one full Laser Battery Pack gives about [Battery] ShotsPerBattery shots. The pack is
    // a Magazine item in the .zedmod; MakeBattery turns it into a real game battery with PackCapacity energy.
    internal static class Power
    {
        const string RifleGuid = "93b81951-6391-4707-beb4-0ea971889dc7";
        const string PackGuid = "cef9bff0-579f-401b-a5a2-890dc6fd3ec8";

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

        static readonly System.Text.RegularExpressions.Regex Num = new(@"[0-9]+(\.[0-9]+)?");

        // Gives `pack` the template battery's item type and features, with its own battery feature holding `capacity`.
        static float MakeBattery(ItemAttr pack, ItemAttr tmpl, int capacity)
        {
            var src = tmpl.GetItemFeatureByFeatureType(ItemFeatureType.Battery)?.TryCast<ItemFeature_Battery>();
            if (src == null) return 0f;
            var bat = UnityEngine.Object.Instantiate(src);
            bat.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
            string old = bat.batteryCapacity ?? "";
            bat.batteryCapacity = Num.IsMatch(old) ? Num.Replace(old, capacity.ToString(), 1) : capacity.ToString();

            pack.itemType = tmpl.itemType;
            var types = new Il2CppSystem.Collections.Generic.List<ItemFeatureType>();
            var temp = new Il2CppSystem.Collections.Generic.List<ItemFeature>();
            var dic = new Il2CppSystem.Collections.Generic.Dictionary<ItemFeatureType, ItemFeature>();
            var cfgs = new Il2CppSystem.Collections.Generic.List<ItemFeatureConfigData>();
            types.Add(ItemFeatureType.Battery); temp.Add(bat); dic[ItemFeatureType.Battery] = bat;
            var tc = tmpl.itemFeatureConfigDatas;
            if (tc != null)
                for (int i = 0; i < tc.Count; i++)
                {
                    var c = tc[i];
                    if (c == null || c.featureType != ItemFeatureType.Battery) continue;
                    var n = new ItemFeatureConfigData { featureType = c.featureType, itemFeatureClassName = c.itemFeatureClassName };
                    var fields = new Il2CppSystem.Collections.Generic.List<ItemFeatureConfigData.ItemFeatureFieldConfigData>();
                    var fs = c.ItemFeatureFieldConfigDatas;
                    if (fs != null)
                        for (int j = 0; j < fs.Count; j++)
                        {
                            var f = fs[j]; if (f == null) continue;
                            string v = f.itemFeatureFieldValue;
                            if (f.itemFeatureFieldName == "batteryCapacity") v = bat.batteryCapacity;
                            fields.Add(new ItemFeatureConfigData.ItemFeatureFieldConfigData { itemFeatureFieldName = f.itemFeatureFieldName, itemFeatureFieldValue = v });
                        }
                    n.ItemFeatureFieldConfigDatas = fields;
                    cfgs.Add(n);
                }
            pack.itemFeatures = types;
            pack.itemFeatureConfigDatas = cfgs;
            pack.itemFeatureTemp = temp;
            pack.itemFeatureDataDic = dic;
            try { pack.InvalidateDescriptionCache(); } catch { }
            return Capacity(pack, out _);
        }

        static bool IsWeaponBattery(ItemManager im, int id)
        {
            try
            {
                var a = im.GetItemAttrById(id);
                var d = a != null ? im.GetRandomDataByAttr(a, false, false) : null;
                return d != null && BasicItemUI.IsWeaponBattery(d);
            }
            catch { return false; }
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

            // Template: the smallest game battery the game itself lets you load into a battery weapon
            // (BasicItemUI.IsWeaponBattery); if none says yes, the smallest that isn't a vehicle battery.
            // The Laser Battery Pack copies its battery setup (type, feature, model) with PackCapacity energy.
            int packId = IdOf(PackGuid);
            int tmplId = -1; float tmplCap = 0f; bool tmplWeapon = false;
            var weaponOk = new List<string>();
            foreach (var c in caps)
            {
                if (c.id == packId) continue;
                string l = (c.name + " " + c.model).ToLowerInvariant();
                bool vehicle = l.Contains("car") || l.Contains("vehicle") || l.Contains("truck");
                bool weapon = IsWeaponBattery(im, c.id);
                if (weapon) weaponOk.Add(c.name);
                bool better = weapon && !tmplWeapon || (weapon == tmplWeapon && !vehicle && (tmplCap <= 0f || c.cap < tmplCap));
                if (better) { tmplCap = c.cap; tmplId = c.id; tmplWeapon = weapon; }
            }
            PLog.Info("Power: batteries the game loads into weapons: " + (weaponOk.Count > 0 ? string.Join(", ", weaponOk) : "none reported"));
            float packCap = 0f;
            var pack = packId > 0 ? im.GetItemAttrById(packId) : null;
            var tmpl = tmplId > 0 ? im.GetItemAttrById(tmplId) : null;
            if (pack == null) PLog.Warn("Power: the Laser Battery Pack item isn't loaded.");
            else if (tmpl == null) PLog.Warn("Power: no game battery found to copy, so the Laser Battery Pack can't hold power.");
            else
            {
                try { packCap = MakeBattery(pack, tmpl, Settings.PackCapacity.Value); }
                catch (Exception e) { PLog.Warn("Power: couldn't make the Laser Battery Pack a battery: " + e); }
                PLog.Info($"Power: Laser Battery Pack {packId} copies {Name(tmpl)} ({tmplId}), capacity {packCap:0.##} (type {pack.itemType}, weapon battery: {IsWeaponBattery(im, packId)}, hidden {pack.hiddenItem})");
            }

            // Reference battery for the drain: the configured one, else the Laser Battery Pack.
            float refCap = 0f; string refName = "";
            int want = Settings.BatteryItemId.Value;
            if (want > 0) { foreach (var c in caps) if (c.id == want) { refCap = c.cap; refName = c.name; } }
            if (refCap <= 0f && packCap > 0f) { refCap = packCap; refName = "Laser Battery Pack"; }
            if (refCap <= 0f && tmplCap > 0f) { refCap = tmplCap; refName = Name(tmpl); }
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

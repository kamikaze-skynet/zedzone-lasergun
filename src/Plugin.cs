using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace LaserGun
{
    [BepInPlugin(Guid, "Laser Gun", Version)]
    public class Plugin : BasePlugin
    {
        public const string Guid = "girthy_mods.zedzone.lasergun";
        public const string Version = "1.1.0";

        public override void Load()
        {
            Settings.Bind(Config);
            PLog.Verbose = Settings.VerboseLog.Value;
            PLog.Open(Log, Version);
            if (Settings.PendingResetNote != null) PLog.Info(Settings.PendingResetNote);

            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<LaserHost>();
                var go = new GameObject("LaserGun.Host");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.HideAndDontSave;
                go.AddComponent<LaserHost>();
            }
            catch (Exception e) { PLog.Error("Could not create the host object; the mod is off", e); return; }

            Patches.Apply(new Harmony(Guid));
            PLog.Info($"Laser Gun {Version} loaded.");
        }
    }

    // Hidden object that moves the beams and fades them out.
    public class LaserHost : MonoBehaviour
    {
        public LaserHost(IntPtr ptr) : base(ptr) { }

        static bool _warned;
        void LateUpdate()
        {
            try { Battery.Frame(); } catch (Exception e) { if (!_warned) { _warned = true; PLog.Warn("Battery frame failed: " + e); } }
            PackIcon.Tick();
            try { Beam.Tick(); }
            catch (Exception e) { PLog.Error("Beam update failed", e); Beam.Clear(); }
        }
    }
}

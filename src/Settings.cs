using BepInEx.Configuration;

namespace LaserGun
{
    // Everything tunable, in BepInEx\config\girthy_mods.zedzone.lasergun.cfg (also in Mod Menu, F11).
    internal static class Settings
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<string> WeaponModGuids, ExtraWeaponIds;
        internal static ConfigEntry<float> Damage, BeamSpeed;
        internal static ConfigEntry<float> IgniteChance, IgniteChanceSurfaces;
        internal static ConfigEntry<bool> IgniteGroundWithTarget;
        internal static ConfigEntry<bool> Beam;
        internal static ConfigEntry<float> BeamWidth, BeamFade;
        internal static ConfigEntry<string> BeamColor;
        internal static ConfigEntry<float> SoundVolume;
        internal static ConfigEntry<string> SoundFile;
        internal static ConfigEntry<int> BatteryItemId, ShotsPerBattery, PackCapacity;
        
        internal static ConfigEntry<bool> ShowPack, ShowPower;
        internal static ConfigEntry<float> EnergyPerShot, FireRate, TrailLinger, MuzzleX, MuzzleY, PackX, PackY, PackSize;
        internal static ConfigEntry<float> EnergyPerSecond;
        internal static ConfigEntry<bool> VerboseLog;

        static AcceptableValueRange<float> R(float a, float b) => new AcceptableValueRange<float>(a, b);

        internal static void Bind(ConfigFile c)
        {
            Enabled = c.Bind("General", "Enabled", true, "Turn the laser behaviour (beam, sound, fire) on or off. The items stay.");
            WeaponModGuids = c.Bind("General", "WeaponModGuids", "93b81951-6391-4707-beb4-0ea971889dc7",
                "Comma-separated modGuids of weapons that fire lasers. Default: this mod's Laser Rifle.");
            ExtraWeaponIds = c.Bind("General", "ExtraWeaponIds", "", "Extra item ids (comma-separated) that also fire lasers, e.g. a vanilla gun.");
            BatteryItemId = c.Bind("Battery", "ReferenceBatteryId", 0,
                "Item id of the battery that gives exactly ShotsPerBattery shots (bigger batteries give more). 0 = the Laser Battery Pack. LaserGun.log lists every battery with its id.");
            ShowPower = c.Bind("Battery", "ShowPowerOnHud", true, "Show the battery's power on the weapon HUD the way the game's own battery guns do, instead of a shot count. Turn off if the gun misbehaves. Applies at once.");
            EnergyPerShot = c.Bind("Battery", "EnergyPerShot", 40f, new ConfigDescription("Battery energy (Wh) one shot uses, taken from the loaded battery the same way the game's own battery guns drain it. A 2000 Wh Laser Battery Pack gives 2000 / this shots.", R(0.1f, 10000)));
            ShowPower.SettingChanged += (_, __) => Power.ApplyWeaponType();
            PackCapacity = c.Bind("Battery", "PackCapacity", 2000, new ConfigDescription("Energy a full Laser Battery Pack holds (same units as the game's batteries).", new AcceptableValueRange<int>(1, 100000)));
            ShotsPerBattery = c.Bind("Battery", "ShotsPerBattery", 50, new ConfigDescription("Shots from one full reference battery.", new AcceptableValueRange<int>(1, 1000)));
            EnergyPerSecond = c.Bind("Battery", "EnergyPerSecond", 0f, new ConfigDescription("Override: battery energy (Wh) used per second of firing. 0 = work it out from ShotsPerBattery.", R(0, 10000)));

            FireRate = c.Bind("Laser", "FireRate", 300f, new ConfigDescription("Shots per minute. Applies at once.", R(10, 1200)));
            FireRate.SettingChanged += (_, __) => Power.ApplyFireRate();
            Damage = c.Bind("Laser", "Damage", 350f, new ConfigDescription("Damage per shot. 0 keeps the weapon's own damage.", R(0, 20000)));
            BeamSpeed = c.Bind("Laser", "BeamSpeed", 1200f, new ConfigDescription("Speed (m/s) of the shot. Light is instant; this is fast enough to look instant on screen.", R(50, 5000)));
            IgniteChance = c.Bind("Fire", "IgniteChance", 0.15f, new ConfigDescription("Chance (0-1) that a hit sets the target on fire.", R(0, 1)));
            IgniteChanceSurfaces = c.Bind("Fire", "IgniteChanceSurfaces", 0.05f, new ConfigDescription("Chance (0-1) that a shot hitting the ground, a wall or an object starts a fire there.", R(0, 1)));

            IgniteGroundWithTarget = c.Bind("Fire", "IgniteGroundToo", false, "When a target catches fire, also light the ground under it (can spread through grass).");

            ShowPack = c.Bind("Visuals", "ShowBatteryPack", true, "Draw the battery pack under the rifle while a charged battery is loaded.");
            PackX = c.Bind("Visuals", "BatteryPackForward", 0f, new ConfigDescription("Moves the battery pack drawn on the gun forward (+) or back (-), in gun-sprite pixels. Applies at once.", R(-80, 80)));
            PackY = c.Bind("Visuals", "BatteryPackUp", 6f, new ConfigDescription("Moves the battery pack drawn on the gun up (+) or down (-), in gun-sprite pixels. Applies at once.", R(-40, 40)));
            PackSize = c.Bind("Visuals", "BatteryPackSize", 1f, new ConfigDescription("Size of the battery pack drawn on the gun (1 = normal). Applies at once.", R(0.3f, 3f)));
            MuzzleX = c.Bind("Visuals", "BeamStartForward", 0f, new ConfigDescription("Moves where the beam starts along the barrel, in gun-sprite pixels (+ forward, - back toward the player). Applies at once.", R(-150, 60)));
            MuzzleY = c.Bind("Visuals", "BeamStartUp", 0f, new ConfigDescription("Moves where the beam starts up (+) or down (-) on the gun, in gun-sprite pixels. Applies at once.", R(-40, 40)));
            TrailLinger = c.Bind("Visuals", "TrailLinger", 1.2f, new ConfigDescription("Seconds a glowing trail hangs in the air along each shot's path before it fades out. 0 = off.", R(0, 10)));
            Beam = c.Bind("Visuals", "Beam", true, "Draw a bright laser beam behind each shot.");
            BeamWidth = c.Bind("Visuals", "BeamWidth", 0.05f, new ConfigDescription("Width (m) of the beam.", R(0.005f, 0.5f)));
            BeamFade = c.Bind("Visuals", "BeamFade", 0.07f, new ConfigDescription("Seconds the beam stays on screen.", R(0.01f, 1)));
            BeamColor = c.Bind("Visuals", "BeamColor", "FF3A1E", "Beam colour as hex RRGGBB (red by default; try 3AFF5A for green or 3AA0FF for blue).");

            SoundVolume = c.Bind("Sound", "Volume", 0.9f, new ConfigDescription("Volume of the laser crack. 0 = only the game's quiet shot sound.", R(0, 2)));
            SoundFile = c.Bind("Sound", "File", "fire.wav", "WAV file played on every shot, next to LaserGun.dll (or its sound folder). Replace it with your own sound if you like.");

            VerboseLog = c.Bind("Debug", "VerboseLog", false, "Log every shot, hit and fire to BepInEx\\LaserGun.log.");
        }
    }
}

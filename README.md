# Laser Gun (ZED ZONE)

A laser rifle for ZED ZONE: battery packs, near-instant beam shots, a laser sound and a chance to set
targets on fire.

| Item | What it is |
|---|---|
| Laser Rifle | Full auto, ~200 RPM (fireInterval 0.3 s), 350 damage, 1500 m/s, recoil 0.25, spread 0.6 |
| Laser Battery Pack | Rechargeable pack, 2000 energy (~50 shots). The rifle uses the game's battery feeding (feedingType 2): R swaps a pack in, firing drains it, chargers refill it |
| Laser Charge (old) | 1.0.0 ammo, now hidden; kept so old saves load |

The plugin (`src/`, BepInEx IL2CPP, `girthy_mods.zedzone.lasergun`):

- `Shots.cs` sets speed/damage when a laser round is fired, and rolls the fire chance on hits.
- `Ignite.cs` uses the game's own fire system: `VoxelFireSystem.IgniteCharactersNear` for a hit creature,
  `VoxelFireSystem.TryIgniteAt` for the ground.
- `Beam.cs` draws the beam (two short trails) and hides the game's bullet sprite.
- `Sound.cs` loads `fire.wav` from the mod folder (any PCM/float WAV) and plays it at the muzzle.
- `Remote.cs` gives other players' shots the same beam and sound.
- `Power.cs` turns the Laser Battery Pack (a Magazine item in the .zedmod) into a real game battery by copying a game battery's features with `[Battery] PackCapacity` energy, sets battery feeding, and tunes `batteryEnergyPerSec` so one pack gives `[Battery] ShotsPerBattery` shots (logs every battery's capacity).
- `Bundled.cs` loads `items\*.zedmod` next to the DLL (Girthy Mods / manual installs).

Settings: `BepInEx\config\girthy_mods.zedzone.lasergun.cfg`, or Mod Menu (F11). Log: `BepInEx\LaserGun.log`.

## Build

```powershell
dotnet build src\LaserGun.csproj -c Release -p:GameDir="D:\SteamLibrary\steamapps\common\ZED ZONE"
python tools\make_art.py      # sprites + Workshop preview (original art)
python tools\build_items.py   # .zedmod items + modpack.json (all stats live here)
.\publish-workshop.ps1 -SteamUser <login>
```

## Sound

`workshop/content/LaserGun/fire.wav` is one shot cut from a clip the mod author supplied (a fan
animation's lasgun sound). `fire-original.wav` is an original synthesized crack (`tools/make_sound.py`).
To ship only original audio, set `[Sound] File = fire-original.wav` or replace `fire.wav`.

## Art

The rifle sprite is made from the reference picture the mod author supplied (`art-src/laser-rifle-reference-3.png`), cut out from its white background (`tools/make_rifle_from_reference.py`). The battery pack sprite is also from the author's art (`art-src/laser-battery-pack-reference.png`, `tools/make_battery_from_reference.py`); the old charge sprite is original (`tools/make_art.py`).
The rifle uses the Boltgun rifle's canvas, sprite regions and hand/muzzle/magazine offsets so it sits
in the hands the same way.

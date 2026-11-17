"""Writes the game's item mods (.zedmod = zip of mod.json + sprite.png + preview.jpg + locale)
into workshop/content, plus modpack.json. Stats live here, so a balance change is one edit.
Run:  python tools/make_art.py && python tools/build_items.py"""
import json, os, zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.join(HERE, "..")
ITEMS = os.path.join(REPO, "items")
CONTENT = os.path.join(REPO, "workshop", "content")

PACK = "Laser Gun"
NAMES = {
    "rifle":   ("Laser Rifle", "A rugged military laser rifle in chipped olive paint. Instead of bullets it fires a focused beam of light "
                "that arrives the instant you pull the trigger: no drop, no lead, no travel time. Each shot hits for 350 and has a chance to set "
                "its target on fire. Runs on Laser Battery Packs: load one like a magazine (R swaps in a fresh one from your pack), 40 Wh a shot so about 50 shots each, and recharge them in the game's chargers. Fires about 300 shots a minute "
                "with almost no recoil, and makes a sharp electric crack you will learn to love."),
    "battery": ("Laser Battery Pack", "A heavy rechargeable power pack for the Laser Rifle. Holds 2000 Wh; each shot uses 40 Wh, so about 50 shots. "
                "Load it like a magazine (R swaps in a fresh one from your pack) and recharge it in the game's battery chargers."),
    "cell":    ("Laser Charge (old)", "Retired in 1.1.0: the Laser Rifle now runs on the game's own batteries. Kept so old saves still load."),
}

# Fixed ids. Never change a guid after release: saves and other mods refer to it.
GUID = {
    "rifle":   "93b81951-6391-4707-beb4-0ea971889dc7",
    "battery": "cef9bff0-579f-401b-a5a2-890dc6fd3ec8",
    "cell":    "b0286f7b-0596-4c17-a7ed-aab63338872a",
}
# Requested runtime ids (Boltgun uses 751075-751080); the game's loader remaps them if they clash.
RID = {"cell": 751090, "battery": 751091, "rifle": 751092}

DAMAGE = 350.0
FIRE_RATE_RPM = 300
BATTERY_SHOTS = 50
# The rifle runs on the game's own batteries (feedingType 2 = Battery). The plugin re-tunes this at start-up
# so one battery gives about BATTERY_SHOTS shots; this is only the starting value.
BATTERY_ENERGY_PER_SEC = 1.0

def base(key, mod_type, ppu, regions, size, rarity, weight, price, stack, craft, platform, out_n, repair, spawn=True, hidden=False):
    name, desc = NAMES[key]
    return {
        "modType": mod_type, "modGuid": GUID[key], "runtimeId": RID[key], "publishMode": 0, "targetNativeItemId": 0,
        "primaryLangCode": "en_US", "spritePath": "sprite.png", "ppu": ppu,
        "spriteRegions": [{"slotName": s, "region": {"x": r[0], "y": r[1], "w": r[2], "h": r[3]}} for s, r in regions],
        "itemName": name, "itemName_EN": name, "itemDescription": desc, "itemDescription_EN": desc,
        "itemRarity": rarity, "hiddenItem": hidden, "itemInventorySizeX": size[0], "itemInventorySizeY": size[1],
        "generateItemInRoom": spawn, "generateItemInField": False, "generateItemInCoastFloat": 0.0,
        "weight": weight, "itemPrice": price, "stackNumber": stack,
        "craftRecipeItems": [{"itemId": i, "itemNumber": float(n)} for i, n in craft],
        "craftPlatform": platform, "craftAudioType": 0, "craftOutputNumber": out_n,
        "repairRecipeItems": [{"itemId": i, "itemNumber": float(n)} for i, n in repair],
    }

# The sprite is made from the user's reference picture (tools/make_rifle_from_reference.py): a 304x144 canvas at
# 0.1408 scale with the pistol grip at (75, 99). The Main region's centre sits at the same place relative to the
# grip as on the Boltgun rifle, so the Boltgun's hand offsets still line up. The muzzle is 20 px lower than the
# Boltgun's, hence the lower gunPointOffset_y.
RIFLE_REGIONS = [("Main", (5, 14, 299, 118)), ("MainBody", (5, 14, 299, 118)), ("MovingPart", (150, 48, 30, 8)),
                 ("Barrel", (246, 54, 30, 20)), ("Aimer", (110, 46, 90, 6)), ("Gunstock", (6, 52, 55, 50))]

def rifle():
    d = base("rifle", "RangedWeapon", 230, RIFLE_REGIONS, (6, 2), 0.92, 4.2, 2400.0, 0,
             [(8, 10), (29, 6), (13, 6)], 2, 1, [(8, 1)])
    d.update({
        "feedingType": 2, "exinjectShellMode": 0, "isMechWeapon": False, "isFlareGun": False,
        "hp": 2000.0, "smokeParticleNumber": 4, "temperaturePerShot": 1.0, "ammoPlusOne": False,
        "ammoId": RID["cell"], "shellId": -1, "shellExinjectionForceFloat": 0.0,
        "liquidAmmoItemID": -1, "extraLiquidAmmoItemIDAry": [], "liquidCapacity": 0.0, "batteryEnergyPerSec": BATTERY_ENERGY_PER_SEC,
        "isAirPowerd": False, "airPressure_redZoneSize": 0.0, "airPressure_yellowZoneSize": 0.0,
        "airPressure_greenZoneSize": 0.0, "airPressure_chargeZoneSize": 0.0, "movingPartRange": 0.0,
        "hasMainGripBaseline": True, "mainGripBaselineY": 0.09031009674072266, "handPoseVersion": 1,
        "partModifiable_Barrel": False, "partModifiable_Aimer": True, "partModifiable_Gunstock": False,
        # The mod plays its own sound (sound/fire.wav) on every shot; the game's own shot sound is muted.
        "bulletPrefabName": "50BMGBullet", "fireClip": "50BMG_Fire_0", "fillingClip": "AK_Reload_2",
        "reloadClip": "Uzi_Reload", "reloadFinishedClip": "Shotgun_Reload", "boltActionClip": "",
        "fireClipVolume": 0.0, "fireClipPitch": 2.2, "fillingClipVolume": 1.0, "fillingClipPitch": 1.4,
        "reloadClipVolume": 1.0, "reloadClipPitch": 1.2, "reloadFinishedClipVolume": 1.0, "reloadFinishedClipPitch": 1.3,
        "boltActionClipVolume": 1.0, "boltActionClipPitch": 1.0,
        "weaponType": 2, "weaponSlotSize": 1, "gunActionType": 0, "singleHand": False,
        "moveSpeedFloat": 0.95, "damage": DAMAGE, "fireRange": 45.0, "noiseRange": 35.0,
        "bulletForce": 300.0, "bulletSpeed": 1200.0, "fireAngle": 0.6, "bulletNumber": 1,
        "fireInterval": round(60.0 / FIRE_RATE_RPM, 3), "recoilIntensity": 0.25, "reloadTime": 2.4,
        "defaultMagazineId": -1, "magazineSize": BATTERY_SHOTS,
        "weaponHandHeightOffset": -0.2847541570663452, "weaponHandOffsetRange": 0.3921833336353302,
        "leftHandOffsetRange": 0.4398137927055359, "leftHandHeightOffset": -0.0033399462699890138,
        "spriteOffset_x": 0.293729305267334, "spriteOffset_y": 0.2013387680053711,
        "gunPointOffset_x": 0.36, "gunPointOffset_y": 0.129,
        "magazineOffset_x": 0.22326290607452393, "magazineOffset_y": 0.2423933744430542,
        "barrelOffset_x": 0.26514285802841187, "barrelOffset_y": 0.11695237457752228, "aimerOffset_x": 0.045904770493507385, "aimerOffset_y": 0.12323809415102005,
        "gunstockOffset_x": -0.08761905878782272, "gunstockOffset_y": 0.024761900305747986,
    })
    return d

def cell():
    # Fallback recipe uses the same game parts as the Boltgun's bolts (7 x4, 75 x2 -> 25);
    # the plugin swaps in the game's battery item if it finds one by name.
    d = base("cell", "Ammo", 2500, [("Main", (27, 27, 206, 48))], (1, 1), 0.6, 0.02, 15.0, 200,
             [(7, 4), (75, 2)], 3, 25, [(56, 1)], spawn=False, hidden=True)
    d.update({"ammoNumber": 10, "ammoBaseDamage": DAMAGE})
    return d

# Laser Battery Pack: shipped as a Magazine item for its sprite/name/recipe; at start-up the plugin (Power.cs)
# gives it the game's battery feature with PACK_CAPACITY energy, so the rifle loads it and chargers charge it.
PACK_CAPACITY = 2000

def battery():
    d = base("battery", "Magazine", 400, [("Main", (8, 2, 112, 140))], (2, 2), 0.3, 1.2, 600.0, 0,
             [(34, 6), (35, 3)], 2, 1, [(35, 1)], spawn=True, hidden=False)
    d.update({"ammoId": RID["cell"], "magazineSize": BATTERY_SHOTS, "durabilityCostPerShot": 0.005,
              "sizeFloat": 1.2000000476837158, "magazineFit": 1,
              "magazineOffset_x": 0.012888908386230469, "magazineOffset_y": -0.12591969966888428})
    return d

BUILD = {
    ("RangedWeapons", "rifle"): rifle,
    ("Ammo", "cell"): cell,
    ("Magazine", "battery"): battery,
}

def main():
    os.makedirs(CONTENT, exist_ok=True)
    pack = {"packName": PACK, "packDescription": NAMES["rifle"][1], "author": "girthy_mods",
            "createTime": "2026-11-17 00:00:00", "mods": []}
    for (kind, key), fn in BUILD.items():
        src = os.path.join(ITEMS, kind, key)
        d = fn()
        name, desc = NAMES[key]
        locale = {"langCode": "en_US", "modGuid": d["modGuid"], "translatorNote": "", "itemName": name,
                  "itemDescription": desc, "extraFields": []}
        out_dir = os.path.join(CONTENT, kind); os.makedirs(out_dir, exist_ok=True)
        rel = f"{kind}/{d['modGuid']}.zedmod"
        with zipfile.ZipFile(os.path.join(CONTENT, rel), "w", zipfile.ZIP_DEFLATED) as z:
            z.writestr("mod.json", json.dumps(d, indent=4, ensure_ascii=False))
            z.write(os.path.join(src, "sprite.png"), "sprite.png")
            z.write(os.path.join(src, "preview.jpg"), "preview.jpg")
            z.writestr("locale/en_US.json", json.dumps(locale, indent=4, ensure_ascii=False))
        pack["mods"].append({"modGuid": d["modGuid"], "modType": d["modType"], "itemName": name, "relativePath": rel})
        print("built", rel, name)
    with open(os.path.join(CONTENT, "modpack.json"), "w", encoding="utf-8") as f:
        json.dump(pack, f, indent=4, ensure_ascii=False)

if __name__ == "__main__":
    main()

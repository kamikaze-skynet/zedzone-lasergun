using System;
using HarmonyLib;
using UnityEngine;

namespace LaserGun
{
    internal static class Patches
    {
        internal static void Apply(Harmony h)
        {
            Patch(h, typeof(BasicBullet), nameof(BasicBullet.Init), null, nameof(Init_Post));
            Patch(h, typeof(BasicBullet), nameof(BasicBullet.HitCharacter), nameof(HitCharacter_Pre), nameof(HitCharacter_Post));
            Patch(h, typeof(BasicBullet), nameof(BasicBullet.OnHit), null, nameof(OnHit_Post));
            Patch(h, typeof(ModLoader), nameof(ModLoader.LoadAll), null, nameof(LoadAll_Post));
            Patch(h, typeof(BasicRangedWeapon), nameof(BasicRangedWeapon.CanFire), null, nameof(CanFire_Post));
            Patch(h, typeof(BasicRangedWeapon), nameof(BasicRangedWeapon.Fire), nameof(Fire_Pre), nameof(Fire_Post));
            Patch(h, typeof(BasicRangedWeapon), nameof(BasicRangedWeapon.LateUpdate), null, nameof(WeaponLate_Post));
            Patch(h, typeof(ItemAttr_RangedWeapon), nameof(ItemAttr_RangedWeapon.SwapBatteryIntoWeapon), null, nameof(SwapBattery_Post));
            Patch(h, typeof(ItemAttr_RangedWeapon), nameof(ItemAttr_RangedWeapon.ClearAmmoFromRangedWeapon), nameof(ClearAmmo_Pre), null);
            Patch(h, typeof(BasicRangedWeapon), nameof(BasicRangedWeapon.OnFireButtonDown), nameof(FireDown_Pre), null);
            Patch(h, typeof(BasicRangedWeapon), nameof(BasicRangedWeapon.Fire_EnergyWeapon), nameof(FireEnergy_Pre), null);
            Patch(h, typeof(BasicRangedWeapon), nameof(BasicRangedWeapon.Fire_Cartridge), nameof(FireCartridge_Pre), nameof(FireCartridge_Post));
            Patch(h, typeof(PreviewPanel), nameof(PreviewPanel.Setup), null, nameof(PreviewSetup_Post));
            Patch(h, typeof(PreviewPanel), nameof(PreviewPanel.OnReload), null, nameof(PreviewReload_Post));
            Patch(h, typeof(PreviewPanel), nameof(PreviewPanel.OnFirePreview), nameof(PreviewFire_Pre), null);
            Patch(h, typeof(PreviewPanel), nameof(PreviewPanel.OnDisable), null, nameof(PreviewOff_Post));
            Patch(h, typeof(CharacterWeaponSlotUI), nameof(CharacterWeaponSlotUI.Update), null, nameof(WeaponSlot_Post));
            Patch(h, typeof(BasicItemUI), nameof(BasicItemUI.RefreshItemUI), null, nameof(ItemUI_Post));
            Patch(h, typeof(BasicItemUI), nameof(BasicItemUI.RefreshItemUIIconSpriteImmediate), null, nameof(ItemUI_Post));
            Patch(h, typeof(BasicItemUI), nameof(BasicItemUI.RefreshItemUIIconIfAlive), null, nameof(ItemUI_Post));
            Patch(h, typeof(ZedZone.Net.NetProjectileSync), nameof(ZedZone.Net.NetProjectileSync.SpawnVisualBullet), nameof(Spawn_Pre), nameof(Spawn_Post));
        }

        static void Patch(Harmony h, Type t, string method, string pre, string post)
        {
            try
            {
                var target = AccessTools.Method(t, method);
                if (target == null) { PLog.Error($"{t.Name}.{method} not found; that part of the mod is off."); return; }
                h.Patch(target, pre == null ? null : new HarmonyMethod(typeof(Patches), pre),
                                post == null ? null : new HarmonyMethod(typeof(Patches), post));
            }
            catch (Exception e) { PLog.Error($"Patching {t.Name}.{method} failed", e); }
        }

        static void LoadAll_Post()
        {
            try { Bundled.LoadMissing(); } catch (Exception e) { PLog.Error("Loading bundled items failed", e); }
            try { Power.Check(); } catch (Exception e) { PLog.Error("Battery setup failed", e); }
            try { Crafting.Apply(); } catch (Exception e) { PLog.Error("Crafting recipes failed", e); }
        }

        static bool _diagWarned;
        static void FireDown_Pre(BasicRangedWeapon __instance) { try { Diag.Event(__instance, "fire button down"); } catch { } }
        // With the power HUD on, the game treats the rifle as an energy weapon, whose own fire code does no damage
        // with this gun. Fire a normal shot instead.
        static bool FireEnergy_Pre(BasicRangedWeapon __instance)
        {
            try
            {
                if (!Battery.IsMine(__instance)) return true;
                Diag.Event(__instance, "Fire_EnergyWeapon -> shot");
                __instance.Fire_Cartridge(1f);
                return false;
            }
            catch (Exception e) { PLog.Warn("Energy fire redirect failed: " + e.Message); return true; }
        }
        // Fire_Cartridge can be reached without Fire(); hold it to the same pause and count the shot there if so.
        static bool FireCartridge_Pre(BasicRangedWeapon __instance, out float __state)
        {
            __state = -1f;
            try { Diag.Event(__instance, "Fire_Cartridge"); } catch { }
            try
            {
                if (_inFire) return true;
                if (!Battery.Allow(__instance)) return false;
                __state = Battery.Before(__instance);
            }
            catch { }
            return true;
        }
        static void FireCartridge_Post(BasicRangedWeapon __instance, float __state)
        {
            try { if (!_inFire) Battery.After(__instance, __state); } catch { }
        }
        static bool _inFire;

        static bool Fire_Pre(BasicRangedWeapon __instance, out float __state)
        {
            __state = -1f;
            try { if (!Battery.Allow(__instance)) return false; } catch { }
            try { __state = Battery.Before(__instance); } catch (Exception e) { PLog.Debug("Battery pre failed: " + e.Message); }
            _inFire = true;
            return true;
        }

        static void Fire_Post(BasicRangedWeapon __instance, float __state)
        {
            _inFire = false;
            try { Battery.After(__instance, __state); } catch (Exception e) { PLog.Debug("Battery drain failed: " + e.Message); }
        }

        static void SwapBattery_Post(ItemData weaponData)
        {
            try { Battery.Swapped(weaponData); } catch (Exception e) { PLog.Warn("Battery load hook failed: " + e.Message); }
        }

        static void ClearAmmo_Pre(ItemData rangedWeaponItemData)
        {
            try { Battery.BeforeClearAmmo(rangedWeaponItemData); } catch (Exception e) { PLog.Warn("Unload hook failed: " + e.Message); }
        }

        static void PreviewSetup_Post(PreviewPanel __instance, BaseModData data) { Preview.Setup(__instance, data); }
        static void PreviewReload_Post(PreviewPanel __instance) { Preview.Load(__instance, "reload"); }
        static void PreviewFire_Pre(PreviewPanel __instance)
        {
            try
            {
                if (!Weapons.PreviewOurs) return;
                var d = __instance.currentCharacter?.rangedWeapon?.weaponData;
                if (d != null && Battery.Energy(d) < Settings.EnergyPerShot.Value) Preview.Load(__instance, "fire");
            }
            catch { }
        }
        static void PreviewOff_Post() { Preview.Closed(); }
        static void WeaponSlot_Post(CharacterWeaponSlotUI __instance) { PackIcon.WeaponSlot(__instance); }
        static void ItemUI_Post(BasicItemUI __instance) { PackIcon.Refreshed(__instance); }

        static bool _drawWarned;
        static void WeaponLate_Post(BasicRangedWeapon __instance)
        {
            try { Battery.Draw(__instance); }
            catch (Exception e) { if (!_drawWarned) { _drawWarned = true; PLog.Warn("Pack drawing failed: " + e); } }
        }

        static void CanFire_Post(BasicRangedWeapon __instance, ref bool __result)
        {
            try { Battery.CanFire(__instance, ref __result); } catch (Exception e) { PLog.Debug("CanFire battery check failed: " + e.Message); }
            try { Diag.CanFire(__instance, __result); } catch (Exception e) { if (!_diagWarned) { _diagWarned = true; PLog.Warn("CanFire diag failed: " + e); } }
        }

        static void Init_Post(BasicBullet __instance)
        {
            try { if (__instance != null && Weapons.IsLaser(Weapons.ItemIdOf(__instance))) Diag.Note("laser shot spawned"); } catch { }
            try { Shots.OnFired(__instance, false); } catch (Exception e) { PLog.Error("Fire hook failed", e); }
        }

        static void HitCharacter_Pre(BasicBullet __instance)
        {
            try { Shots.BeforeCharacter(__instance); } catch (Exception e) { PLog.Error("Hit hook (pre) failed", e); }
        }

        static void HitCharacter_Post(BasicBullet __instance, RaycastHit2D hit2D, bool __result)
        {
            try { Shots.AfterCharacter(__instance, hit2D, __result); } catch (Exception e) { PLog.Error("Hit hook failed", e); }
        }

        static void OnHit_Post(BasicBullet __instance, RaycastHit2D hit, bool __result)
        {
            try { Shots.AfterAnyHit(__instance, hit, __result); } catch (Exception e) { PLog.Error("Impact hook failed", e); }
        }

        static void Spawn_Pre(ZedZone.Net.MsgProjectileFire msg)
        {
            try { Remote.Before(msg); } catch (Exception e) { PLog.Error("Remote shot hook (pre) failed", e); }
        }

        static void Spawn_Post(Transform parent)
        {
            try { Remote.After(parent); } catch (Exception e) { PLog.Error("Remote shot hook failed", e); }
        }
    }
}

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
        }

        static void CanFire_Post(BasicRangedWeapon __instance, bool __result)
        {
            try { Diag.CanFire(__instance, __result); } catch (Exception e) { PLog.Debug("CanFire diag failed: " + e.Message); }
        }

        static void Init_Post(BasicBullet __instance)
        {
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

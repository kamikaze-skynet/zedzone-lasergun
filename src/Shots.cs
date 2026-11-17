using System;
using UnityEngine;

namespace LaserGun
{
    // Per-shot logic: speed, damage, beam and sound when fired; the fire chance when it hits.
    internal static class Shots
    {
        internal static void OnFired(BasicBullet b, bool remote)
        {
            if (b == null || !Settings.Enabled.Value) return;
            int id = Weapons.ItemIdOf(b);
            if (!remote && !Weapons.IsLaser(id)) return;
            if (!remote) try { remote = b.netVisualOnly; } catch { }

            b.bulletSpeed = Settings.BeamSpeed.Value;
            if (!remote && Settings.Damage.Value > 0f) b.bulletDamage = Settings.Damage.Value;
            b.penetrateTime = 0;          // a beam stops in the first thing it burns
            b.m_penetrateCounter = 0;

            Vector3 muzzle = b.transform.position;
            try { var sp = b.startPos; if (sp != Vector3.zero) muzzle = sp; } catch { }
            Beam.Fire(b);
            try { Sound.Shot(muzzle); } catch (Exception e) { PLog.Debug("Sound: " + e.Message); }
            if (Settings.VerboseLog.Value) PLog.Debug($"Fired: item {id}{(remote ? " (other player)" : "")} speed {b.bulletSpeed:0} dmg {b.bulletDamage:0}");
        }

        internal static void BeforeCharacter(BasicBullet b)
        {
            if (!Beam.Tracked(b)) return;
            b.m_penetrateCounter = 1;     // no through-and-through copy
        }

        internal static void AfterCharacter(BasicBullet b, RaycastHit2D hit, bool hitIt)
        {
            if (!Beam.Tracked(b) || !hitIt) return;
            try { Beam.HitAt(b, hit.point); } catch { }
            bool remote = false;
            try { remote = b.netVisualOnly; } catch { }
            if (remote) return;             // the shooter rolls the fire chance and the game syncs the burning
            BasicCharacterController target = null;
            try { if (hit.collider != null) target = BasicCharacterController.GetCharacterFromCollider(hit.collider); } catch { }
            if (target == null) return;
            if (UnityEngine.Random.value < Settings.IgniteChance.Value) Ignite.Character(target, b, hit.point);
        }

        internal static void AfterAnyHit(BasicBullet b, RaycastHit2D hit, bool consumed)
        {
            if (!Beam.Tracked(b) || !consumed) return;
            try { Beam.HitAt(b, hit.point); } catch { }
            bool remote = false;
            try { remote = b.netVisualOnly; } catch { }
            if (remote) return;
            BasicCharacterController target = null;
            try { if (hit.collider != null) target = BasicCharacterController.GetCharacterFromCollider(hit.collider); } catch { }
            if (target != null) return;     // characters are handled in AfterCharacter
            if (UnityEngine.Random.value < Settings.IgniteChanceSurfaces.Value) Ignite.Ground(hit.point, b);
        }
    }
}

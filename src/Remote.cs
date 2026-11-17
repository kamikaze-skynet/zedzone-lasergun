using System;
using UnityEngine;
using ZedZone.Net;

namespace LaserGun
{
    // Other players' shots arrive as visual-only rounds (NetProjectileSync.SpawnVisualBullet) with no weapon
    // attached. The message's weapon id is kept while the round spawns, then the round gets a beam and sound.
    internal static class Remote
    {
        internal static int WeaponId = -1;

        internal static void Before(MsgProjectileFire msg)
        {
            WeaponId = -1;
            try { if (msg != null) WeaponId = msg.weaponItemId; } catch { }
        }

        internal static void After(Transform parent)
        {
            int id = WeaponId;
            WeaponId = -1;
            if (id < 0 || !Settings.Enabled.Value || !Weapons.IsLaser(id) || parent == null) return;
            BasicBullet b = null;
            try
            {
                int n = parent.childCount;
                for (int i = n - 1; i >= 0 && i >= n - 4; i--)
                {
                    var cand = parent.GetChild(i).GetComponent<BasicBullet>();
                    if (cand != null && cand.netVisualOnly) { b = cand; break; }
                }
            }
            catch (Exception e) { PLog.Debug("Remote shot lookup failed: " + e.Message); }
            if (b == null || Beam.Tracked(b)) return;
            Shots.OnFired(b, true);
        }
    }
}

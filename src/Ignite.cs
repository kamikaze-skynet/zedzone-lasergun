using System;
using UnityEngine;

namespace LaserGun
{
    // Setting things alight with the game's own fire system (VoxelFireSystem), so burning looks,
    // spreads, hurts and gets put out exactly like any other fire in ZED ZONE.
    internal static class Ignite
    {
        // A hit creature catches fire: the game ignites the characters at that spot.
        internal static void Character(BasicCharacterController target, BasicBullet b, Vector2 at)
        {
            try
            {
                VoxelFireSystem.IgniteCharactersNear(at);
                if (Settings.VerboseLog.Value) PLog.Debug($"Ignited the target at {at}");
            }
            catch (Exception e) { PLog.Warn("Setting the target on fire failed: " + e.Message); }
            if (Settings.IgniteGroundWithTarget.Value) Ground(at, b);
        }

        // A shot that hits the ground or an object can start a fire there (grass, wood...).
        internal static void Ground(Vector2 at, BasicBullet b)
        {
            try
            {
                bool lit = VoxelFireSystem.TryIgniteAt(at);
                if (Settings.VerboseLog.Value) PLog.Debug($"Ground fire at {at}: {(lit ? "lit" : "nothing to burn")}");
            }
            catch (Exception e) { PLog.Warn("Starting a fire failed: " + e.Message); }
        }
    }
}

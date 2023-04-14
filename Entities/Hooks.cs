// https://github.com/EverestAPI/Resources/wiki/Your-First-Code-Mod#modifying-the-games-code

using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Monocle;
using Celeste.Mod.KisluHelper.Entities;
using System.Runtime.InteropServices.ComTypes;

namespace Celeste.Mod.KisluHelper
{
    public static class Hooks
    {
        private static ILHook wallJumpHook;

        // TODO get from Player?
        private const int WallJumpCheckDist = 3;
        private const int SuperWallJumpCheckDist = 5;

        private const float initWallJumpSpeedH = 130f;

        private const float initWallJumpSpeedV = -105f;

        private const float initWallBounceSpeedH = 170f;

        private const float initWallBounceSpeedV = -160f;

        internal static void Load()
        {
            //On.Celeste.Player.ClimbBegin += DebugPrint;
            //On.Celeste.Player.WallJump += DebugPrint2;
            On.Celeste.Player.Jump += DebugPrint3;
            On.Celeste.Player.WallJump += DebugPrint4;

            WallJumpBooster.LoadHooks();
        }

        internal static void Unload()
        {
            //On.Celeste.Player.ClimbBegin -= DebugPrint;
            //On.Celeste.Player.WallJump -= DebugPrint2;
            On.Celeste.Player.Jump -= DebugPrint3;
            On.Celeste.Player.WallJump -= DebugPrint4;

            WallJumpBooster.UnloadHooks();
        }

        private static void DebugPrint(On.Celeste.Player.orig_ClimbBegin orig, Player self)
        {
            orig(self);
            Logger.Log("KisluHelper", "Debug: Climb has begun");
        }

        private static void DebugPrint2(On.Celeste.Player.orig_WallJump orig, Player self, int dir)
        {
            Logger.Log("KisluHelper", $"Debug: boost? {ShouldApplyWallJumpBoost(self)}");
            Logger.Log("KisluHelper", $"Debug: WallJump of {GetWall(self).GetType()}");
            orig(self, dir);
        }

        private static void DebugPrint3(On.Celeste.Player.orig_Jump orig, Player self, bool particles, bool sfx)
        {
            DebugPrint5(self);
            orig(self, particles, sfx);
        }

        private static void DebugPrint4(On.Celeste.Player.orig_WallJump orig, Player self, int dir)
        {
            DebugPrint5(self);
            orig(self, dir);
        }

        private static void DebugPrint5(Player self)
        {
            Logger.Log("KisluHelper", $"Jumped. Check? {DebugCheck(self, 1) || DebugCheck(self, -1)}");
        }

        private static Solid GetWall(Player self)
        {
            Solid solid = self.CollideFirst<Solid>(self.Position - Vector2.UnitX * (int)self.Facing * WallJumpCheckDist + Vector2.UnitY);
            if (solid == null)
            {
                solid = self.CollideFirst<Solid>(self.Position + Vector2.UnitX * (int)self.Facing * WallJumpCheckDist + Vector2.UnitY);
            }
            return solid;
        }

        private static bool ShouldApplyWallJumpBoost(Player self)
        {
            Solid wall = GetWall(self);
            if (wall != null)
            {
                return wall.GetType() == typeof(WallJumpBooster);
            }
            return false;
        }

        private static bool DebugCheck(Player player, int dir)
        {
            int num = 3;
            bool flag = player.DashAttacking && player.DashDir.X == 0f && player.DashDir.Y == -1f;
            if (flag)
            {
                Spikes.Directions directions = ((dir <= 0) ? Spikes.Directions.Right : Spikes.Directions.Left);
                foreach (Spikes entity in Engine.Scene.Tracker.GetEntities<Spikes>())
                {
                    if (entity.Direction == directions && player.CollideCheck(entity, player.Position + Vector2.UnitX * dir * 5f))
                    {
                        flag = false;
                        break;
                    }
                }
            }
            if (flag)
            {
                num = 5;
            }
            if (player.ClimbBoundsCheck(dir) && !ClimbBlocker.EdgeCheck(Engine.Scene, player, dir * num))
            {
                return player.CollideCheck<Solid>(player.Position + Vector2.UnitX * dir * num);
            }
            return false;
        }
    }
}

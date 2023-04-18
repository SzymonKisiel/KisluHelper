using Celeste.Mod.Entities;
using Celeste.Mod.KisluHelper.Components;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.KisluHelper.Entities
{
    [CustomEntity("KisluHelper/WallJumpBooster")]
    public class WallJumpBooster : Solid
    {
        private static ILHook wallJumpHook;
        
        private const int WallJumpCheckDist = 3;

        private const int SuperWallJumpCheckDist = 5;

        private const float initWallJumpSpeedH = 130f;

        private const float initWallJumpSpeedV = -105f;

        private const float initWallBounceSpeedH = 170f;

        private const float initWallBounceSpeedV = -160f;

        private const float wallJumpMultiplier = 2.0f;

        private enum JumpType
        {
            ClimbJump,
            WallJump,
            SuperWallJump
        }

        public WallJumpBooster(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, true)
        {
        }
        public override void Render()
        {
            Draw.Rect(X, Y, Width, Height, Color.Red);
        }

        public static void LoadHooks()
        {
            wallJumpHook = new ILHook(typeof(Player).GetMethod("orig_WallJump", BindingFlags.Instance | BindingFlags.NonPublic), ModWallJump);
            IL.Celeste.Player.Jump += ModClimbJump;
            IL.Celeste.Player.SuperWallJump += ModWallBounce;
        }

        public static void UnloadHooks()
        {
            if (wallJumpHook != null)
            {
                wallJumpHook.Dispose();
            }
            IL.Celeste.Player.Jump -= ModClimbJump;
            IL.Celeste.Player.SuperWallJump -= ModWallBounce;
        }

        private static void ModClimbJump(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(initWallJumpSpeedV)))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, typeof(Player).GetField("onGround", BindingFlags.NonPublic | BindingFlags.Instance));
                cursor.EmitReference(JumpType.ClimbJump);
                cursor.EmitDelegate(ApplyWallJumpBoost);
            }
        }

        private static void ModWallJump(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(initWallJumpSpeedV)))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, typeof(Player).GetField("onGround", BindingFlags.NonPublic | BindingFlags.Instance));
                cursor.EmitReference(JumpType.WallJump);
                cursor.EmitDelegate(ApplyWallJumpBoost);
            }
        }

        private static void ModWallBounce(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(initWallBounceSpeedV)))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, typeof(Player).GetField("onGround", BindingFlags.NonPublic | BindingFlags.Instance));
                cursor.EmitReference(JumpType.SuperWallJump);
                cursor.EmitDelegate(ApplyWallJumpBoost);
            }
        }

        private static float ApplyWallJumpBoost(float origSpeed, Player self, bool isOnGround, JumpType jumpType)
        {
            // don't apply boost if it is normal jump instead of climb jump or dash-climb jump (corner boost)
            bool isClimbing = self.StateMachine.State == Player.StClimb || self.StateMachine.State == Player.StDash;
            if (jumpType == JumpType.ClimbJump && !isClimbing)
            {
                return origSpeed;
            }


            if (self == null || isOnGround && (jumpType != JumpType.ClimbJump))
            {
                return origSpeed;
            }

            Solid wall = GetWall(self, jumpType);
            if (wall != null)
            {
                if (wall.GetType() == typeof(WallJumpBooster))
                {
                    return origSpeed * wallJumpMultiplier;
                }
            }
            return origSpeed;
        }

        private static Solid GetWall(Player self, JumpType jumpType)
        {
            // if player is climb jumping first check the wall they are facing
            float firstCheckDir = (float)self.Facing;

            float checkDist = jumpType == JumpType.SuperWallJump ? SuperWallJumpCheckDist : WallJumpCheckDist;

            Solid solid = self.CollideFirst<Solid>(self.Position + firstCheckDir * Vector2.UnitX * checkDist);
            if (solid == null)
            {
                solid = self.CollideFirst<Solid>(self.Position - firstCheckDir * Vector2.UnitX * checkDist);
            }

            return solid;
        }
    }
}

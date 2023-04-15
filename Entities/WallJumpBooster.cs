using Celeste.Mod.Entities;
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

        private bool isColliding;

        // TODO get from Player?
        private const int WallJumpCheckDist = 3;
        private const int SuperWallJumpCheckDist = 5;

        private const float initWallJumpSpeedH = 130f;

        private const float initWallJumpSpeedV = -105f;

        private const float initWallBounceSpeedH = 170f;

        private const float initWallBounceSpeedV = -160f;

        private const float wallJumpMultiplier = 2.0f;

        //public static float WallJumpBoost = 2.0f;

        public WallJumpBooster(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, true)
        {
        }
        public override void Render()
        {
            Draw.Rect(base.X, base.Y, base.Width, base.Height, Color.Red);
        }

        public static void LoadHooks()
        {
            wallJumpHook = new ILHook(typeof(Player).GetMethod("orig_WallJump", BindingFlags.Instance | BindingFlags.NonPublic), ModJump);
            IL.Celeste.Player.Jump += ModJump;
            IL.Celeste.Player.SuperWallJump += ModWallBounce;
        }

        public static void UnloadHooks()
        {

            if (wallJumpHook != null)
            {
                wallJumpHook.Dispose();
            }
            IL.Celeste.Player.Jump -= ModJump;
            IL.Celeste.Player.SuperWallJump -= ModWallBounce;
        }

        private static void ModJump(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(initWallJumpSpeedV)))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, typeof(Player).GetField("onGround", BindingFlags.NonPublic | BindingFlags.Instance));
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
                cursor.EmitDelegate(ApplyWallJumpBoost);
            }
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

        private static float ApplyWallJumpBoost(float origSpeed, Player self, bool isOnGround)
        {
            bool isClimbing = self.StateMachine.State == Player.StClimb;

            if (self == null || isOnGround && !isClimbing)
            {
                return origSpeed;
            }
            Solid wall = GetWall(self);
            if (wall != null)
            {
                if (wall.GetType() == typeof(WallJumpBooster))
                {
                    return origSpeed * wallJumpMultiplier;
                }
            }
            return origSpeed;
        }
    }
}

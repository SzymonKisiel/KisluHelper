using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;

namespace Celeste.Mod.KisluHelper.Entities
{
    [CustomEntity("KisluHelper/BouncyWall")]
    public class BouncyWall : Solid {

        private static ILHook wallJumpHook;

        private const int WallJumpCheckDist = 3;

        private const int SuperWallJumpCheckDist = 5;

        private const float initWallJumpSpeedH = 130f;

        private const float initWallJumpSpeedV = -105f;

        private const float initWallBounceSpeedH = 170f;

        private const float initWallBounceSpeedV = -160f;

        private const float wallJumpSpeedHMultiplier = 2.5f;

        private const float wallJumpSpeedVMultiplier = 0.1f;

        private const float wallBounceSpeedHMultiplier = wallJumpSpeedHMultiplier * initWallJumpSpeedH / initWallBounceSpeedH;

        private const float wallBounceSpeedVMultiplier = wallJumpSpeedVMultiplier * initWallJumpSpeedV / initWallBounceSpeedV;

        private enum JumpType
        {
            WallJump,
            SuperWallJump
        }

        public BouncyWall(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, true)
        {
            //collider = this.Collider;
            //Collider c = new Hitbox(this.Width+3.0f, this.Height, this.Position.X, this.Position.Y);
            //playerCollider = new PlayerCollider(OnPlayer, c);
            //Add(playerCollider = new PlayerCollider(OnPlayer));
        }

        private void OnPlayer(Player player)
        {
            Logger.Log("KisluHelper/GoodbyeEntity", "OnPlayer");
        }

        public override void Render()
        {
            Color c = Color.Chocolate;
            if (GetExampleSettings())
            {
                c = Color.GreenYellow;
            }
            Draw.Rect(base.X, base.Y, base.Width, base.Height, c);
        }

        public bool GetExampleSettings()
        {
            return KisluHelperModule.Settings.ExampleSwitch;
        }

        public static void LoadHooks()
        {
            wallJumpHook = new ILHook(typeof(Player).GetMethod("orig_WallJump", BindingFlags.Instance | BindingFlags.NonPublic), ModWallJump);

            IL.Celeste.Player.SuperWallJump += ModWallBounce;
        }

        public static void UnloadHooks()
        {
            if (wallJumpHook != null)
            {
                wallJumpHook.Dispose();
            }

            IL.Celeste.Player.SuperWallJump -= ModWallBounce;
        }

        private static void ModWallJump(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            VariableDefinition shouldApplyModVar = new VariableDefinition(il.Import(typeof(bool)));
            il.Body.Variables.Add(shouldApplyModVar);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, typeof(Player).GetField("onGround", BindingFlags.NonPublic | BindingFlags.Instance));
            cursor.EmitReference(JumpType.WallJump);
            cursor.EmitDelegate(ShouldApplyWallJumpBoost);
            cursor.Emit(OpCodes.Stloc, shouldApplyModVar);

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(initWallJumpSpeedH)))
            {
                cursor.Emit(OpCodes.Ldloc, shouldApplyModVar);
                cursor.EmitReference(wallJumpSpeedHMultiplier);
                cursor.EmitDelegate<Func<float, bool, float, float>>((orig, shouldApply, mult) =>
                {
                    return shouldApply ? mult * orig : orig;
                });
            }

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(initWallJumpSpeedV)))
            {
                cursor.Emit(OpCodes.Ldloc, shouldApplyModVar);
                cursor.EmitReference(wallJumpSpeedVMultiplier);
                cursor.EmitDelegate<Func<float, bool, float, float>>((orig, shouldApply, mult) =>
                {
                    return shouldApply ? mult * orig : orig;
                });
            }
        }

        private static void ModWallBounce(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            VariableDefinition shouldApplyModVar = new VariableDefinition(il.Import(typeof(bool)));
            il.Body.Variables.Add(shouldApplyModVar);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, typeof(Player).GetField("onGround", BindingFlags.NonPublic | BindingFlags.Instance));
            cursor.EmitReference(JumpType.SuperWallJump);
            cursor.EmitDelegate(ShouldApplyWallJumpBoost);
            cursor.Emit(OpCodes.Stloc, shouldApplyModVar);

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(initWallBounceSpeedH)))
            {
                Logger.Log("HelloCeleste", $"WallBounce V speed changed to: {wallBounceSpeedHMultiplier}");

                cursor.Emit(OpCodes.Ldloc, shouldApplyModVar);
                cursor.EmitReference(wallBounceSpeedHMultiplier);
                cursor.EmitDelegate<Func<float, bool, float, float>>((orig, shouldApply, mult) =>
                {
                    return shouldApply ? mult * orig : orig;
                });
            }

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(initWallBounceSpeedV)))
            {
                Logger.Log("HelloCeleste", $"WallBounce V speed changed to: {wallBounceSpeedVMultiplier}");

                cursor.Emit(OpCodes.Ldloc, shouldApplyModVar);
                cursor.EmitReference(wallBounceSpeedVMultiplier);
                cursor.EmitDelegate<Func<float, bool, float, float>>((orig, shouldApply, mult) =>
                {
                    return shouldApply ? mult * orig : orig;
                });
            }
        }

        private static bool ShouldApplyWallJumpBoost(Player self, bool isOnGround, JumpType jumpType)
        {
            bool isClimbing = self.StateMachine.State == Player.StClimb;

            if (self == null || isOnGround && !isClimbing)
            {
                return false;
            }

            Solid wall = GetWall(self, isClimbing, jumpType);
            if (wall != null)
            {
                if (wall.GetType() == typeof(BouncyWall))
                {
                    return true;
                }
            }

            return false;
        }

        private static Solid GetWall(Player self, bool isClimbing, JumpType jumpType)
        {
            float firstCheckDir = -(float)self.Facing;

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

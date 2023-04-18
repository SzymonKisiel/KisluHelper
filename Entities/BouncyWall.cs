using Celeste.Mod.Entities;
using Celeste.Mod.KisluHelper.Components.Constants;
using Celeste.Mod.KisluHelper.Components.Enums;
using Celeste.Mod.KisluHelper.Components.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil;
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

        private const float wallJumpSpeedHMultiplier = 2.5f;

        private const float wallJumpSpeedVMultiplier = 0.1f;

        private const float wallBounceSpeedHMultiplier = wallJumpSpeedHMultiplier * PlayerConstants.WallJumpSpeedH / PlayerConstants.WallBounceSpeedH;

        private const float wallBounceSpeedVMultiplier = wallJumpSpeedVMultiplier * PlayerConstants.WallJumpSpeedV / PlayerConstants.WallBounceSpeedV;

        private const float ForceMoveTime = 0.16f;

        private const float ModifiedForceMoveTime = 0.08f;

        public BouncyWall(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, true)
        {
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
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate(ShouldApplyWallJumpBoost);
            cursor.Emit(OpCodes.Stloc, shouldApplyModVar);

            if (cursor.TryGotoNext(MoveType.Before,
                instr => instr.OpCode == OpCodes.Ldarg_0,
                instr => instr.MatchLdfld<Player>("moveX")))
            {

                // sneak between the ldarg.0 and the ldfld (the ldarg.0 is the target to a jump instruction, so we should put ourselves after that.)
                cursor.Index++;

                ILCursor cursorAfterBranch = cursor.Clone();
                if (cursorAfterBranch.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Brfalse_S))
                {

                    // pop the ldarg.0
                    cursor.Emit(OpCodes.Pop);

                    cursor.Emit(OpCodes.Ldloc, shouldApplyModVar);
                    cursor.Emit(OpCodes.Brtrue_S, cursorAfterBranch.Next);

                    // push the ldarg.0 again   
                    cursor.Emit(OpCodes.Ldarg_0);
                }
            }

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(ForceMoveTime)))
            {
                cursor.Emit(OpCodes.Ldloc, shouldApplyModVar);
                cursor.Emit(OpCodes.Ldc_R4, ModifiedForceMoveTime);
                cursor.EmitDelegate<Func<float, bool, float, float>>((origVal, shouldApply, modValue) =>
                {
                    return shouldApply ? modValue : origVal;
                });
            }

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(PlayerConstants.WallJumpSpeedH)))
            {
                cursor.Emit(OpCodes.Ldloc, shouldApplyModVar);
                cursor.EmitReference(wallJumpSpeedHMultiplier);
                cursor.EmitDelegate<Func<float, bool, float, float>>((orig, shouldApply, mult) =>
                {
                    return shouldApply ? mult * orig : orig;
                });
            }

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(PlayerConstants.WallJumpSpeedV)))
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
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate(ShouldApplyWallJumpBoost);
            cursor.Emit(OpCodes.Stloc, shouldApplyModVar);

            // branch over if shouldn't apply
            var nextInstruction = cursor.Next;
            cursor.Emit(OpCodes.Ldloc, shouldApplyModVar);
            cursor.Emit(OpCodes.Brfalse_S, nextInstruction);

            // apply force move timer
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Stfld, typeof(Player).GetField("forceMoveX", BindingFlags.NonPublic | BindingFlags.Instance));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldc_R4, ModifiedForceMoveTime);
            cursor.Emit(OpCodes.Stfld, typeof(Player).GetField("forceMoveXTimer", BindingFlags.NonPublic | BindingFlags.Instance));


            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(PlayerConstants.WallBounceSpeedH)))
            {
                Logger.Log("HelloCeleste", $"WallBounce V speed changed to: {wallBounceSpeedHMultiplier}");

                cursor.Emit(OpCodes.Ldloc, shouldApplyModVar);
                cursor.EmitReference(wallBounceSpeedHMultiplier);
                cursor.EmitDelegate<Func<float, bool, float, float>>((orig, shouldApply, mult) =>
                {
                    return shouldApply ? mult * orig : orig;
                });
            }

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(PlayerConstants.WallBounceSpeedV)))
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

        private static bool ShouldApplyWallJumpBoost(Player self, bool isOnGround, JumpType jumpType, int jumpDir)
        {
            bool isClimbing = self.StateMachine.State == Player.StClimb;

            if (self == null || isOnGround && !isClimbing)
            {
                return false;
            }

            Solid wall = HookUtils.GetWall(self, jumpType, jumpDir);
            if (wall != null)
            {
                if (wall.GetType() == typeof(BouncyWall))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

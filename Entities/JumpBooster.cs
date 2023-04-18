using Celeste.Mod.Entities;
using Celeste.Mod.KisluHelper.Components.Constants;
using Celeste.Mod.KisluHelper.Components.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;


namespace Celeste.Mod.KisluHelper.Entities
{
    [CustomEntity("KisluHelper/JumpBooster")]
    public class JumpBooster : Solid
    {
        private const float JumpBoostMult = 2.0f;

        public JumpBooster(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, true)
        {
        }

        public override void Render()
        {
            Draw.Rect(X, Y, Width, Height, Color.Yellow);
        }

        public static void LoadHooks()
        {
            IL.Celeste.Player.Jump += ModJump;
            IL.Celeste.Player.SuperJump += ModJump;
        }

        public static void UnloadHooks()
        {
            IL.Celeste.Player.Jump -= ModJump;
            IL.Celeste.Player.SuperJump -= ModJump;
        }

        private static void ModJump(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(PlayerConstants.JumpSpeedV)))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(ApplyJumpBoost);
            }
        }

        private static float ApplyJumpBoost(float orig, Player self)
        {
            var ground = HookUtils.GetGround(self);
            if (ground != null)
            {
                if (ground.GetType() == typeof(JumpBooster))
                {
                    return orig * JumpBoostMult;
                }
            }

            return orig;
        }
    }
}

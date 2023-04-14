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

        private bool isColliding;

        private PlayerCollider playerCollider;

        private const float initWallJumpSpeedH = 130f;

        private const float initWallJumpSpeedV = -105f;

        private const float initWallBounceSpeedH = 170f;

        private const float initWallBounceSpeedV = -160f;

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
            if (GetFunnySettings())
            {
                c = Color.GreenYellow;
            }
            Draw.Rect(base.X, base.Y, base.Width, base.Height, c);
        }

        public bool GetFunnySettings()
        {
            return KisluHelperModule.Settings.FunnySwitch;
        }

        public override void Update()
        {
            base.Update();

            //Player player = CollideFirst<Player>(collider);
            //if ()
            //{

            //}
            //bool flag = this.DashAttacking && this.DashDir.X == 0f && this.DashDir.Y == -1f;
            Player player = CollideFirst<Player>(Position - Vector2.UnitX * 5.0f);
            if (player == null)
            {
                player = CollideFirst<Player>(Position + Vector2.UnitX * 5.0f);
            }
            if (player != null && !isColliding)
            {
                isColliding = true;
                LoadHooks();
            }
            else if (player == null && isColliding)
            {
                if (isColliding)
                {
                    UnloadHooks();
                }
                isColliding = false;
            }
        }

        private void LoadHooks()
        {
            Logger.Log("KisluHelper/GoodbyeEntity", "Entered GoodbyeEntity");
            //IL.Celeste.Player.Jump += ModJump;
            IL.Celeste.Player.SuperWallJump += ModWallbounceJump;
            wallJumpHook = new ILHook(typeof(Player).GetMethod("orig_WallJump", BindingFlags.Instance | BindingFlags.NonPublic), ModWallJump);
        }

        private void UnloadHooks()
        {
            Logger.Log("KisluHelper/GoodbyeEntity", "Left GoodbyeEntity");
            //IL.Celeste.Player.Jump -= ModJump;
            IL.Celeste.Player.SuperWallJump -= ModWallbounceJump;
            if (wallJumpHook != null)
            {
                wallJumpHook.Dispose();
            }
        }

        //private static void ModJump(ILContext il)
        //{
        //    ILCursor cursor = new ILCursor(il);

        //    if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(-105f)))
        //    {
        //        cursor.EmitDelegate<Func<float>>(getVerticalJumpMod);
        //        cursor.Emit(OpCodes.Mul);
        //    }
        //}

        private static void ModWallJump(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(initWallJumpSpeedH)))
            {
                cursor.EmitDelegate<Func<float>>(getHorizontalJumpMod);
                cursor.Emit(OpCodes.Mul);
            }

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(initWallJumpSpeedV)))
            {
                cursor.EmitDelegate<Func<float>>(getVerticalJumpMod);
                cursor.Emit(OpCodes.Mul);
            }
        }

        private static void ModWallbounceJump(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(initWallBounceSpeedH)))
            {
                cursor.EmitDelegate<Func<float>>(getHorizontalWallJumpMod);
                cursor.Emit(OpCodes.Mul);
            }

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(initWallBounceSpeedV)))
            {
                cursor.EmitDelegate<Func<float>>(getVerticalJumpMod);
                cursor.Emit(OpCodes.Mul);
            }
        }

        private static float getVerticalJumpMod()
        {
            return 0.1f;
        }

        private static float getHorizontalJumpMod()
        {
            return 2.0f;
        }

        private static float getHorizontalWallJumpMod()
        {
            // modify wallbounce horizontal speed to the same as walljump horizontal speed
            float ratio = initWallJumpSpeedH / initWallBounceSpeedH;
            return ratio * getHorizontalJumpMod();
        }
    }
}

using Celeste.Mod.Entities;
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
        private bool isColliding = false;

        public JumpBooster(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, true)
        {
        }

        private void OnCollide(Player player)
        {
            Logger.Log("KisluHelper/HelloEntity", "Collided");
            //player.Speed.X *= 1.1f;
        }

        public override void Render()
        {
            Draw.Rect(base.X, base.Y, base.Width, base.Height, Color.Yellow);
        }

        public override void Update()
        {
            base.Update();
            Player player = CollideFirst<Player>(Position - Vector2.UnitY);
            if (player != null && !isColliding)
            {
                isColliding = true;
                LoadHooks();

                //this.OnCollide(player);
                //global::Celeste.Celeste.Freeze(0.1f);
                //player.Bounce(Position.Y - Vector2.UnitY.Y);
                //player.MoveTowardsX(player.X - 1, 10.0f);
                //player.Jump();
                //player.HiccupJump();
                //player.Speed.Y += -200f;
                //player.Jump();
                //player.StateMachine.State = 0;
                //player.Speed.Y = -250f;
                //player.StartJumpGraceTime();
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
            Logger.Log("KisluHelper/HelloEntity", "Entered HelloEntity");
            IL.Celeste.Player.Jump += ModJump;
            IL.Celeste.Player.SuperJump += ModJump;
        }

        private void UnloadHooks()
        {
            Logger.Log("KisluHelper/HelloEntity", "Left HelloEntity");
            IL.Celeste.Player.Jump -= ModJump;
            IL.Celeste.Player.SuperJump -= ModJump;
        }

        private static void ModJump(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(-105f)))
            {
                cursor.EmitDelegate<Func<float>>(getJumpMod);
                cursor.Emit(OpCodes.Mul);
            }
        }

        private static float getJumpMod()
        {
            return 2.0f;
        }
    }
}

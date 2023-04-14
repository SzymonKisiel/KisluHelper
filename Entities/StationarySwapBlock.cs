using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Celeste.Mod.KisluHelper.Entities
{
    [CustomEntity("KisluHelper/StationarySwapBlock")]
    public class StationarySwapBlock : Solid
    {

        private bool isActive;
        //private List<StationarySwapBlock> group;
        private MTexture mTexture;
        private MTexture mTexture2;
        private MTexture mTexture3;
        private MTexture[,] nineSliceGreen;
        private MTexture[,] nineSliceRed;
        private MTexture[,] nineSliceTarget;
        private Sprite middleGreen;
        private Sprite middleRed;
        private Color disabledStaticMoverColor;

        public StationarySwapBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, true)
        {
            Depth = 100000;
            isActive = data.Bool("activeOnStart", true);

            Logger.Log("KisluHelper/StationarySwapBlock", $"activeOnStart: {isActive}");
            Add(new DashListener
            {
                OnDash = new Action<Vector2>(OnDash)
            });
            //Add(new StaticMover
            //{

            //});

            mTexture = GFX.Game["objects/swapblock/block"];
            mTexture2 = GFX.Game["objects/swapblock/blockRed"];
            mTexture3 = GFX.Game["objects/swapblock/target"];
            nineSliceGreen = new MTexture[3, 3];
            nineSliceRed = new MTexture[3, 3];
            nineSliceTarget = new MTexture[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    nineSliceGreen[i, j] = mTexture.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                    nineSliceRed[i, j] = mTexture2.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                    nineSliceTarget[i, j] = mTexture3.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                }
            }
            Add(middleGreen = GFX.SpriteBank.Create("swapBlockLight"));
            Add(middleRed = GFX.SpriteBank.Create("swapBlockLightRed"));

            disabledStaticMoverColor = Color.DarkGray;
            //Add(new LightOcclude(0.2f));
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);

            foreach (StaticMover staticMover in staticMovers)
            {
                if (staticMover.Entity is Spikes spikes)
                {
                    //spikes.EnabledColor = this.color;
                    spikes.DisabledColor = disabledStaticMoverColor;
                    spikes.VisibleWhenDisabled = true;
                    //spikes.SetSpikeColor(this.color);
                }
                if (staticMover.Entity is Spring spring)
                {
                    spring.DisabledColor = disabledStaticMoverColor;
                    spring.VisibleWhenDisabled = true;
                }
            }
        }

        public override void Update()
        {
            base.Update();

            if (isActive && !Collidable)
            {
                var isBlocked = BlockedCheck();
                if (!isBlocked)
                {
                    Collidable = true;
                    EnableStaticMovers();
                }
            }
            else if (!isActive)
            {
                Collidable = false;
                DisableStaticMovers();
            }
        }

        public override void Render()
        {
            if (Collidable)
            {
                Draw.Rect(base.X, base.Y, base.Width, base.Height, Color.Green);
            }
            else
            {
                if (isActive)
                {
                    Draw.HollowRect(base.X, base.Y, base.Width, base.Height, Color.Yellow);
                }
                else
                {
                    Draw.HollowRect(base.X, base.Y, base.Width, base.Height, Color.Red);
                }
            }

            //Color c = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            //Color c1 = new Color(1.0f, 1.0f, 1.0f, 0.7f);
            //Color c2 = new Color(1.0f, 1.0f, 1.0f, 0.01f);

            //if (Collidable)
            //{
            //    DrawBlockStyle(new Vector2(X, Y), Width, Height, nineSliceGreen, middleGreen, c);
            //}
            //else
            //{
            //    if (isActive)
            //    {
            //        DrawBlockStyle(new Vector2(X, Y), Width, Height, nineSliceGreen, middleGreen, c1);
            //    }
            //    else
            //    {
            //        DrawBlockStyle(new Vector2(X, Y), Width, Height, nineSliceRed, middleRed, c2);
            //    }
            //}
        }

        public void OnDash(Vector2 dashDirection)
        {
            isActive = !isActive;
            //if (isActive)
            //{
            //    //this.Collidable = true;
            //}
            //else
            //{
            //    this.Collidable = false;
            //}
        }

        private bool BlockedCheck()
        {
            //TheoCrystal theoCrystal = base.CollideFirst<TheoCrystal>();
            //if (theoCrystal != null && !this.TryActorWiggleUp(theoCrystal))
            //{
            //    return true;
            //}
            Player player = CollideFirst<Player>();
            if (player != null)
            {
                //return !TryActorWiggleUp(player) && !TryActorWiggleLeft(player, 1) && !TryActorWiggleLeft(player, -1);
                return !TryActorWiggle(player, -Vector2.UnitY, 4) &&
                    !TryActorWiggle(player, Vector2.UnitX, 2) &&
                    !TryActorWiggle(player, -Vector2.UnitX, 2);
            }
            else
            {
                return false;
            }
        }

        private bool TryActorWiggle(Entity actor, Vector2 dir, int range)
        {
            this.Collidable = true;
            for (int i = 1; i <= range; i++)
            {
                if (!actor.CollideCheck<Solid>(actor.Position + dir * (float)i))
                {
                    actor.Position += dir * (float)i;
                    Logger.Log("KisluHelper/StationarySwapBlock", $"Wiggled to {dir} by {i}");
                    Collidable = true;
                    //EnableStaticMovers();
                    return true;
                }
            }
            Collidable = false;
            //DisableStaticMovers();
            return false;
        }

        private void DrawBlockStyle(Vector2 pos, float width, float height, MTexture[,] ninSlice, Sprite middle, Color color)
        {
            int num = (int)(width / 8f);
            int num2 = (int)(height / 8f);
            ninSlice[0, 0].Draw(pos + new Vector2(0f, 0f), Vector2.Zero, color);
            ninSlice[2, 0].Draw(pos + new Vector2(width - 8f, 0f), Vector2.Zero, color);
            ninSlice[0, 2].Draw(pos + new Vector2(0f, height - 8f), Vector2.Zero, color);
            ninSlice[2, 2].Draw(pos + new Vector2(width - 8f, height - 8f), Vector2.Zero, color);
            for (int i = 1; i < num - 1; i++)
            {
                ninSlice[1, 0].Draw(pos + new Vector2(i * 8, 0f), Vector2.Zero, color);
                ninSlice[1, 2].Draw(pos + new Vector2(i * 8, height - 8f), Vector2.Zero, color);
            }
            for (int j = 1; j < num2 - 1; j++)
            {
                ninSlice[0, 1].Draw(pos + new Vector2(0f, j * 8), Vector2.Zero, color);
                ninSlice[2, 1].Draw(pos + new Vector2(width - 8f, j * 8), Vector2.Zero, color);
            }
            for (int k = 1; k < num - 1; k++)
            {
                for (int l = 1; l < num2 - 1; l++)
                {
                    ninSlice[1, 1].Draw(pos + new Vector2(k, l) * 8f, Vector2.Zero, color);
                }
            }
            if (middle != null)
            {
                middle.Color = color;
                middle.RenderPosition = pos + new Vector2(width / 2f, height / 2f);
                middle.Render();
            }
        }
    }

}

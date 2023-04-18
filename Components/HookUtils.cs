﻿using Celeste.Mod.KisluHelper.Components.Enums;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.KisluHelper.Components
{
    public static class HookUtils
    {
        private const int WallJumpCheckDist = 3;

        private const int SuperWallJumpCheckDist = 5;

        public static Solid GetWall(Player self, JumpType jumpType, int jumpDir)
        {
            Solid solid = null;

            float checkDist = WallJumpCheckDist;
            float checkDir;

            switch (jumpType)
            {
                case JumpType.ClimbJump:
                    checkDir = (float)self.Facing;
                    solid = self.CollideFirst<Solid>(self.Position + checkDir * Vector2.UnitX * checkDist);
                    if (solid == null)
                    {
                        solid = self.CollideFirst<Solid>(self.Position - checkDir * Vector2.UnitX * checkDist);
                    }
                    break;
                case JumpType.WallJump:
                    checkDir = -jumpDir;
                    solid = self.CollideFirst<Solid>(self.Position + checkDir * Vector2.UnitX * checkDist);
                    break;
                case JumpType.SuperWallJump:
                    checkDist = SuperWallJumpCheckDist;
                    goto case JumpType.WallJump;
            }

            return solid;
        }
    }
}
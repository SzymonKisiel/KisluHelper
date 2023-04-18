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
        internal static void Load()
        {
            WallJumpBooster.LoadHooks();
            BouncyWall.LoadHooks();
        }

        internal static void Unload()
        {
            WallJumpBooster.UnloadHooks();
            BouncyWall.UnloadHooks();
        }
    }
}

using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.KisluHelper {
    public class KisluHelperModule : EverestModule {
        public static KisluHelperModule Instance { get; private set; }

        public override Type SettingsType => typeof(KisluHelperModuleSettings);
        public static KisluHelperModuleSettings Settings => (KisluHelperModuleSettings) Instance._Settings;

        public override Type SessionType => typeof(KisluHelperModuleSession);
        public static KisluHelperModuleSession Session => (KisluHelperModuleSession) Instance._Session;

        public KisluHelperModule() {
            Instance = this;
#if DEBUG
            // debug builds use verbose logging
            Logger.SetLogLevel(nameof(KisluHelperModule), LogLevel.Verbose);
#else
            // release builds use info logging to reduce spam in log files
            Logger.SetLogLevel(nameof(KisluHelperModule), LogLevel.Info);
#endif
        }

        public override void Load() {
            Hooks.Load();
        }

        public override void Unload() {
            Hooks.Unload();
        }
    }
}
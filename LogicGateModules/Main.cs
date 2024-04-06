using ModLoader;
using HarmonyLib;

namespace LogicGateModules
{
    public class Main : Mod
    {
        public override string ModNameID => "LogicGateModules";
        public override string DisplayName => "Logic Gate Mod Modules";
        public override string ModVersion => "v1.0";
        public override string Author => "N2O4, Fusion";
        public override string MinimumGameVersionNecessary => "1.5.10.2";
        public override string Description => "Modules for the Logic Gate Mod";

        public override void Early_Load()
        {
            new Harmony("com.fusion.logic-gates").PatchAll();
        }
    }
}

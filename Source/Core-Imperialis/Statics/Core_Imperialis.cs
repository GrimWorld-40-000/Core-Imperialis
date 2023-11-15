using HarmonyLib;
using Verse;

namespace Core_Imp
{
    public class Core_Imperialis : Mod
    {
        private const string ModName = "Core_Imperialis.Mod";

        public Core_Imperialis(ModContentPack content) : base(content)
        {
            new Harmony(ModName).PatchAll();
        }
    }
}
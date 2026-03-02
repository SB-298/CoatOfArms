using HarmonyLib;
using Verse;

namespace CoatOfArms;

[StaticConstructorOnStartup]
public static class Core
{
    static Core()
    {
        new Harmony("kiero298.coatofarms").PatchAll();
    }
}

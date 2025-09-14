using HarmonyLib;
using RimWorld;
using Verse;
using System;
using System.Reflection;

namespace UntilNutrition;

[StaticConstructorOnStartup]
public class HarmonyPatches
{
    static HarmonyPatches()
    {
        var harmony = new Harmony("llunak.UntilNutrition");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}

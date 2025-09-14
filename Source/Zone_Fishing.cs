using HarmonyLib;
using RimWorld;
using Verse;
using System;

namespace UntilNutrition;

[HarmonyPatch(typeof(Zone_Fishing))]
public static class Zone_Fishing_Patch
{
    // Patching OwnedFishCount will make the cost use our count (if active),
    // since we set the zone's repeat mode to TargetCount.
    [HarmonyPrefix]
    [HarmonyPatch(nameof(OwnedFishCount))]
    [HarmonyPatch(MethodType.Getter)]
    public static bool OwnedFishCount(ref int __result, Zone_Fishing __instance)
    {
        ZoneFishingData data = ZoneFishingData.Get( __instance );
        if( !data.active )
            return true;
        __result = data.NutritionCount( __instance.Map );
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ExposeData))]
    public static void ExposeData( Zone_Fishing __instance )
    {
        ZoneFishingData.Get( __instance ).ExposeData();
    }
}

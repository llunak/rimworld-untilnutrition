using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Profile;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UntilNutrition;

public class ZoneFishingData
{
    // Use Zone_Fishing.targetCount for the 'until X' count.
    public bool active = false;
    public ThingFilter filter = new ThingFilter();

    private static Dictionary< Zone_Fishing, ZoneFishingData > dict = new Dictionary< Zone_Fishing, ZoneFishingData >();

    public static ZoneFishingData Get( Zone_Fishing zone )
    {
        ZoneFishingData data;
        if( dict.TryGetValue( zone, out data ))
            return data;
        data = new ZoneFishingData();
        dict[ zone ] = data;
        return data;
    }

    public int NutritionCount( Map map )
    {
        float count = map.listerThings.ThingsMatchingFilter( filter ).Sum((Thing t) => NutritionCount( t )) + CarriedNutritionCount( map );
        return (int)count;
    }

    private float CarriedNutritionCount( Map map )
    {
        float num = 0;
        foreach (Pawn item in map.mapPawns.FreeColonistsSpawned)
        {
            Thing carriedThing = item.carryTracker.CarriedThing;
            if (carriedThing != null && filter.Allows(carriedThing.def) && !carriedThing.Spawned)
                num += NutritionCount(carriedThing);
        }
        return num;
    }

    private float NutritionCount(Thing thing)
    {
        return thing.GetStatValue(StatDefOf.Nutrition, cacheStaleAfterTicks : 100) * thing.stackCount;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref active, "UntilNutrition.active", false);
        Scribe_Deep.Look(ref filter, "UntilNutrition.filter");
        if (Scribe.mode == LoadSaveMode.LoadingVars && filter == null )
            filter = new ThingFilter();
    }

    public static void Remove( Zone_Fishing zone )
    {
        dict.Remove( zone );
    }

    public static void ClearAll()
    {
        dict.Clear();
    }
}

[HarmonyPatch(typeof(ZoneManager))]
public static class ZoneManager_Patch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(DeregisterZone))]
    public static void DeregisterZone(Zone oldZone)
    {
        if( oldZone is Zone_Fishing zone )
            ZoneFishingData.Remove( zone );
    }
}

[HarmonyPatch(typeof(MemoryUtility))]
public static class MemoryUtility_Patch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ClearAllMapsAndWorld))]
    public static void ClearAllMapsAndWorld()
    {
        ZoneFishingData.ClearAll();
    }
}

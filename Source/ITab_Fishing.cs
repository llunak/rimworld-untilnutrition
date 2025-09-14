using HarmonyLib;
using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace UntilNutrition;

[HarmonyPatch(typeof(ITab_Fishing))]
public static class ITab_Fishing_Patch
{
    private const float EXTRA_HEIGHT = 40;

    [HarmonyPostfix]
    [HarmonyPatch(MethodType.Constructor)]
    public static void ctor( ITab_Fishing __instance )
    {
        __instance.size = new Vector2( __instance.size.x, __instance.size.y + EXTRA_HEIGHT );
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(FillTab))]
    public static IEnumerable<CodeInstruction> FillTab(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        bool found1 = false;
        bool found2 = false;
        bool found3 = false;
        bool found4 = false;
        for( int i = 0; i < codes.Count; ++i )
        {
            // The function has code:
            // Listing_Standard listing_Standard2 = listing_Standard.BeginSection(200f);
            // Change to:
            // Listing_Standard listing_Standard2 = listing_Standard.BeginSection(200f + 100);
            // TODO: Is it worth bothering to be flexible here?
            if( codes[i].opcode == OpCodes.Ldc_R4 && codes[i].operand.ToString() == "200")
            {
                codes[i].operand = 200f + EXTRA_HEIGHT;
                found1 = true;
            }
            // The function has code:
            // if (listing_Standard2.ButtonText(RepeatModeLabel(zone.repeatMode)))
            // Replace with:
            // if (listing_Standard2.ButtonText(FillTab_Hook1(RepeatModeLabel(zone.repeatMode))))
            if( codes[i].opcode == OpCodes.Ldfld && codes[i].operand.ToString() == "RimWorld.FishRepeatMode repeatMode"
                && i + 1 < codes.Count
                && codes[i + 1].opcode == OpCodes.Call
                && codes[i + 1].operand.ToString() == "System.String RepeatModeLabel(RimWorld.FishRepeatMode)")
            {
                codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldarg_0)); // load 'this'
                codes.Insert(i + 3, new CodeInstruction(OpCodes.Call, typeof(ITab_Fishing_Patch).GetMethod(nameof(FillTab_Hook1))));
                found2 = true;
            }
            // The function has code:
            // Find.WindowStack.Add(new FloatMenu(list));
            // Prepend:
            // ITab_Fishing_Patch.FillTab_Hook2(list);
            // Log.Message("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
            if( codes[i].IsLdloc()
                && i + 2 < codes.Count
                && codes[i + 1].opcode == OpCodes.Newobj
                && codes[i + 1].operand.ToString() == "Void .ctor(System.Collections.Generic.List`1[Verse.FloatMenuOption])"
                && codes[i + 2].opcode == OpCodes.Callvirt && codes[i + 2].operand.ToString() == "Void Add(Verse.Window)")
            {
                // Place after the load of 'list'.
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_0)); // load 'this'
                codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, typeof(ITab_Fishing_Patch).GetMethod(nameof(FillTab_Hook2))));
                found3 = true;
            }

            // The fuction has code:
            // listing_Standard.EndSection(listing_Standard2);
            // Change to:
            // listing_Standard.EndSection(FillTab_Hook3(listing_Standard2));
            // (This is the first call to EndSection(), so find it using that.)
            if( !found4 && codes[i].opcode == OpCodes.Callvirt && codes[i].operand.ToString() == "Void EndSection(Verse.Listing_Standard)")
            {
                codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_0)); // load 'this'
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, typeof(ITab_Fishing_Patch).GetMethod(nameof(FillTab_Hook3))));
                found4 = true;
            }

        }
        if(!found1 || !found2 || !found3)
            Log.Error("UntilNutrition: Failed to patch ITab_Fishing.FillTab()");
        return codes;
    }

    public static string FillTab_Hook1( string label, ITab_Fishing __instance )
    {
        ZoneFishingData data = ZoneFishingData.Get( __instance.SelZone );
        if( !data.active )
            return label;
        return "UntilNutrition.FishRepeatMode_UntilNutrition".Translate().CapitalizeFirst();
    }

    public static List<FloatMenuOption> FillTab_Hook2( List<FloatMenuOption> list, ITab_Fishing tab )
    {
        Zone_Fishing zone = tab.SelZone;
        ZoneFishingData data = ZoneFishingData.Get( zone );
        int insertIndex = -1;
        // Change other float options to reset our mode.
        for( int i = 0; i < list.Count; ++i )
        {
            FloatMenuOption option = list[ i ];
            // Put the new option after the 'Do until X' one.
            if( option.Label == tab.RepeatModeLabel( FishRepeatMode.TargetCount ))
                insertIndex = i + 1;
            Action originalAction = option.action;
            option.action = delegate
            {
                originalAction();
                data.active = false;
                // Also fix the edit field default values.
                tab.repeatCountEditBuffer = "100";
                tab.targetCountEditBuffer = "1";
            };
        }
        if( insertIndex == -1 ) // huh?
            insertIndex = list.Count - 1;
        list.Insert( insertIndex, new FloatMenuOption("UntilNutrition.FishRepeatMode_UntilNutrition".Translate().CapitalizeFirst(),
            delegate
            {
                zone.targetCount = 1;
                zone.repeatCount = 100;
                zone.pauseWhenSatisfied = false;
                zone.unpauseAtCount = 50;
                zone.repeatMode = FishRepeatMode.TargetCount; // So that we can reuse the code for this mode.
                data.active = true;
                // Also fix the edit field default value.
                tab.targetCountEditBuffer = "1";
                zone.RecheckPausedDueToResourceCount();
            }));
        return list;
    }

    public static Listing_Standard FillTab_Hook3( Listing_Standard listing, ITab_Fishing __instance )
    {
        Zone_Fishing zone = __instance.SelZone;
        ZoneFishingData data = ZoneFishingData.Get( zone );
        if( !data.active )
            return listing;
        listing.Gap( 10 );
        if( listing.ButtonText( "UntilNutrition.ItemsToCount".Translate()))
            Find.WindowStack.Add( new DialogFilter( zone, data ));
        return listing;
    }
}

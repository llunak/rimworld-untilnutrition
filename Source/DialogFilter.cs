using HarmonyLib;
using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UntilNutrition;

// Based on Dialog_BillConfig.

public class DialogFilter : Window
{
    private Zone_Fishing zone;
    private ZoneFishingData data;

    private ThingFilterUI.UIState thingFilterState = new ThingFilterUI.UIState();

    private readonly SpecialThingFilterDef[] hideFreshRotten = { Defs.AllowFresh, Defs.AllowRotten };

    public override Vector2 InitialSize => new Vector2(600f, 600f);

    public DialogFilter( Zone_Fishing zone, ZoneFishingData data )
    {
        this.zone = zone;
        this.data = data;
        forcePause = true;
        doCloseX = true;
        doCloseButton = true;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = true;
    }

    public override void PreOpen()
    {
        base.PreOpen();
        thingFilterState.quickSearch.Reset();
    }

    public override void PostClose()
    {
        zone.RecheckPausedDueToResourceCount();
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect rect =  new Rect( 0, 0, inRect.width, inRect.height - Window.CloseButSize.y * 1.5f );
        ThingFilterUI.DoThingFilterConfigWindow( rect, thingFilterState, data.filter, Dialog_ManageFoodPolicies.FoodGlobalFilter,
            forceHideHitPointsConfig : true, forceHideQualityConfig : true, forceHiddenFilters : hideFreshRotten );
    }
}

using RimWorld;
using Verse;

namespace UntilNutrition;

[DefOf]
public static class Defs
{
    public static SpecialThingFilterDef AllowFresh;
    public static SpecialThingFilterDef AllowRotten;

    static Defs()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(Defs));
    }
}

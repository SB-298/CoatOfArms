using Verse;

namespace CoatOfArms;

public class CoatOfArmsSettings : ModSettings
{
    public static int EmblemLimit = 20;
    public static int Resolution = 128;
    /// <summary>Custom folder for PNG exports. Null or empty = use default (mod folder/Exports).</summary>
    public static string ExportFolderOverride = null;
    /// <summary>When true, the Edit Coat of Arms gizmo appears on non-player settlements without god mode.</summary>
    public static bool AllowEditOtherFactions = false;

    public static readonly int[] PredefinedResolutions = { 64, 128, 256, 512 };

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref EmblemLimit, "emblemLimit", 20);
        Scribe_Values.Look(ref Resolution, "resolution", 128);
        Scribe_Values.Look(ref ExportFolderOverride, "exportFolderOverride", null);
        Scribe_Values.Look(ref AllowEditOtherFactions, "allowEditOtherFactions", false);
    }
}

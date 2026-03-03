using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace CoatOfArms;

[StaticConstructorOnStartup]
public class CoatOfArmsComponent : GameComponent
{
    public static CoatOfArmsComponent Instance;

    /// <summary>Player faction vanilla icon tint (brighter cyan so reverted icon is clearly visible).</summary>
    private static readonly Color PlayerFactionVanillaIconTint = new Color(0f, 212f / 255f, 255f / 255f, 1f);

    public CoatOfArmsData data = new CoatOfArmsData();
    public bool enabled;
    private Texture2D rendered;
    private bool dirty = true;
    private static Texture2D vanillaIcon;

    public Dictionary<string, CoatOfArmsData> otherFactionData = new Dictionary<string, CoatOfArmsData>();
    private Dictionary<string, Texture2D> factionTextures = new Dictionary<string, Texture2D>();
    private List<FactionCoatOfArmsEntry> otherFactionDataList = new List<FactionCoatOfArmsEntry>();

    public CoatOfArmsComponent(Game game)
    {
    }

    public Texture2D Rendered
    {
        get
        {
            if (dirty || rendered == null)
            {
                Rerender();
            }
            return rendered;
        }
    }

    public void MarkDirty()
    {
        dirty = true;
    }

    public void Rerender()
    {
        int resolution = CoatOfArmsSettings.Resolution;
        rendered = CoatOfArmsRenderer.Render(data, resolution);
        dirty = false;
    }

    public CoatOfArmsData GetDataForFaction(Faction faction)
    {
        if (faction == null || faction.IsPlayer)
            return data?.Clone() ?? new CoatOfArmsData();
        string key = faction.GetUniqueLoadID();
        if (otherFactionData.TryGetValue(key, out CoatOfArmsData stored))
            return stored.Clone();
        CoatOfArmsData fresh = new CoatOfArmsData();
        if (fresh.background.pattern == null)
            fresh.background.pattern = DefDatabase<BackgroundPatternDef>.GetNamedSilentFail("Solid");
        if (fresh.frame == null)
            fresh.frame = DefDatabase<FrameDef>.GetNamedSilentFail("Square");
        return fresh;
    }

    public void SetDataForFaction(Faction faction, CoatOfArmsData newData)
    {
        if (faction == null || newData == null)
            return;
        if (faction.IsPlayer)
        {
            data = newData;
            enabled = true;
            MarkDirty();
            Apply();
            return;
        }
        string key = faction.GetUniqueLoadID();
        otherFactionData[key] = newData;
        ApplyToFaction(faction);
    }

    public bool HasCustomCoatOfArms(Faction faction)
    {
        if (faction == null || faction.IsPlayer)
            return enabled;
        return otherFactionData.ContainsKey(faction.GetUniqueLoadID());
    }

    public Texture2D GetRenderedTexture(Faction faction)
    {
        if (faction == null)
            return null;
        if (faction.IsPlayer)
            return Rendered;
        string key = faction.GetUniqueLoadID();
        if (factionTextures.TryGetValue(key, out Texture2D texture))
            return texture;
        return null;
    }

    public void ApplyToFaction(Faction faction)
    {
        if (faction == null || faction.def == null)
            return;
        if (faction.IsPlayer)
        {
            Apply();
            return;
        }
        string key = faction.GetUniqueLoadID();
        if (!otherFactionData.TryGetValue(key, out CoatOfArmsData coatOfArms))
            return;
        if (coatOfArms == null)
            return;
        try
        {
            int resolution = CoatOfArmsSettings.Resolution;
            Texture2D texture = CoatOfArmsRenderer.Render(coatOfArms, resolution);
            if (texture != null)
                factionTextures[key] = texture;
        }
        catch (Exception ex)
        {
            Log.Warning("[CoatOfArms] ApplyToFaction failed for " + (faction?.Name ?? "null") + ": " + ex.Message);
        }
    }

    public void RevertFaction(Faction faction)
    {
        if (faction == null)
            return;
        if (faction.IsPlayer)
        {
            Revert();
            return;
        }
        string key = faction.GetUniqueLoadID();
        otherFactionData.Remove(key);
        factionTextures.Remove(key);
    }

    public void Apply()
    {
        LongEventHandler.ExecuteWhenFinished(delegate
        {
            if (vanillaIcon == null && Faction.OfPlayer?.def != null)
            {
                vanillaIcon = Faction.OfPlayer.def.factionIcon;
            }

            if (!enabled)
                return;

            Texture2D texture = Rendered;
            if (texture != null)
            {
                Faction.OfPlayer.def.factionIcon = texture;
            }
        });
    }

    public override void StartedNewGame()
    {
        Instance = this;
        base.StartedNewGame();
        factionTextures.Clear();
        LongEventHandler.ExecuteWhenFinished(delegate
        {
            ResetAllFactionDefIconsToVanilla();
        });
        SetDefaults();
    }

    public override void LoadedGame()
    {
        Instance = this;
        base.LoadedGame();
        factionTextures.Clear();

        LongEventHandler.ExecuteWhenFinished(delegate
        {
            ResetAllFactionDefIconsToVanilla();

            if (Faction.OfPlayer?.def != null)
                SetFactionIcon(Faction.OfPlayer.def, GetVanillaFactionIconWithTint(Faction.OfPlayer.def));

            Apply();
            ApplyOtherFactionIconsWhenReady();
        });
    }

    public override void FinalizeInit()
    {
        Instance = this;
        base.FinalizeInit();
        factionTextures.Clear();

        LongEventHandler.ExecuteWhenFinished(delegate
        {
            ResetAllFactionDefIconsToVanilla();

            if (Faction.OfPlayer?.def != null)
                SetFactionIcon(Faction.OfPlayer.def, GetVanillaFactionIconWithTint(Faction.OfPlayer.def));

            Apply();
            ApplyOtherFactionIconsWhenReady();
        });
    }

    private void ApplyOtherFactionIconsWhenReady()
    {
        LongEventHandler.ExecuteWhenFinished(delegate
        {
            if (Find.World?.factionManager?.AllFactions == null)
                return;
            foreach (Faction faction in Find.World.factionManager.AllFactions)
            {
                try
                {
                    if (faction != null && !faction.IsPlayer)
                        ApplyToFaction(faction);
                }
                catch (Exception ex)
                {
                    Log.Warning("[CoatOfArms] ApplyOtherFactionIconsWhenReady failed for faction: " + ex.Message);
                }
            }
        });
    }

    public void Revert()
    {
        enabled = false;
        if (Faction.OfPlayer?.def == null)
            return;
        Texture2D vanilla = GetVanillaFactionIconWithTint(Faction.OfPlayer.def);
        SetFactionIcon(Faction.OfPlayer.def, vanilla);
    }

    private static void SetFactionIcon(FactionDef factionDef, Texture2D texture)
    {
        if (factionDef == null)
            return;
        FieldInfo field = typeof(FactionDef).GetField("factionIcon",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            Log.Error("[CoatOfArms] FactionDef.factionIcon field not found. RimWorld may have changed.");
            return;
        }
        field.SetValue(factionDef, texture);
    }

    private static Texture2D GetVanillaFactionIcon(FactionDef factionDef)
    {
        if (factionDef == null)
            return BaseContent.BadTex;
        if (!factionDef.factionIconPath.NullOrEmpty())
        {
            Texture2D texture = ContentFinder<Texture2D>.Get(factionDef.factionIconPath);
            if (texture != null)
                return texture;
        }
        return BaseContent.BadTex;
    }

    /// <summary>Vanilla icon with faction color applied for player defs, so reverted icon is cyan not white.</summary>
    private static Texture2D GetVanillaFactionIconWithTint(FactionDef factionDef)
    {
        Texture2D vanilla = GetVanillaFactionIcon(factionDef);
        if (vanilla == null || vanilla == BaseContent.BadTex)
            return vanilla;
        if (factionDef.isPlayer)
        {
            Texture2D tinted = CoatOfArmsRenderer.TintTexture(vanilla, PlayerFactionVanillaIconTint);
            if (tinted != null)
                return tinted;
        }
        return vanilla;
    }

    /// <summary>Resets every FactionDef's icon to vanilla so icons do not leak across saves.</summary>
    private static void ResetAllFactionDefIconsToVanilla()
    {
        foreach (FactionDef factionDef in DefDatabase<FactionDef>.AllDefs)
        {
            if (factionDef == null || factionDef.factionIconPath.NullOrEmpty())
                continue;
            Texture2D vanilla = GetVanillaFactionIconWithTint(factionDef);
            SetFactionIcon(factionDef, vanilla);
        }
    }

    public override void ExposeData()
    {
        Instance = this;
        base.ExposeData();
        Scribe_Deep.Look(ref data, "coatOfArmsData");
        Scribe_Values.Look(ref enabled, "enabled");

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            otherFactionDataList.Clear();
            foreach (KeyValuePair<string, CoatOfArmsData> pair in otherFactionData)
            {
                if (pair.Value != null && !string.IsNullOrEmpty(pair.Key))
                    otherFactionDataList.Add(new FactionCoatOfArmsEntry { factionId = pair.Key, coatOfArms = pair.Value });
            }
        }

        Scribe_Collections.Look(ref otherFactionDataList, "otherFactionCoatOfArms", LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            data ??= new CoatOfArmsData();
            otherFactionData = new Dictionary<string, CoatOfArmsData>();
            if (otherFactionDataList != null)
            {
                foreach (FactionCoatOfArmsEntry entry in otherFactionDataList)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.factionId) && entry.coatOfArms != null)
                        otherFactionData[entry.factionId] = entry.coatOfArms;
                }
            }
            otherFactionDataList = new List<FactionCoatOfArmsEntry>();
            MarkDirty();
        }
    }

    private void SetDefaults()
    {
        if (data.background.pattern == null)
        {
            data.background.pattern = DefDatabase<BackgroundPatternDef>.GetNamedSilentFail("Solid");
        }
        if (data.frame == null)
        {
            data.frame = DefDatabase<FrameDef>.GetNamedSilentFail("Square");
        }
    }
}

public class FactionCoatOfArmsEntry : IExposable
{
    public string factionId;
    public CoatOfArmsData coatOfArms;

    public void ExposeData()
    {
        Scribe_Values.Look(ref factionId, "factionId", "");
        Scribe_Deep.Look(ref coatOfArms, "coatOfArms");
    }
}

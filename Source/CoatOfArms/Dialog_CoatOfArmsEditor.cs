using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CoatOfArms;

public class Dialog_CoatOfArmsEditor : Window
{
    private enum Tab
    {
        Background,
        Emblems
    }

    private CoatOfArmsData working;
    private Texture2D preview;
    private Tab activeTab = Tab.Background;
    private int selectedLayer = -1;
    private bool dirty = true;
    private Faction targetFaction;

    private static readonly Vector2 ButtonSize = new Vector2(120f, 36f);

    public Dialog_CoatOfArmsEditor(Faction faction = null)
    {
        absorbInputAroundWindow = true;
        closeOnClickedOutside = false;
        forcePause = true;
        doCloseX = true;

        targetFaction = faction ?? Faction.OfPlayer;
        CoatOfArmsComponent component = CoatOfArmsComponent.Instance;
        working = component != null ? component.GetDataForFaction(targetFaction) : new CoatOfArmsData();
        RefreshPreview();
    }

    public override Vector2 InitialSize => new Vector2(900f, 700f);

    private void RefreshPreview()
    {
        preview = CoatOfArmsRenderer.Render(working, CoatOfArmsSettings.Resolution);
        dirty = false;
    }

    private void MarkDirty()
    {
        dirty = true;
    }

    public override void DoWindowContents(Rect rect)
    {
        if (dirty)
        {
            RefreshPreview();
        }

        float panelGap = 12f;
        float previewWidth = 260f;
        float tabBarHeight = 32f;

        Rect leftPane = new Rect(rect.x, rect.y, previewWidth, rect.height - ButtonSize.y - 8f);
        Rect rightPane = new Rect(
            rect.x + previewWidth + panelGap,
            rect.y,
            rect.width - previewWidth - panelGap,
            rect.height - ButtonSize.y - 8f
        );

        DrawLeftPane(leftPane);
        DrawRightPane(rightPane, tabBarHeight);
        DrawButtons(rect);
    }

    private void DrawLeftPane(Rect rect)
    {
        float previewSize = rect.width;
        Rect previewRect = new Rect(rect.x, rect.y, previewSize, previewSize);
        Panel_Preview.Draw(previewRect, working, preview);

        float layerTop = rect.y + previewSize + 8f;
        Rect layerRect = new Rect(rect.x, layerTop, rect.width, rect.yMax - layerTop);
        bool layerChanged = Panel_Layers.Draw(layerRect, working, ref selectedLayer, () => activeTab = Tab.Emblems, () => activeTab = Tab.Emblems);
        if (layerChanged)
        {
            MarkDirty();
        }
    }

    private void DrawRightPane(Rect rect, float tabBarHeight)
    {
        Rect tabBar = new Rect(rect.x, rect.y, rect.width, tabBarHeight);
        DrawTabBar(tabBar);

        Rect tabContent = new Rect(rect.x, rect.y + tabBarHeight + 4f, rect.width,
            rect.height - tabBarHeight - 4f);

        bool changed = false;

        switch (activeTab)
        {
            case Tab.Background:
                changed = Panel_Background.Draw(tabContent, working);
                break;
            case Tab.Emblems:
                changed = Panel_Emblems.Draw(tabContent, working, ref selectedLayer);
                break;
        }

        if (changed)
        {
            MarkDirty();
        }
    }

    private void DrawTabBar(Rect rect)
    {
        float tabWidth = rect.width / 2f;

        if (DrawTab(new Rect(rect.x, rect.y, tabWidth, rect.height),
                "CoA_TabBackground".Translate(), activeTab == Tab.Background))
        {
            activeTab = Tab.Background;
        }

        if (DrawTab(new Rect(rect.x + tabWidth, rect.y, tabWidth, rect.height),
                "CoA_TabEmblems".Translate(), activeTab == Tab.Emblems))
        {
            activeTab = Tab.Emblems;
        }
    }

    private bool DrawTab(Rect rect, string label, bool active)
    {
        if (active)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.3f, 0.3f, 0.3f));
        }
        else
        {
            Widgets.DrawBoxSolid(rect, new Color(0.18f, 0.18f, 0.18f));
            Widgets.DrawHighlightIfMouseover(rect);
        }

        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(rect, label);
        Text.Anchor = TextAnchor.UpperLeft;

        if (!active && Widgets.ButtonInvisible(rect))
        {
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
            return true;
        }

        return false;
    }

    private void DrawButtons(Rect rect)
    {
        float leftX = rect.x;
        const float buttonGap = 8f;

        Rect presetsRect = new Rect(leftX, rect.yMax - ButtonSize.y, 100f, ButtonSize.y);
        if (Widgets.ButtonText(presetsRect, "CoA_Presets".Translate()))
        {
            Find.WindowStack.Add(new Dialog_CoatOfArmsPresets(working, MarkDirty));
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        }
        leftX += 100f + buttonGap;

        Rect exportImportRect = new Rect(leftX, rect.yMax - ButtonSize.y, 120f, ButtonSize.y);
        if (Widgets.ButtonText(exportImportRect, "CoA_ExportImport".Translate()))
        {
            Find.WindowStack.Add(new Dialog_ExportImport(working, MarkDirty));
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        }
        leftX += 120f + buttonGap;

        Rect revertRect = new Rect(leftX, rect.yMax - ButtonSize.y, ButtonSize.x, ButtonSize.y);
        if (targetFaction != null && Widgets.ButtonText(revertRect, "CoA_Revert".Translate()))
        {
            CoatOfArmsComponent component = CoatOfArmsComponent.Instance;
            component?.RevertFaction(targetFaction);
            Close();
        }
        leftX += ButtonSize.x + buttonGap;

        float rightX = rect.xMax - ButtonSize.x;
        Rect cancelRect = new Rect(rightX - ButtonSize.x - buttonGap, rect.yMax - ButtonSize.y, ButtonSize.x, ButtonSize.y);
        if (Widgets.ButtonText(cancelRect, "CoA_Cancel".Translate()))
        {
            Close();
        }

        Rect acceptRect = new Rect(rightX, rect.yMax - ButtonSize.y, ButtonSize.x, ButtonSize.y);
        if (Widgets.ButtonText(acceptRect, "CoA_Apply".Translate()))
        {
            AcceptChanges();
        }
    }

    private void AcceptChanges()
    {
        CoatOfArmsComponent component = CoatOfArmsComponent.Instance;
        if (component != null && targetFaction != null)
        {
            component.SetDataForFaction(targetFaction, working);
        }

        Close();
    }

    public override void OnAcceptKeyPressed()
    {
        AcceptChanges();
        Event.current.Use();
    }
}

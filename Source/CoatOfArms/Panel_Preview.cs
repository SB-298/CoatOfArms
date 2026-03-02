using UnityEngine;
using Verse;

namespace CoatOfArms;

public static class Panel_Preview
{
    public static void Draw(Rect rect, CoatOfArmsData data, Texture2D rendered)
    {
        Widgets.DrawBoxSolid(rect, new Color(0.15f, 0.15f, 0.15f));
        Widgets.DrawBox(rect);

        if (rendered == null)
            return;

        float size = Mathf.Min(rect.width, rect.height) - 20f;
        Rect preview = new Rect(
            rect.x + (rect.width - size) * 0.5f,
            rect.y + (rect.height - size) * 0.5f,
            size,
            size
        );

        GUI.DrawTexture(preview, rendered);
    }
}

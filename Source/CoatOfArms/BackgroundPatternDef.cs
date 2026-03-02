using UnityEngine;
using Verse;

namespace CoatOfArms;

public class BackgroundPatternDef : Def
{
    public string texturePath;
    public int colorCount = 1;
    public int order;

    private Texture2D cached;

    public Texture2D Texture
    {
        get { return CoatOfArmsRenderer.GetOrLoadTexture(texturePath, ref cached); }
    }
}

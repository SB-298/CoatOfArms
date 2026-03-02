using UnityEngine;
using Verse;

namespace CoatOfArms;

public class FrameDef : Def
{
    public string texturePath;
    public int order;

    /// <summary>Uniform scale of the shape within the frame (1 = full size, &lt; 1 = smaller, &gt; 1 = bigger).</summary>
    public float maskScale = 1f;
    /// <summary>Horizontal scale; used for non-uniform scaling when set (e.g. banner).</summary>
    public float maskScaleX = 1f;
    /// <summary>Vertical scale; used for non-uniform scaling when set (e.g. banner).</summary>
    public float maskScaleY = 1f;

    private Texture2D cached;

    public Texture2D Texture
    {
        get { return CoatOfArmsRenderer.GetOrLoadTexture(texturePath, ref cached); }
    }
}

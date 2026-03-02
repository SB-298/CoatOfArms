using UnityEngine;
using Verse;

namespace CoatOfArms;

public class EmblemDef : Def
{
    public string texturePath;
    public string category;
    public int order;
    /// <summary>If set, this emblem is only available when the given mod/DLC is active. Use "Ideology" for Ideology DLC.</summary>
    public string modRequired;

    private Texture2D cached;
    private static int textureLoadsThisFrame;
    private static int textureLoadFrameId;

    /// <summary>Call once per frame before drawing the emblem grid. Resets the per-frame load counter.</summary>
    public static void BeginTextureLoadFrame()
    {
        int frame = Time.frameCount;
        if (frame != textureLoadFrameId)
        {
            textureLoadFrameId = frame;
            textureLoadsThisFrame = 0;
        }
    }

    /// <summary>Ensure texture is loaded. Returns true if texture is available (now or already). Loads at most maxPerFrame across all defs per frame.</summary>
    public bool TryLoadTexture(int maxPerFrame = 10)
    {
        if (cached != null)
            return true;
        if (texturePath.NullOrEmpty())
            return false;
        int frame = Time.frameCount;
        if (frame != textureLoadFrameId)
        {
            textureLoadFrameId = frame;
            textureLoadsThisFrame = 0;
        }
        if (textureLoadsThisFrame >= maxPerFrame)
            return false;
        bool hadCached = cached != null;
        CoatOfArmsRenderer.GetOrLoadTexture(texturePath, ref cached);
        if (cached != null && !hadCached)
            textureLoadsThisFrame++;
        return cached != null;
    }
    public bool IsAvailable()
    {
        if (modRequired.NullOrEmpty())
            return true;
        if (modRequired == "Ideology")
            return ModsConfig.IdeologyActive;
        if (modRequired == "Biotech")
            return ModsConfig.BiotechActive;
        if (modRequired == "Royalty")
            return ModsConfig.RoyaltyActive;
        return true;
    }

    /// <summary>True when the emblem is available for the current mod set and its texture loaded successfully.</summary>
    public bool IsAvailableAndLoaded()
    {
        return IsAvailable() && Texture != null;
    }

    /// <summary>True if the texture has been loaded (does not trigger loading).</summary>
    public bool IsTextureLoaded => cached != null;
    public Texture2D Texture
    {
        get { return CoatOfArmsRenderer.GetOrLoadTexture(texturePath, ref cached); }
    }
}

using UnityEngine;
using Verse;

namespace CoatOfArms;

public static class CoatOfArmsRenderer
{
    /// <summary>Baked secondary colour in mask PNGs (f8fc00) so images display correctly when viewed.</summary>
    private static readonly Color MaskSecondaryColor = new Color(248f / 255f, 252f / 255f, 0f, 1f);

    /// <summary>Loads and caches a texture from a def path; returns a readable texture. Used by FrameDef, BackgroundPatternDef, and EmblemDef.</summary>
    public static Texture2D GetOrLoadTexture(string path, ref Texture2D cached)
    {
        if (cached != null)
            return cached;
        if (path.NullOrEmpty())
            return null;
        Texture2D loaded = ContentFinder<Texture2D>.Get(path, false);
        if (loaded == null)
            return null;
        cached = loaded.isReadable ? loaded : RenderReadable(loaded);
        return cached;
    }

    public static Texture2D Render(CoatOfArmsData data, int resolution = 128)
    {
        if (data == null)
            return null;

        Texture2D result = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, false);
        result.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[resolution * resolution];

        if (data.frame != null)
        {
            RenderBackground(data.background, pixels, resolution);
        }

        foreach (EmblemLayer layer in data.emblems)
        {
            if (layer.emblem?.Texture == null)
                continue;
            RenderEmblem(layer, pixels, resolution);
        }

        ApplyFrameMask(data.frame, pixels, resolution);

        if (data.showOutline)
            ApplyOutline(pixels, resolution, data.outlineThickness);

        result.SetPixels(pixels);
        result.Apply();
        return result;
    }

    private static void RenderBackground(BackgroundLayer background, Color[] pixels, int resolution)
    {
        Texture2D mask = background.pattern?.Texture;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int index = y * resolution + x;

                if (mask == null || !mask.isReadable)
                {
                    pixels[index] = background.primary;
                    continue;
                }

                int mx = x * mask.width / resolution;
                int my = y * mask.height / resolution;
                Color sample = mask.GetPixel(mx, my);

                float weightR;
                float weightG;
                float weightB;
                if (MaskWeightFromSample(sample, out weightR, out weightG, out weightB))
                {
                    Color blended = background.primary * weightR
                                  + background.secondary * weightG
                                  + background.tertiary * weightB;
                    blended.a = 1f;
                    pixels[index] = blended;
                }
                else
                {
                    float total = sample.r + sample.g + sample.b;
                    if (total < 0.001f)
                    {
                        pixels[index] = background.primary;
                        continue;
                    }
                    weightR = sample.r / total;
                    weightG = sample.g / total;
                    weightB = sample.b / total;
                    Color blended = background.primary * weightR
                                  + background.secondary * weightG
                                  + background.tertiary * weightB;
                    blended.a = 1f;
                    pixels[index] = blended;
                }
            }
        }
    }

    private static bool MaskWeightFromSample(Color sample, out float weightR, out float weightG, out float weightB)
    {
        weightR = 0f;
        weightG = 0f;
        weightB = 0f;
        float r = sample.r;
        float g = sample.g;
        float b = sample.b;
        if (r > 0.99f && g < 0.01f && b < 0.01f)
        {
            weightR = 1f;
            return true;
        }
        if (g > 0.99f && r < 0.01f && b < 0.01f)
        {
            weightG = 1f;
            return true;
        }
        if (b > 0.99f && r < 0.01f && g < 0.01f)
        {
            weightB = 1f;
            return true;
        }
        if (Mathf.Abs(r - MaskSecondaryColor.r) < 0.02f && Mathf.Abs(g - MaskSecondaryColor.g) < 0.02f && Mathf.Abs(b - MaskSecondaryColor.b) < 0.02f)
        {
            weightG = 1f;
            return true;
        }
        return false;
    }

    private static void RenderEmblem(EmblemLayer layer, Color[] pixels, int resolution)
    {
        RenderEmblemWithOffset(layer, layer.color, pixels, resolution, 0f, 0f);
    }

    private static void RenderEmblemWithOffset(EmblemLayer layer, Color tintColor, Color[] pixels, int resolution,
        float pixelOffsetX, float pixelOffsetY)
    {
        Texture2D emblem = layer.emblem.Texture;
        if (!emblem.isReadable)
            return;

        float center = resolution * 0.5f;
        float offsetX = layer.position.x * resolution;
        float offsetY = layer.position.y * resolution;
        float radians = layer.rotation * Mathf.Deg2Rad;
        float cosA = Mathf.Cos(radians);
        float sinA = Mathf.Sin(radians);
        float effectiveScaleX = layer.scaleX != 1f ? layer.scaleX : layer.scale;
        float effectiveScaleY = layer.scaleY != 1f ? layer.scaleY : layer.scale;
        float invScaleX = 1f / Mathf.Max(effectiveScaleX, 0.01f);
        float invScaleY = 1f / Mathf.Max(effectiveScaleY, 0.01f);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float px = x - pixelOffsetX;
                float py = y - pixelOffsetY;
                float relX = px - center - offsetX;
                float relY = py - center - offsetY;

                float rotX = relX * cosA + relY * sinA;
                float rotY = -relX * sinA + relY * cosA;

                rotX *= invScaleX;
                rotY *= invScaleY;

                float u = (rotX + center) / resolution;
                float v = (rotY + center) / resolution;

                if (layer.flipX)
                    u = 1f - u;
                if (layer.flipY)
                    v = 1f - v;

                if (u < 0f || u > 1f || v < 0f || v > 1f)
                    continue;

                Color sample = emblem.GetPixelBilinear(u, v);
                Color tinted = new Color(
                    sample.r * tintColor.r,
                    sample.g * tintColor.g,
                    sample.b * tintColor.b,
                    sample.a * tintColor.a
                );

                if (tinted.a < 0.01f)
                    continue;

                int index = y * resolution + x;
                Color existing = pixels[index];
                float alpha = tinted.a;
                float inverse = 1f - alpha;

                pixels[index] = new Color(
                    existing.r * inverse + tinted.r * alpha,
                    existing.g * inverse + tinted.g * alpha,
                    existing.b * inverse + tinted.b * alpha,
                    Mathf.Max(existing.a, alpha)
                );
            }
        }
    }

    private static void ApplyFrameMask(FrameDef frame, Color[] pixels, int resolution)
    {
        Texture2D mask = frame?.Texture;
        if (mask == null)
            return;

        float scaleX = frame.maskScaleX != 1f ? frame.maskScaleX : frame.maskScale;
        float scaleY = frame.maskScaleY != 1f ? frame.maskScaleY : frame.maskScale;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float nx = (x + 0.5f) / resolution;
                float ny = (y + 0.5f) / resolution;
                float u = (nx - 0.5f) * scaleX + 0.5f;
                float v = (ny - 0.5f) * scaleY + 0.5f;

                if (u < 0f || u > 1f || v < 0f || v > 1f)
                {
                    pixels[y * resolution + x] = new Color(0f, 0f, 0f, 0f);
                    continue;
                }

                int mx = (int)(u * (mask.width - 1));
                int my = (int)(v * (mask.height - 1));
                if (mask.GetPixel(mx, my).a < 0.5f)
                {
                    pixels[y * resolution + x] = new Color(0f, 0f, 0f, 0f);
                }
            }
        }
    }

    private static void ApplyOutline(Color[] pixels, int resolution, float thicknessMultiplier)
    {
        int outlineThickness = Mathf.Max(1, Mathf.RoundToInt((resolution / 16f) * thicknessMultiplier));
        Color outlineColor = Color.black;
        Color[] read = new Color[pixels.Length];

        for (int ring = 0; ring < outlineThickness; ring++)
        {
            for (int i = 0; i < pixels.Length; i++)
                read[i] = pixels[i];

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int index = y * resolution + x;
                    if (read[index].a >= 0.01f)
                        continue;

                    bool hasVisibleOrOutlineNeighbor = false;
                    if (x > 0 && read[index - 1].a >= 0.01f) hasVisibleOrOutlineNeighbor = true;
                    if (x < resolution - 1 && read[index + 1].a >= 0.01f) hasVisibleOrOutlineNeighbor = true;
                    if (y > 0 && read[index - resolution].a >= 0.01f) hasVisibleOrOutlineNeighbor = true;
                    if (y < resolution - 1 && read[index + resolution].a >= 0.01f) hasVisibleOrOutlineNeighbor = true;

                    if (hasVisibleOrOutlineNeighbor)
                        pixels[index] = outlineColor;
                }
            }
        }
    }

    public static Texture2D RenderReadable(Texture2D source)
    {
        RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0);
        Graphics.Blit(source, temporary);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = temporary;
        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.ARGB32, false, false);
        readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        readable.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temporary);
        return readable;
    }

    /// <summary>Tints a texture by multiplying RGB by the given color. Used so reverted player faction icon shows cyan instead of white.</summary>
    public static Texture2D TintTexture(Texture2D source, Color tint)
    {
        if (source == null)
            return null;
        Texture2D readable = source.isReadable ? source : RenderReadable(source);
        Color[] pixels = readable.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color pixel = pixels[i];
            pixels[i] = new Color(pixel.r * tint.r, pixel.g * tint.g, pixel.b * tint.b, pixel.a);
        }
        Texture2D result = new Texture2D(readable.width, readable.height, TextureFormat.ARGB32, false, false);
        result.filterMode = FilterMode.Bilinear;
        result.SetPixels(pixels);
        result.Apply();
        if (readable != source)
            Object.Destroy(readable);
        return result;
    }
}

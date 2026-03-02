using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CoatOfArms;

public static class ColorPalette
{
    public static readonly List<Color> Colors = new List<Color>
    {
        new ColorInt(255, 255, 255).ToColor,
        new ColorInt(200, 200, 200).ToColor,
        new ColorInt(140, 140, 140).ToColor,
        new ColorInt(80, 80, 80).ToColor,
        new ColorInt(20, 20, 20).ToColor,

        new ColorInt(255, 50, 50).ToColor,
        new ColorInt(200, 30, 30).ToColor,
        new ColorInt(140, 15, 15).ToColor,

        new ColorInt(255, 180, 50).ToColor,
        new ColorInt(230, 140, 30).ToColor,
        new ColorInt(180, 100, 15).ToColor,

        new ColorInt(255, 255, 50).ToColor,
        new ColorInt(248, 252, 0).ToColor,
        new ColorInt(220, 200, 30).ToColor,
        new ColorInt(180, 160, 20).ToColor,

        new ColorInt(50, 200, 50).ToColor,
        new ColorInt(30, 160, 30).ToColor,
        new ColorInt(15, 110, 15).ToColor,

        new ColorInt(50, 120, 255).ToColor,
        new ColorInt(30, 90, 200).ToColor,
        new ColorInt(15, 60, 140).ToColor,

        new ColorInt(160, 50, 255).ToColor,
        new ColorInt(120, 30, 200).ToColor,
        new ColorInt(80, 15, 140).ToColor,

        new ColorInt(255, 50, 180).ToColor,
        new ColorInt(200, 30, 140).ToColor,
        new ColorInt(140, 15, 100).ToColor,

        new ColorInt(120, 70, 40).ToColor,
        new ColorInt(90, 50, 25).ToColor,
        new ColorInt(200, 160, 100).ToColor,
        new ColorInt(240, 210, 160).ToColor
    };
}

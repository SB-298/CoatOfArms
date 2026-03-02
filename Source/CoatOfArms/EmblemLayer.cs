using UnityEngine;
using Verse;

namespace CoatOfArms;

public class EmblemLayer : IExposable
{
    public EmblemDef emblem;
    public Color color = Color.white;
    public Vector2 position = Vector2.zero;
    public float rotation;
    public float scale = 1f;
    public float scaleX = 1f;
    public float scaleY = 1f;
    public bool flipX;
    public bool flipY;

    public void ExposeData()
    {
        Scribe_Defs.Look(ref emblem, "emblem");
        Scribe_Values.Look(ref color, "color");
        Scribe_Values.Look(ref position, "position");
        Scribe_Values.Look(ref rotation, "rotation");
        Scribe_Values.Look(ref scale, "scale", 1f);
        Scribe_Values.Look(ref scaleX, "scaleX", 1f);
        Scribe_Values.Look(ref scaleY, "scaleY", 1f);
        Scribe_Values.Look(ref flipX, "flipX");
        Scribe_Values.Look(ref flipY, "flipY");
    }

    public EmblemLayer Clone()
    {
        EmblemLayer copy = new EmblemLayer();
        copy.emblem = emblem;
        copy.color = color;
        copy.position = position;
        copy.rotation = rotation;
        copy.scale = scale;
        copy.scaleX = scaleX;
        copy.scaleY = scaleY;
        copy.flipX = flipX;
        copy.flipY = flipY;
        return copy;
    }
}

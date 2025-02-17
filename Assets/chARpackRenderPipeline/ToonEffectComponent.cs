using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[VolumeComponentMenuForRenderPipeline("Custom/Toon", typeof(UniversalRenderPipeline))]
public class ToonEffectComponent : VolumeComponent, IPostProcessComponent
{
    public bool IsActive()
    {
        return true;
    }

    public bool IsTileCompatible()
    {
        return false;
    }

    [Header("Outline Setings")]
    public FloatParameter outlineWidth = new FloatParameter(2f, true);
    public NoInterpColorParameter outlineColor = new NoInterpColorParameter(Color.black);
    public BoolParameter showOutline = new BoolParameter(true);

    [Header("Paint Setings")]
    public BoolParameter showPaint = new BoolParameter(true);

    [Header("Highlight Setings")]
    public NoInterpColorParameter highlightColor = new NoInterpColorParameter(Color.white);
    public BoolParameter showHighlight = new BoolParameter(true);
}

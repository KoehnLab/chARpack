using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public class ToonPostProcessRenderPass : ScriptableRenderPass
{

    ToonEffectComponent m_ToonEffect;
    private Material m_outlineMaterial;
    private Material m_paintMaterial;
    private Material m_highlightMaterial;

    private RTHandle _tempTexture1;
    private RTHandle _tempTexture2;

    private RTHandle m_cameraColorTarget;

    public ToonPostProcessRenderPass(Material outline_material, Material paint_material, Material highlights_material)
    {
        m_outlineMaterial = outline_material;
        m_paintMaterial = paint_material;
        m_highlightMaterial = highlights_material;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (m_cameraColorTarget.rt == null)
        {
            return;
        }
        if (m_outlineMaterial == null || m_paintMaterial == null || m_highlightMaterial == null) return;
        if (_tempTexture1 == null || _tempTexture2 == null) return;

        CommandBuffer cmd = CommandBufferPool.Get();

        VolumeStack stack = VolumeManager.instance.stack;
        m_ToonEffect = stack.GetComponent<ToonEffectComponent>();

        using (new ProfilingScope(cmd, new ProfilingSampler("Toon Post Process Effect")))
        {
            m_outlineMaterial.SetFloat("_OutlineWidth", m_ToonEffect.outlineWidth.value);
            m_outlineMaterial.SetColor("_OutlineColor", m_ToonEffect.outlineColor.value);

            m_highlightMaterial.SetColor("_HighlightColor", m_ToonEffect.highlightColor.value);

            if (m_ToonEffect.showOutline.value && m_ToonEffect.showPaint.value && m_ToonEffect.showHighlight.value)
            {
                Blitter.BlitCameraTexture(cmd, m_cameraColorTarget, _tempTexture1, m_outlineMaterial, 0);
                Blitter.BlitCameraTexture(cmd, _tempTexture1, _tempTexture2, m_paintMaterial, 0);
                Blitter.BlitCameraTexture(cmd, _tempTexture2, m_cameraColorTarget, m_highlightMaterial, 0);
            }
            else if (!m_ToonEffect.showOutline.value && m_ToonEffect.showPaint.value && m_ToonEffect.showHighlight.value)
            {
                Blitter.BlitCameraTexture(cmd, m_cameraColorTarget, _tempTexture2, m_paintMaterial, 0);
                Blitter.BlitCameraTexture(cmd, _tempTexture2, m_cameraColorTarget, m_highlightMaterial, 0);
            }
            else if (m_ToonEffect.showOutline.value && !m_ToonEffect.showPaint.value && m_ToonEffect.showHighlight.value)
            {
                Blitter.BlitCameraTexture(cmd, m_cameraColorTarget, _tempTexture1, m_outlineMaterial, 0);
                Blitter.BlitCameraTexture(cmd, _tempTexture1, m_cameraColorTarget, m_highlightMaterial, 0);
            }
            else if (m_ToonEffect.showOutline.value && m_ToonEffect.showPaint.value && !m_ToonEffect.showHighlight.value)
            {
                Blitter.BlitCameraTexture(cmd, m_cameraColorTarget, _tempTexture1, m_outlineMaterial, 0);
                Blitter.BlitCameraTexture(cmd, _tempTexture1, m_cameraColorTarget, m_paintMaterial, 0);
            }
            else if (!m_ToonEffect.showOutline.value && !m_ToonEffect.showPaint.value && m_ToonEffect.showHighlight.value)
            {
                Blitter.BlitCameraTexture(cmd, m_cameraColorTarget, m_cameraColorTarget, m_highlightMaterial, 0);
            }
            else if (!m_ToonEffect.showOutline.value && m_ToonEffect.showPaint.value && !m_ToonEffect.showHighlight.value)
            {
                Blitter.BlitCameraTexture(cmd, m_cameraColorTarget, m_cameraColorTarget, m_paintMaterial, 0);
            }
            else if (m_ToonEffect.showOutline.value && !m_ToonEffect.showPaint.value && !m_ToonEffect.showHighlight.value)
            {
                Blitter.BlitCameraTexture(cmd, m_cameraColorTarget, m_cameraColorTarget, m_outlineMaterial, 0);
            }

        }

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;

        if (_tempTexture1 == null || !_tempTexture1.rt)
            _tempTexture1 = RTHandles.Alloc(descriptor, name: "_TempToonTex1");

        if (_tempTexture2 == null || !_tempTexture2.rt)
            _tempTexture2 = RTHandles.Alloc(descriptor, name: "_TempToonTex2");

        // Ensure the source texture is not null
        m_cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
        if (m_cameraColorTarget == null)
        {
            Debug.LogError("Source RTHandle is null in ToonPostProcessRenderPass!");
        }
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        // No need to manually release RTHandle; Unity handles cleanup automatically.
    }

    public void SetTarget(RTHandle cameraColorTargetHandle)
    {
        m_cameraColorTarget = cameraColorTargetHandle;
    }
}

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class ToonRenderPostProcessRenderFeature : ScriptableRendererFeature
{
    [SerializeField]
    private Shader m_toonOutlineShader;
    private Material m_toonOutlineMaterial;
    [SerializeField]
    private Shader m_toonPaintShader;
    private Material m_toonPaintMaterial;
    [SerializeField]
    private Shader m_toonHighlightShader;
    private Material m_toonHighlightMaterial;

    private ToonPostProcessRenderPass m_toonRenderPass;

    public override void Create()
    {
        m_toonOutlineMaterial = CoreUtils.CreateEngineMaterial(m_toonOutlineShader);
        m_toonPaintMaterial = CoreUtils.CreateEngineMaterial(m_toonPaintShader);
        m_toonHighlightMaterial = CoreUtils.CreateEngineMaterial(m_toonHighlightShader);

        m_toonRenderPass = new ToonPostProcessRenderPass(m_toonOutlineMaterial, m_toonPaintMaterial, m_toonHighlightMaterial)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_toonRenderPass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            m_toonRenderPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            m_toonRenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
            m_toonRenderPass.ConfigureInput(ScriptableRenderPassInput.Normal);
            m_toonRenderPass.SetTarget(renderer.cameraColorTargetHandle);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (m_toonOutlineMaterial != null)
        {
            CoreUtils.Destroy(m_toonOutlineMaterial);
        }
        if (m_toonPaintMaterial != null)
        {
            CoreUtils.Destroy(m_toonPaintMaterial);
        }
        if (m_toonHighlightMaterial != null)
        {
            CoreUtils.Destroy(m_toonHighlightMaterial);
        }
    }

}
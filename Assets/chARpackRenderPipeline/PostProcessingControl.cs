using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace chARpack
{
    public class PostProcessingControl : MonoBehaviour
    {
        private static PostProcessingControl _singleton;
        public static PostProcessingControl Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                {
                    _singleton = value;
                }
                else if (_singleton != value)
                {
                    Debug.Log($"[{nameof(PostProcessingControl)}] Instance already exists, destroying duplicate!");
                    Destroy(value.gameObject);
                }

            }
        }

        private bool useToonShading;
        public bool UseToonShading { get { return useToonShading; }
                        set { if (foundRendererFeatures) { 
                                        setToonShading(value); 
                                        useToonShading = value; } }
        }

        [SerializeField]
        private float ToonOutlineWidth = 2.0f;
        public Color ToonOutlineColor = Color.black;

        private ScriptableRendererFeature ToonPaintRendererFeature;
        private ScriptableRendererFeature ToonOutlineRendererFeature;
        private ScriptableRendererFeature ToonHighlightsRendererFeature;

        private bool foundRendererFeatures;
        private Shader universalLit;
        private Shader universalUnlit;
        private Shader bondLit;
        private Shader bondLitTransparent;
        private Shader bondUnlit;

        private Material ToonOutlineMat;

        private void OnValidate()
        {
            if (ToonOutlineMat)
            {
                ToonOutlineMat.SetFloat("_OutlineWidth", ToonOutlineWidth);
                ToonOutlineMat.SetColor("_OutlineColor", ToonOutlineColor);
            }
        }

        private void Awake()
        {
            Singleton = this;
            useToonShading = SettingsData.useToonShading;
            universalLit = Shader.Find("Universal Render Pipeline/Lit");
            universalUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            bondLit = Shader.Find("Shader Graphs/BondShaderGraph");
            bondLitTransparent = Shader.Find("Shader Graphs/BondShaderGraph_Transparent");
            bondUnlit = Shader.Find("Shader Graphs/BondShaderGraph_Unlit");
        }

        private void Start()
        {
            List<ScriptableRendererFeature> rendererFeatures = GetRendererFeatures();
            ToonPaintRendererFeature = rendererFeatures.Find(s => s.name.Equals("ToonPaintRendererFeature"));
            ToonOutlineRendererFeature = rendererFeatures.Find(s => s.name.Equals("ToonOutlineRendererFeature"));
            ToonHighlightsRendererFeature = rendererFeatures.Find(s => s.name.Equals("ToonHighlightsRendererFeature"));
            if (!ToonPaintRendererFeature || !ToonOutlineRendererFeature || !ToonHighlightsRendererFeature) foundRendererFeatures = false;
            else 
            { 
                foundRendererFeatures = true;
                ToonOutlineMat = (ToonOutlineRendererFeature as FullScreenPassRendererFeature).passMaterial;
                setToonShading(UseToonShading);
            }
        }

        private void setToonShading(bool value)
        {
            var atomShader = value ? universalUnlit : universalLit;
            var bondShader = value ? bondUnlit : bondLit;

            var atoms = GameObject.FindGameObjectsWithTag("Atom");
            var bonds = GameObject.FindGameObjectsWithTag("Bond");
            foreach(var atom in atoms)
            {
                atom.GetComponent<MeshRenderer>().material.shader = atomShader;
                SetMaterialTransparent(atom.GetComponent<MeshRenderer>().material, false);
                atom.GetComponent<OutlinePro>().OutlineWidth = value ? new float[4] { 7f, 7f, 7f, 7f } : new float[4] { 5f, 5f, 5f, 5f };
                atom.GetComponent<OutlinePro>().NeedsUpdate();
            }
            foreach(var bond in bonds)
            {
                bond.GetComponentInChildren<MeshRenderer>().material.shader = bondShader;
                bond.GetComponent<Outline>().OutlineWidth = value ? 7f : 5f;
                
            }
            GlobalCtrl.Singleton.atomMatPrefab.shader = atomShader;
            GlobalCtrl.Singleton.dummyMatPrefab.shader = atomShader;
            SetMaterialTransparent(GlobalCtrl.Singleton.atomMatPrefab, false);
            SetMaterialTransparent(GlobalCtrl.Singleton.dummyMatPrefab, false);
            GlobalCtrl.Singleton.bondMat.shader = bondShader;
            foreach(var mat in GlobalCtrl.Singleton.Dic_AtomMat.Values)
            {
                mat.shader = atomShader;
                SetMaterialTransparent(mat, false);
            }

            ToonPaintRendererFeature.SetActive(value);
            ToonOutlineRendererFeature.SetActive(value);
            ToonHighlightsRendererFeature.SetActive(value);
        }

        public void setNoPostProcessingAndTransparency()
        {
            var atomShader = universalLit;
            var bondShader = bondLitTransparent;

            var atoms = GameObject.FindGameObjectsWithTag("Atom");
            var bonds = GameObject.FindGameObjectsWithTag("Bond");
            foreach (var atom in atoms)
            {
                atom.GetComponent<MeshRenderer>().material.shader = atomShader;
                SetMaterialTransparent(atom.GetComponent<MeshRenderer>().material, true);
                atom.GetComponent<OutlinePro>().OutlineWidth = new float[4] { 5f, 5f, 5f, 5f };
                atom.GetComponent<OutlinePro>().NeedsUpdate();
            }
            foreach (var bond in bonds)
            {
                bond.GetComponentInChildren<MeshRenderer>().material.shader = bondShader;
                bond.GetComponent<Outline>().OutlineWidth = 5f;

            }
            GlobalCtrl.Singleton.atomMatPrefab.shader = atomShader;
            GlobalCtrl.Singleton.dummyMatPrefab.shader = atomShader;
            SetMaterialTransparent(GlobalCtrl.Singleton.atomMatPrefab, true);
            SetMaterialTransparent(GlobalCtrl.Singleton.dummyMatPrefab, true);
            GlobalCtrl.Singleton.bondMat.shader = bondShader;
            foreach (var mat in GlobalCtrl.Singleton.Dic_AtomMat.Values)
            {
                mat.shader = atomShader;
                SetMaterialTransparent(mat, true);
            }

            ToonPaintRendererFeature.SetActive(false);
            ToonOutlineRendererFeature.SetActive(false);
            ToonHighlightsRendererFeature.SetActive(false);
        }

        private const int MATERIAL_OPAQUE = 0;
        private const int MATERIAL_TRANSPARENT = 1;

        private void SetMaterialTransparent(Material material, bool value)
        {
            material.SetFloat("_Surface", value ? MATERIAL_TRANSPARENT : MATERIAL_OPAQUE);
            material.SetShaderPassEnabled("SHADOWCASTER", !value);
            material.renderQueue = value ? 3000 : 2000;
            material.SetFloat("_DstBlend", value ? 10 : 0);
            material.SetFloat("_SrcBlend", value ? 5 : 1);
            material.SetFloat("_ZWrite", value ? 0 : 1);
        }

        public static List<ScriptableRendererFeature> GetRendererFeatures()
        {
            var renderer = (GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset).scriptableRenderer;
            return typeof(ScriptableRenderer)
                .GetField("m_RendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(renderer) as List<ScriptableRendererFeature>;
        }

        private void OnApplicationQuit()
        {
            GlobalCtrl.Singleton.atomMatPrefab.shader = universalLit;
            GlobalCtrl.Singleton.dummyMatPrefab.shader = universalLit;
            GlobalCtrl.Singleton.bondMat.shader = bondLit;
        }
    }
}

using chARpackColorPalette;
using chARpackTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StructureFormulaManager : MonoBehaviour
{
    private static StructureFormulaManager _singleton;

    public static StructureFormulaManager Singleton
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
                Debug.Log($"[{nameof(StructureFormulaManager)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    public List<Guid> getMolIDs()
    {
        List<Guid> mol_ids = new List<Guid>();
        foreach (var id in svg_instances.Keys)
        {
            mol_ids.Add(id);
        }
        return mol_ids;
    }

    private void Awake()
    {
        Singleton = this;
    }

    public Dictionary<Guid, Triple<GameObject, string, List<GameObject>>> svg_instances { get; private set; } // mol_id, primary_structure_formula, svg_content, secondary_structure_formulas
    private GameObject interactiblePrefab;
    private GameObject structureFormulaPrefab;
    public GameObject UICanvas;
    private List<Texture2D> heatMapTextures;
    private List<string> heatMapNames = new List<string> { "HeatTexture_Cool", "HeatTexture_Inferno", "HeatTexture_Magma", "HeatTexture_Plasma", "HeatTexture_Viridis", "HeatTexture_Warm" };
    [HideInInspector]
    public GameObject secondaryStructureDialogPrefab;

    private void Start()
    {
        svg_instances = new Dictionary<Guid, Triple<GameObject, string, List<GameObject>>>();
        structureFormulaPrefab = (GameObject)Resources.Load("prefabs/StructureFormulaPrefab");
        interactiblePrefab = (GameObject)Resources.Load("prefabs/2DAtom");
        selectionBoxPrefab = (GameObject)Resources.Load("prefabs/2DSelectionBox");
        secondaryStructureDialogPrefab = (GameObject)Resources.Load("prefabs/SecondaryStructureFormulaDialog");
        UICanvas = GameObject.Find("UICanvas");

        heatMapTextures = new List<Texture2D>();
        foreach (var name in heatMapNames)
        {
            heatMapTextures.Add((Texture2D)Resources.Load($"materials/{name}"));
        }
    }

    public void setColorMap(int id)
    {
        if (id >= heatMapNames.Count) return;

        foreach (var svg in svg_instances.Values)
        {
            svg.Item1.GetComponentInChildren<HeatMap2D>().UpdateTexture(heatMapTextures[id]);
        }
    }


    public void pushSecondaryContent(Guid mol_id, int focus_id)
    {
        if (!svg_instances.ContainsKey(mol_id))
        {
            Debug.LogError($"[StructureFormulaManager] Not able to generate secondary content for Molecule {mol_id}.");
            return;
        }
        var sf = svg_instances[mol_id].Item1.GetComponentInParent<StructureFormula>();
        var new_go = Instantiate(sf.gameObject, UICanvas.transform);
        new_go.transform.localScale = 0.8f * new_go.transform.localScale;
        var new_sf = new_go.GetComponent<StructureFormula>();
        new_sf.label.text = $"{sf.label.text} Focus: {focus_id}";
        new_sf.label.transform.parent.GetComponent<Image>().color = FocusColors.getColor(focus_id);
        new_sf.onlyUser = focus_id;
        // Deactivate interactibles
        var interactables = new_go.GetComponentInChildren<SVGImage>().gameObject.GetComponentsInChildren<Atom2D>();
        new_sf.interactables = interactables;
        foreach (var inter in new_sf.interactables)
        {
            inter.GetComponent<Button>().interactable = false;
        }

        svg_instances[mol_id].Item3.Add(new_go.GetComponentInChildren<SVGImage>().gameObject);
    }

    public void updateSecondaryContent(Guid mol_id, GameObject old_go)
    {
        var old_sf = old_go.GetComponentInParent<StructureFormula>();
        var sf = svg_instances[mol_id].Item1.GetComponentInParent<StructureFormula>();
        var new_go = Instantiate(sf.gameObject, UICanvas.transform);
        new_go.transform.localScale = old_sf.transform.localScale;
        new_go.transform.localPosition = old_sf.transform.localPosition;
        var new_sf = new_go.GetComponentInChildren<StructureFormula>();
        new_sf.label.text = old_sf.label.text;
        new_sf.label.transform.parent.GetComponent<Image>().color = FocusColors.getColor(old_sf.onlyUser);
        new_sf.onlyUser = old_sf.onlyUser;
        new_sf.setHighlightOption(old_sf.current_highlight_choice);
        // Deactivate interactibles
        var interactables = new_go.GetComponentInChildren<SVGImage>().gameObject.GetComponentsInChildren<Atom2D>();
        new_sf.interactables = interactables;
        foreach (var inter in new_sf.interactables)
        {
            inter.GetComponent<Button>().interactable = false;
        }

        Destroy(old_sf.gameObject);
        svg_instances[mol_id].Item3.Add(new_go.GetComponentInChildren<SVGImage>().gameObject);
    }

    public void pushContent(Guid mol_id, string svg_content)
    {
        if (svg_instances.ContainsKey(mol_id)) // UPDATE
        {
            var sceneInfo = SVGParser.ImportSVG(new StringReader(svg_content));
            var rect = svg_instances[mol_id].Item1.transform as RectTransform;
            var ui_rect = UICanvas.transform as RectTransform;
            float scaling_factor_w = (ui_rect.sizeDelta.x * 0.3f) / sceneInfo.SceneViewport.width;
            float scaling_factor_h = (ui_rect.sizeDelta.y * 0.5f) / sceneInfo.SceneViewport.height;
            var scaling_factor = scaling_factor_w < scaling_factor_h ? scaling_factor_w : scaling_factor_h;
            rect.sizeDelta = scaling_factor * new Vector2(sceneInfo.SceneViewport.width, sceneInfo.SceneViewport.height);

            var svg_component = svg_instances[mol_id].Item1.GetComponent<SVGImage>();
            // Tessellate
            var geometries = VectorUtils.TessellateScene(sceneInfo.Scene, new VectorUtils.TessellationOptions
            {
                StepDistance = 0.1f,
                SamplingStepSize = 50,
                MaxCordDeviation = 0.5f,
                MaxTanAngleDeviation = 0.1f
            });
            // Build a sprite
            var sprite = VectorUtils.BuildSprite(geometries, 100, VectorUtils.Alignment.Center, Vector2.zero, 128, true);

            // push image
            svg_component.sprite = sprite;
            var sf = svg_component.GetComponentInParent<StructureFormula>();
            sf.originalSize = new Vector2(sceneInfo.SceneViewport.width, sceneInfo.SceneViewport.height);
            sf.scaleFactor = scaling_factor;
            sf.sceneInfo = sceneInfo;
            sf.newImageResize();

            var old_secondary_structures = svg_instances[mol_id].Item3;

            svg_instances[mol_id] = new Triple<GameObject, string, List<GameObject>>(svg_instances[mol_id].Item1, svg_content, new List<GameObject>());

            removeInteractables(mol_id);
            createInteractables(mol_id);

            if (old_secondary_structures.Count > 0)
            {
                foreach (var secondary_sf in old_secondary_structures)
                {
                    updateSecondaryContent(mol_id, secondary_sf);
                }
            }
        }
        else // New Content
        {
            GameObject sf_object = Instantiate(structureFormulaPrefab, UICanvas.transform);
            sf_object.transform.localScale = Vector3.one;
            var sf = sf_object.GetComponent<StructureFormula>();

            sf.label.text = $"StructureFormula_{mol_id.ToString().Substring(0, 5)}";
            var rect = sf.image.transform as RectTransform;
            rect.transform.localScale = Vector2.one;

            var sceneInfo = SVGParser.ImportSVG(new StringReader(svg_content));

            var ui_rect = UICanvas.transform as RectTransform;
            float scaling_factor_w = (ui_rect.sizeDelta.x * 0.3f) / sceneInfo.SceneViewport.width;
            float scaling_factor_h = (ui_rect.sizeDelta.y * 0.5f) / sceneInfo.SceneViewport.height;
            var scaling_factor = scaling_factor_w < scaling_factor_h ? scaling_factor_w : scaling_factor_h;

            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-0.5f * sceneInfo.SceneViewport.width, 0f);
            rect.sizeDelta = scaling_factor * new Vector2(sceneInfo.SceneViewport.width, sceneInfo.SceneViewport.height);


            // Tessellate
            var geometries = VectorUtils.TessellateScene(sceneInfo.Scene, new VectorUtils.TessellationOptions
            {
                StepDistance = 0.1f,
                SamplingStepSize = 50,
                MaxCordDeviation = 0.5f,
                MaxTanAngleDeviation = 0.1f
            });

            // Build a sprite
            var sprite = VectorUtils.BuildSprite(geometries, 100, VectorUtils.Alignment.Center, Vector2.zero, 128, true);
            sf.image.sprite = sprite;
            sf.originalSize = new Vector2(sceneInfo.SceneViewport.width, sceneInfo.SceneViewport.height);
            sf.scaleFactor = scaling_factor;
            sf.sceneInfo = sceneInfo;
            sf.newImageResize();

            svg_instances[mol_id] = new Triple<GameObject, string, List<GameObject>>(sf.image.gameObject, svg_content, new List<GameObject>());

            createInteractables(mol_id);
        }
        validateMolecules();
    }

    public void removeContent(Guid mol_id)
    {
        if (!svg_instances.ContainsKey(mol_id))
        {
            return;
        }
        foreach (var ssf_img in svg_instances[mol_id].Item3)
        {
            var ssf = ssf_img.GetComponentInParent<StructureFormula>();
            Destroy(ssf.gameObject);
        }
        Destroy(svg_instances[mol_id].Item1.transform.parent.gameObject);
        svg_instances.Remove(mol_id);
    }

    private void removeInteractables(Guid mol_id)
    {
        if (!GlobalCtrl.Singleton.List_curMolecules.ContainsKey(mol_id))
        {
            Debug.LogError("[removeInteractables] Invalid Molecule ID.");
            return;
        }

        if (!svg_instances.ContainsKey(mol_id))
        {
            Debug.LogError("[removeInteractables] No structure formula found.");
            return;
        }

        var sf = svg_instances[mol_id].Item1.GetComponentInParent<StructureFormula>();
        //var interactible_instances = svg_instances[mol_id].Item1.GetComponentsInChildren<Atom2D>();

        foreach (var inter in sf.interactables)
        {
            inter.atom.structure_interactible = null;
            Destroy(inter.gameObject);
        }
        sf.interactables = null;
    }

    public void createInteractables(Guid mol_id)
    {
        if (!GlobalCtrl.Singleton.List_curMolecules.ContainsKey(mol_id))
        {
            Debug.LogError("[createInteractables] Invalid Molecule ID.");
            return;
        }
        var mol = GlobalCtrl.Singleton.List_curMolecules[mol_id];
        if (!svg_instances.ContainsKey(mol_id))
        {
            Debug.LogError("[createInteractables] No structure formula found.");
            return;
        }

        var sf_go = svg_instances[mol_id].Item1;
        var sf = sf_go.GetComponentInParent<StructureFormula>();

        List<Atom2D> inter_list = new List<Atom2D>();

        foreach (var atom in mol.atomList)
        {
            if (!atom.structure_interactible)
            {
                var inter = Instantiate(interactiblePrefab);
                inter.transform.SetParent(sf_go.transform, true);
                inter.transform.localScale = Vector3.one;
                atom.structure_interactible = inter;
                inter.GetComponent<Atom2D>().atom = atom;
                inter_list.Add(inter.GetComponent<Atom2D>());
            }


            var rect = sf_go.transform as RectTransform;
            var atom_rect = atom.structure_interactible.transform as RectTransform;
            atom_rect.sizeDelta = 17.5f * sf.scaleFactor * Vector2.one;

            var offset = new Vector2(-rect.sizeDelta.x, 0.5f * rect.sizeDelta.y) + sf.scaleFactor * new Vector2(atom.structure_coords.x, -atom.structure_coords.y) + 0.5f * new Vector2(-atom_rect.sizeDelta.x, atom_rect.sizeDelta.y);
            atom_rect.localPosition = offset;
        }
        sf.interactables = inter_list.ToArray();
    }

    public void updateInteractables(Guid mol_id)
    {
        if (!GlobalCtrl.Singleton.List_curMolecules.ContainsKey(mol_id))
        {
            Debug.LogError("[updateInteractables] Invalid Molecule ID.");
            return;
        }
        var mol = GlobalCtrl.Singleton.List_curMolecules[mol_id];
        if (!svg_instances.ContainsKey(mol_id))
        {
            Debug.LogError("[updateInteractables] No structure formula found.");
            return;
        }

        var sf_go = svg_instances[mol_id].Item1;
        var sf = sf_go.GetComponentInParent<StructureFormula>();

        foreach (var atom in mol.atomList)
        {
            var rect = sf_go.transform as RectTransform;
            var atom_rect = atom.structure_interactible.transform as RectTransform;
            atom_rect.sizeDelta = 17.5f * sf.scaleFactor * Vector2.one;
            var image_offset = Vector2.zero;

            if (sf.image.GetComponent<RectTransform>().rect.height < sf.image.GetComponent<RectTransform>().rect.width / sf.imageAspect)
            { // if window is wider than image
                var x_offset_image = sf.image.GetComponent<RectTransform>().rect.width - sf.image.GetComponent<RectTransform>().rect.height * sf.imageAspect; // empty space to the left of the image
                image_offset = new Vector2(x_offset_image - rect.sizeDelta.x, 0.5f * rect.sizeDelta.y) + 0.5f * new Vector2(-atom_rect.sizeDelta.x, atom_rect.sizeDelta.y); // position at upper left corner of image
            }
            else
            {
                var y_offset_image = (sf.image.GetComponent<RectTransform>().rect.height - sf.image.GetComponent<RectTransform>().rect.width / sf.imageAspect) * 0.5f; // for some reason, image is vertically centered
                image_offset = new Vector2(-rect.sizeDelta.x, 0.5f * rect.sizeDelta.y - y_offset_image) + 0.5f * new Vector2(-atom_rect.sizeDelta.x, atom_rect.sizeDelta.y); // position at upper left corner of image
            }
            var offset = image_offset + sf.scaleFactor * new Vector2(atom.structure_coords.x, -atom.structure_coords.y);
            atom_rect.localPosition = offset;
        }
    }

    public Guid getMolID(StructureFormula in_sf)
    {
        foreach (var go in svg_instances)
        {
            var sf = go.Value.Item1.GetComponentInParent<StructureFormula>();
            if (sf == in_sf)
            {
                return go.Key;
            }
        }
        return Guid.Empty;
    }

    public void addFocusHighlight(Guid mol_id, Atom atom, bool[] values, Color[] cols)
    {
        if (atom.isMarked) return;
        if (svg_instances.ContainsKey(mol_id))
        {
            List<StructureFormula> sf_list = new List<StructureFormula>();
            sf_list.Add(svg_instances[mol_id].Item1.GetComponentInParent<StructureFormula>());
            if (svg_instances[mol_id].Item3.Count > 0)
            {
                foreach (var item in svg_instances[mol_id].Item3)
                {
                    sf_list.Add(item.GetComponentInParent<StructureFormula>());
                }
            }
            foreach (var sf in sf_list)
            {
                var col_copy = cols != null ? new Color[4] { cols[0], cols[1], cols[2], cols[3] } : null;
                if (sf.current_highlight_choice == 0 && cols != null)
                {
                    //var atom2d = atom.structure_interactible.GetComponent<Atom2D>();
                    Atom2D atom2d = null;
                    foreach (var inter in sf.gameObject.GetComponentsInChildren<Atom2D>())
                    {
                        if (inter.atom == atom)
                        {
                            atom2d = inter;
                            break;
                        }
                    }
                    if (atom2d == null) return;
                    if (sf.onlyUser >= 0)
                    {
                        var pos = FocusManager.getPosInArray(sf.onlyUser);
                        for (int i = 0; i < FocusManager.maxNumOutlines; i++)
                        {
                            col_copy[i] = cols[pos];
                        }
                    }
                    atom2d.FociColors = col_copy; // set full array to trigger set function
                }
                else if (sf.current_highlight_choice == 1)
                {
                    var heat = sf.gameObject.GetComponentInChildren<HeatMap2D>();
                    if (sf.onlyUser >= 0)
                    {
                        var pos = FocusManager.getPosInArray(sf.onlyUser);
                        heat.SetAtomFocus(atom, values[pos]);
                    }
                    else
                    {
                        if (values.AnyTrue())
                        {
                            heat.SetAtomFocus(atom, true);
                        }
                        else
                        {
                            heat.SetAtomFocus(atom, false);
                        }
                    }
                }
            }
        }
    }

    public void addServerFocusHighlight(Guid mol_id, Atom atom, Color[] col)
    {
        if (svg_instances.ContainsKey(mol_id))
        {
            var atom2d = atom.structure_interactible.GetComponent<Atom2D>();
            atom2d.FociColors = col; // set full array to trigger set function
        }
    }

    public void addSelectHighlight(Guid mol_id, Atom atom, Color[] selCol)
    {

        if (svg_instances.ContainsKey(mol_id))
        {
            var atom2d = atom.structure_interactible.GetComponent<Atom2D>();
            atom2d.FociColors = selCol; // set full array to trigger set function
        }
    }

    Vector2 selectionStartPos = Vector2.zero;
    bool isSelecting = false;
    bool needsInitialization = false;
    Guid currentMol = Guid.Empty;
    GameObject currentStructureFormula;
    GameObject selectionBoxPrefab;
    GameObject selectionBoxInstance;


    private void Update()
    {

        var rayCastResults = GetEventSystemRaycastResults();

        if (Input.GetMouseButtonDown(0))
        {
            currentMol = getMolIDFromRaycastResult(rayCastResults);
            if (currentMol != Guid.Empty)
            {
                currentStructureFormula = svg_instances[currentMol].Item1;
                selectionStartPos = currentStructureFormula.transform.InverseTransformPoint(Mouse.current.position.ReadValue());
                isSelecting = true;
                needsInitialization = true;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isSelecting = false;
            currentMol = Guid.Empty;
            if (selectionBoxInstance)
            {
                Destroy(selectionBoxInstance);
            }
        }

        if (isSelecting && Input.GetMouseButton(0))
        {
            var currentSelectionPos = currentStructureFormula.transform.InverseTransformPoint(Mouse.current.position.ReadValue());

            float width = currentSelectionPos.x - selectionStartPos.x;
            float height = currentSelectionPos.y - selectionStartPos.y;
            var dist = new Vector2(width, height);

            if (needsInitialization && dist.magnitude > 2f)
            {
                
                selectionBoxInstance = Instantiate(selectionBoxPrefab);
                selectionBoxInstance.transform.SetParent(currentStructureFormula.transform);
                selectionBoxInstance.transform.localScale = Vector3.one;
                needsInitialization = false;
            }
            
            if (selectionBoxInstance)
            {
                var scaleFactor = UICanvas.GetComponent<Canvas>().scaleFactor;

                var selBox = selectionBoxInstance.transform as RectTransform;
                selBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height)); // / scaleFactor;
                selBox.localPosition = (selectionStartPos + new Vector2(width / 2, height / 2)); // / scaleFactor;

                var left = selBox.localPosition.x - 0.5f * selBox.sizeDelta.x - 30;
                var right = selBox.localPosition.x + 0.5f * selBox.sizeDelta.x;
                var bot = selBox.localPosition.y - 0.5f * selBox.sizeDelta.y;
                var top = selBox.localPosition.y + 0.5f * selBox.sizeDelta.y + 30;


                foreach (var atom in GlobalCtrl.Singleton.List_curMolecules[currentMol].atomList)
                {
                    var atom_rect = atom.structure_interactible.transform as RectTransform;
                    if (atom_rect.localPosition.x >= left &&
                        atom_rect.localPosition.x <= right &&
                        atom_rect.localPosition.y >= bot &&
                        atom_rect.localPosition.y <= top)
                    {
                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            if (!atom.isMarked) atom.markAtomUI(true);
                        }
                        else
                        {
                            if (!atom.serverFocus) atom.serverFocusHighlightUI(true);
                        }
                    }
                    else
                    {
                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            if (atom.isMarked) atom.markAtomUI(false);
                        }
                        else
                        {
                            if (atom.serverFocus) atom.serverFocusHighlightUI(false);
                        }
                    }
                }
            }
        }
    }

    public void removeSubstrcutures(int focus_id)
    {
        foreach (var structure in svg_instances.Values)
        {
            List<GameObject> for_delete = new List<GameObject>();
            foreach (var img_obj in structure.Item3)
            {
                var ssf = img_obj.GetComponentInParent<StructureFormula>();
                if (ssf.onlyUser == focus_id)
                {
                    for_delete.Add(img_obj);
                    Destroy(ssf.gameObject);
                }
            }
            foreach (var del in for_delete)
            {
                structure.Item3.Remove(del);
            }
        }
    }

    ///Gets all event systen raycast results of current mouse or touch position.
    private static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);

        return raysastResults;
    }

    private Guid getMolIDFromRaycastResult(List<RaycastResult> eventSystemRaysastResults)
    {
        foreach (var sf in svg_instances)
        {
            // only if its in front of the list (or blocked by interactivble)
            // TODO does not work for overlayed heatmap anymore
            if (eventSystemRaysastResults.Count < 1) return Guid.Empty;
            if (eventSystemRaysastResults[0].gameObject == sf.Value.Item1)
            {
                return sf.Key;
            }
            if (eventSystemRaysastResults.Count < 2) return Guid.Empty;
            if (eventSystemRaysastResults[1].gameObject == sf.Value.Item1)
            {
                return sf.Key;
            }
        }

        //foreach (var sf in svg_instances)
        //{
        //    foreach (var rr in eventSystemRaysastResults)
        //    {
        //        if (sf.Value.Item1 == rr.gameObject)
        //        {
        //            return sf.Key;
        //        }
        //    }
        //}
        return Guid.Empty;
    }

    private Atom2D getInteractibleFromRaycastResult(List<RaycastResult> eventSystemRaysastResults)
    {
        var mol_id = getMolIDFromRaycastResult(eventSystemRaysastResults);
        if (mol_id != Guid.Empty)
        {
            foreach (var rr in eventSystemRaysastResults)
            {
                if (rr.gameObject.GetComponent<Atom2D>() != null)
                {
                    return rr.gameObject.GetComponent<Atom2D>();
                }
            }
        }
        return null;
    }

    private void validateMolecules()
    {
        List<Guid> to_be_removed = new List<Guid>();
        foreach (var sf_id in svg_instances.Keys)
        {
            if (!GlobalCtrl.Singleton.List_curMolecules.ContainsKey(sf_id))
            {
                to_be_removed.Add(sf_id);
            }
        }
        foreach (var tbr in to_be_removed)
        {
            removeContent(tbr);
        }
    }

    public void requestRemove(StructureFormula in_sf)
    {
        List<Guid> to_be_removed = new List<Guid>();
        foreach (var go in svg_instances)
        {
            var sf = go.Value.Item1.GetComponentInParent<StructureFormula>();
            if (sf == in_sf)
            {
                to_be_removed.Add(go.Key);
            }
        }
        foreach (var tbr in to_be_removed)
        {
            removeContent(tbr);
        }
    }
}


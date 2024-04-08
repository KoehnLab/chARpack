using chARpackColorPalette;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    private void Awake()
    {
        Singleton = this;
    }

    private Dictionary<ushort, Tuple<GameObject, string>> svg_instances;
    private GameObject interactiblePrefab;
    private static float scaleFactor = 4f;
    private GameObject UICanvas;

    private void Start()
    {
        svg_instances = new Dictionary<ushort, Tuple<GameObject, string>>();
        interactiblePrefab = (GameObject)Resources.Load("prefabs/2DAtom");
        selectionBoxPrefab = (GameObject)Resources.Load("prefabs/2DSelectionBox");
        UICanvas = GameObject.Find("UICanvas");
    }

    public void pushContent(ushort mol_id, string svg_content)
    {
        if (svg_instances.ContainsKey(mol_id))
        {
            var sceneInfo = SVGParser.ImportSVG(new StringReader(svg_content));
            var rect = svg_instances[mol_id].Item1.GetComponent<RectTransform>();
            rect.sizeDelta = scaleFactor * new Vector2(sceneInfo.SceneViewport.width, sceneInfo.SceneViewport.height);

            var svg_component = svg_instances[mol_id].Item1.GetComponent<SVGImage>();
            // Tessellate
            var geometries = VectorUtils.TessellateScene(sceneInfo.Scene, new VectorUtils.TessellationOptions
            {
                StepDistance = 1,
                SamplingStepSize = 50,
                MaxCordDeviation = 0.5f,
                MaxTanAngleDeviation = 0.1f
            });
            // Build a sprite
            var sprite = VectorUtils.BuildSprite(geometries, 100.0f, VectorUtils.Alignment.Center, Vector2.zero, 128, true);

            // push image
            svg_component.sprite = sprite;

            svg_instances[mol_id] = new Tuple<GameObject, string>(svg_instances[mol_id].Item1, svg_content);
        }
        else
        {
            GameObject svg_canvas = new GameObject();
            svg_canvas.transform.SetParent(UICanvas.transform);

            svg_canvas.name = $"StructureFormula_{mol_id}";
            svg_canvas.AddComponent<CanvasRenderer>();
            var rect = svg_canvas.AddComponent<RectTransform>();
            rect.transform.localScale = Vector2.one;

            var svg_component = svg_canvas.AddComponent<SVGImage>();
            var sceneInfo = SVGParser.ImportSVG(new StringReader(svg_content));

            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-0.5f * sceneInfo.SceneViewport.width, 0f);
            rect.sizeDelta = scaleFactor * new Vector2(sceneInfo.SceneViewport.width, sceneInfo.SceneViewport.height);


            // Tessellate
            var geometries = VectorUtils.TessellateScene(sceneInfo.Scene, new VectorUtils.TessellationOptions
            {
                StepDistance = 1,
                SamplingStepSize = 50,
                MaxCordDeviation = 0.5f,
                MaxTanAngleDeviation = 0.1f
            });


            // Build a sprite
            var sprite = VectorUtils.BuildSprite(geometries, 1, VectorUtils.Alignment.Center, Vector2.zero, 128, true);
            svg_component.sprite = sprite;

            svg_instances[mol_id] = new Tuple<GameObject, string>(svg_canvas, svg_content);

            createInteractibles(mol_id);
        }
    }

    public void createInteractibles(ushort mol_id)
    {
        var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id, null);
        if (!mol)
        {
            Debug.LogError("[createInteractibles] Invalid Molecule ID.");
            return;
        }
        if (!svg_instances.ContainsKey(mol_id))
        {
            Debug.LogError("[createInteractibles] No structure formula found.");
            return;
        }


        foreach (var atom in mol.atomList)
        {
            if (!atom.structure_interactible)
            {
                var inter = Instantiate(interactiblePrefab);
                inter.transform.SetParent(svg_instances[mol_id].Item1.transform, true);
                inter.transform.localScale = Vector3.one;
                atom.structure_interactible = inter;
                inter.GetComponent<Atom2D>().atom = atom;
            }


            var rect = svg_instances[mol_id].Item1.transform as RectTransform;
            var atom_rect = atom.structure_interactible.transform as RectTransform;
            atom_rect.sizeDelta = 60f * Vector2.one;

            var offset = new Vector2(-rect.sizeDelta.x, 0.5f * rect.sizeDelta.y) + scaleFactor * new Vector2(atom.structure_coords.x, -atom.structure_coords.y) + 0.5f * new Vector2(-atom_rect.sizeDelta.x, atom_rect.sizeDelta.y);
            atom_rect.localPosition = offset;
        }

    }


    //public void addHighlight(ushort mol_id, Vector2 coord, bool value)
    //{
    //    if (svg_instances.ContainsKey(mol_id))
    //    {
    //        var radius = 6f;
    //        var fill = "blue";
    //        var alpha = 0.6f;

    //        var svg_content = svg_instances[mol_id].Item2;
    //        var circle = $"<circle cx=\"{coord.x}\" cy=\"{coord.y}\" r=\"{radius}\" fill=\"{fill}\" fill-opacity=\"{alpha}\"/>";
    //        var test = $"<circle cx=\"{coord.x}\" cy=\"{coord.y}\"";

    //        if (value)
    //        {
    //            if (!svg_content.Contains(test))
    //            {
    //                svg_content = svg_content.Replace("</svg>", $"\n{circle}\n</svg>");
    //                pushContent(mol_id, svg_content);
    //                //Debug.Log(svg_content);
    //            }
    //        }
    //        else
    //        {
    //            if (svg_content.Contains(test))
    //            {
    //                svg_content = svg_content.Replace($"{circle}\n", "");
    //                pushContent(mol_id, svg_content);
    //            }
    //        }
    //    }
    //    else
    //    {
    //        Debug.LogError($"[StructureFormulaManager] Tying to add content to non existent structure formula.");
    //        return;
    //    }
    //}

    public void addFocusHighlight(ushort mol_id, Atom atom, bool value, float alpha)
    {
        if (atom.isMarked) return;
        if (svg_instances.ContainsKey(mol_id))
        {
            if (value)
            {
                var col = atom.structure_interactible.GetComponent<Image>().color;
                col.a = alpha;
                atom.structure_interactible.GetComponent<Image>().color = col;
            }
            else
            {
                var col = atom.structure_interactible.GetComponent<Image>().color;
                col.a = 0f;
                atom.structure_interactible.GetComponent<Image>().color = col;
            }
        }
        else
        {
            Debug.LogError($"[StructureFormulaManager] Tying to add content to non existent structure formula.");
            return;
        }
    }

    public void addSelectHighlight(ushort mol_id, Atom atom, bool value)
    {
        if (svg_instances.ContainsKey(mol_id))
        {
            if (value)
            {
                atom.structure_interactible.GetComponent<Image>().color = chARpackColors.structureFormulaSelect;
            }
            else
            {
                atom.structure_interactible.GetComponent<Image>().color = chARpackColors.structureFormulaInvis;
            }
        }
        else
        {
            Debug.LogError($"[StructureFormulaManager] Tying to add content to non existent structure formula.");
            return;
        }
    }

    Vector2 selectionStartPos = Vector2.zero;
    bool isSelecting = false;
    bool needsInitialization = false;
    int currentMol = -1;
    GameObject currentStructureFormula;
    GameObject selectionBoxPrefab;
    GameObject selectionBoxInstance;


    private void Update()
    {
        var rayCastResults = GetEventSystemRaycastResults();

        if (Input.GetMouseButtonDown(0))
        {
            currentMol = getMolIDFromRaycastResult(rayCastResults);
            if (currentMol >= 0)
            {
                currentStructureFormula = svg_instances[(ushort)currentMol].Item1;
                selectionStartPos = currentStructureFormula.transform.InverseTransformPoint(Mouse.current.position.ReadValue());
                isSelecting = true;
                needsInitialization = true;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isSelecting = false;
            currentMol = -1;
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
                        if (!atom.isMarked) atom.markAtomUI(true);
                    }
                    else
                    {
                        if (atom.isMarked) atom.markAtomUI(false);
                    }
                }

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

    private int getMolIDFromRaycastResult(List<RaycastResult> eventSystemRaysastResults)
    {
        foreach(var sf in svg_instances)
        {
            foreach (var rr in eventSystemRaysastResults)
            {
                if (sf.Value.Item1 == rr.gameObject)
                {
                    return sf.Key;
                }
            }
        }
        return -1;
    }

    private Atom2D getInteractibleFromRaycastResult(List<RaycastResult> eventSystemRaysastResults)
    {
        var mol_id = getMolIDFromRaycastResult(eventSystemRaysastResults);
        if (mol_id >= 0)
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



}


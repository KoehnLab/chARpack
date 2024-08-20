using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Unity.VectorGraphics;

/// <summary>
/// This class contains functionality related to the resizing of structure formulas
/// on the server.
/// </summary>
public class myResizer : MonoBehaviour {
    /// <summary>
    /// The structure formula UI window this resizer refers to
    /// </summary>
    public GameObject panel;
    /// <summary>
    /// The resize handles for all four corners
    /// </summary>
    public List<GameObject> resizeHandles;
    /// <summary>
    /// The SVG structure formula image
    /// </summary>
    public GameObject image;

	private RectTransform panelRectTransform;
    /// <summary>
    /// The structure formula this resizer belongs to
    /// </summary>
    public StructureFormula structureFormula;

	void Awake () {
		panelRectTransform = panel.GetComponent<RectTransform> ();
		structureFormula = panel.GetComponent<StructureFormula>();

		// Assign position to each handle
		if(resizeHandles.Count > 0)
        {
			for (var i=0; i<resizeHandles.Count; i++)
			{
				resizeHandles[i].GetComponent<resizeHandle>().corner = (resizeHandle.Corner)i;
				resizeHandles[i].GetComponent<resizeHandle>().rect = panelRectTransform;
			}
        }
	}

    /// <summary>
    /// Moves the handles to the corners of the structure formula UI window and resizes the 
	/// image to fit.
    /// </summary>
    public void moveHandlesAndResize () {
		if (panelRectTransform == null)
			return;

		var offset = 8;

		resizeImage();

		if(image.activeSelf) structureFormula.GetComponent<RectTransform>().sizeDelta = new Vector2(structureFormula.GetComponent<RectTransform>().sizeDelta.x, structureFormula.paddedHeightOfAllElements() + 15f);


		if (resizeHandles.Count > 0)
		{
			resizeHandles[0].GetComponent<RectTransform>().localPosition = new Vector2(panelRectTransform.rect.x + offset, panelRectTransform.rect.y + panelRectTransform.rect.size.y - offset);
			resizeHandles[1].GetComponent<RectTransform>().localPosition = new Vector2(panelRectTransform.rect.x + panelRectTransform.rect.size.x - offset, panelRectTransform.rect.y + panelRectTransform.rect.size.y - offset);
			resizeHandles[2].GetComponent<RectTransform>().localPosition = new Vector2(panelRectTransform.rect.x + offset, panelRectTransform.rect.y + offset);
			resizeHandles[3].GetComponent<RectTransform>().localPosition = new Vector2(panelRectTransform.rect.x + panelRectTransform.rect.size.x - offset, panelRectTransform.rect.y + offset);
		}
	}


    /// <summary>
    /// Resizes the image to fill the full width of the structure formula window.
	/// Also requests and update of the clickable 2D atom representatives belonging to the structure formula.
    /// </summary>
    public void resizeImage()
    {
		image.GetComponent<LayoutElement>().minHeight = panelRectTransform.rect.width / structureFormula.imageAspect;
		var image_rect = image.GetComponent<RectTransform>();
		structureFormula.scaleFactor = Mathf.Min(image_rect.rect.height / structureFormula.sceneInfo.SceneViewport.height, image_rect.rect.width/ structureFormula.sceneInfo.SceneViewport.width);
		StructureFormulaManager.Singleton.updateInteractables(StructureFormulaManager.Singleton.getMolID(structureFormula));
	}
}
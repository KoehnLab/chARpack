using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Unity.VectorGraphics;

public class myResizer : MonoBehaviour {
	public GameObject panel;
	public List<GameObject> resizeHandles;
	public GameObject image;

	private RectTransform panelRectTransform;
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
	
	public void moveHandlesAndResize () {
		if (panelRectTransform == null)
			return;

		var offset = 8;

		resizeImage();

		if(image.activeSelf) structureFormula.GetComponent<RectTransform>().sizeDelta = new Vector2(structureFormula.GetComponent<RectTransform>().sizeDelta.x, structureFormula.paddedHeightOfAllElements());


		if (resizeHandles.Count > 0)
		{
			resizeHandles[0].GetComponent<RectTransform>().localPosition = new Vector2(panelRectTransform.rect.x + offset, panelRectTransform.rect.y + panelRectTransform.rect.size.y - offset);
			resizeHandles[1].GetComponent<RectTransform>().localPosition = new Vector2(panelRectTransform.rect.x + panelRectTransform.rect.size.x - offset, panelRectTransform.rect.y + panelRectTransform.rect.size.y - offset);
			resizeHandles[2].GetComponent<RectTransform>().localPosition = new Vector2(panelRectTransform.rect.x + offset, panelRectTransform.rect.y + offset);
			resizeHandles[3].GetComponent<RectTransform>().localPosition = new Vector2(panelRectTransform.rect.x + panelRectTransform.rect.size.x - offset, panelRectTransform.rect.y + offset);
		}
	}

	public void resizeImage()
    {
		image.GetComponent<LayoutElement>().minHeight = panelRectTransform.rect.width / structureFormula.imageAspect;
		var image_rect = image.GetComponent<RectTransform>();
		structureFormula.scaleFactor = Mathf.Min(image_rect.rect.height / structureFormula.sceneInfo.SceneViewport.height, image_rect.rect.width/ structureFormula.sceneInfo.SceneViewport.width);
		StructureFormulaManager.Singleton.updateInteractables(StructureFormulaManager.Singleton.getMolID(structureFormula));
	}
}
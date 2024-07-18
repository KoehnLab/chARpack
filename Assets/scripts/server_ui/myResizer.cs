using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class myResizer : MonoBehaviour {
	public GameObject panel;
	public List<GameObject> resizeHandles;

	private RectTransform panelRectTransform;

	void Awake () {
		panelRectTransform = panel.GetComponent<RectTransform> ();

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
	
	public void moveHandles () {
		if (panelRectTransform == null)
			return;

		if (resizeHandles.Count > 0)
		{ 
			resizeHandles[0].GetComponent<RectTransform>().localPosition = new Vector2(panelRectTransform.rect.x, panelRectTransform.rect.y + panelRectTransform.rect.size.y);
			resizeHandles[1].GetComponent<RectTransform>().localPosition = new Vector2(panelRectTransform.rect.x + panelRectTransform.rect.size.x, panelRectTransform.rect.y + panelRectTransform.rect.size.y);
			resizeHandles[2].GetComponent<RectTransform>().localPosition = new Vector2(panelRectTransform.rect.x, panelRectTransform.rect.y);
			resizeHandles[3].GetComponent<RectTransform>().localPosition = new Vector2(panelRectTransform.rect.x + panelRectTransform.rect.size.x, panelRectTransform.rect.y);
		}
	}
}
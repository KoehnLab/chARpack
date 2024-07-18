using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class myResizer : MonoBehaviour, IDragHandler {
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
	
	public void OnDrag (PointerEventData data) {
		if (panelRectTransform == null)
			return;

		if (resizeHandles.Count > 0)
		{
			resizeHandles[0].GetComponent<RectTransform>().localPosition = new Vector2(-panelRectTransform.sizeDelta[0], panelRectTransform.sizeDelta[1] / 2);
			resizeHandles[1].GetComponent<RectTransform>().localPosition = new Vector2(-0f, panelRectTransform.sizeDelta[1] / 2);
			resizeHandles[2].GetComponent<RectTransform>().localPosition = new Vector2(-panelRectTransform.sizeDelta[0], -panelRectTransform.sizeDelta[1] / 2);
			resizeHandles[3].GetComponent<RectTransform>().localPosition = new Vector2(-0f, -panelRectTransform.sizeDelta[1] / 2);
		}
	}
}
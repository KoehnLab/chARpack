using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class myResizer : MonoBehaviour, IPointerDownHandler, IDragHandler {
	
	public Vector2 minSize = new Vector2 (100, 100);
	public Vector2 maxSize = new Vector2 (400, 400);
	public GameObject panel;
	public List<GameObject> resizeHandles;

	private RectTransform panelRectTransform;
	private Vector2 originalLocalPointerPosition;
	private Vector2 originalSizeDelta;
	
	void Awake () {
		panelRectTransform = panel.GetComponent<RectTransform> ();
	}
	
	public void OnPointerDown (PointerEventData data) {
		originalSizeDelta = panelRectTransform.sizeDelta;
		RectTransformUtility.ScreenPointToLocalPointInRectangle (panelRectTransform, data.position, data.pressEventCamera, out originalLocalPointerPosition);
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

		Vector2 localPointerPosition;
		RectTransformUtility.ScreenPointToLocalPointInRectangle (panelRectTransform, data.position, data.pressEventCamera, out localPointerPosition);
		Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;
		
		Vector2 sizeDelta = originalSizeDelta + new Vector2 (offsetToOriginal.x, -offsetToOriginal.y);
		sizeDelta = new Vector2 (
			Mathf.Clamp (sizeDelta.x, minSize.x, maxSize.x),
			Mathf.Clamp (sizeDelta.y, minSize.y, maxSize.y)
		);
		
		panelRectTransform.sizeDelta = sizeDelta;
	}
}
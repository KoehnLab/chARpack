using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class resizeHandle : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public enum Corner
    {
        UpperLeft,
        UpperRight,
        LowerLeft,
        LowerRight
    }

    [HideInInspector] public Corner corner;
    private Vector2 originalLocalPointerPosition;
    private Vector2 originalSizeDelta;
    public Vector2 minSize = new Vector2(200, 200);
    public Vector2 maxSize = new Vector2(1000, 1000);

    [HideInInspector] public RectTransform rect;

    public void OnPointerDown(PointerEventData data)
    {
        originalSizeDelta = rect.sizeDelta;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, data.pressEventCamera, out originalLocalPointerPosition);
    }

    public void OnDrag(PointerEventData data)
    {
        Vector2 localPointerPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, data.pressEventCamera, out localPointerPosition);
        Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;

        Vector2 sizeDelta = originalSizeDelta;
        switch (corner)
        {
            case Corner.UpperLeft:
                sizeDelta = originalSizeDelta + new Vector2(-offsetToOriginal.x, offsetToOriginal.y);
                break;
            case Corner.UpperRight:
                sizeDelta = originalSizeDelta + new Vector2(offsetToOriginal.x, offsetToOriginal.y);
                break;
            case Corner.LowerLeft:
                sizeDelta = originalSizeDelta + new Vector2(-offsetToOriginal.x, -offsetToOriginal.y);
                break;
            case Corner.LowerRight:
                sizeDelta = originalSizeDelta + new Vector2(offsetToOriginal.x, -offsetToOriginal.y);
                break;
        }
        sizeDelta = new Vector2(
            Mathf.Clamp(sizeDelta.x, minSize.x, maxSize.x),
            Mathf.Clamp(sizeDelta.y, minSize.y, maxSize.y)
        );

        rect.sizeDelta = sizeDelta;
    }
}

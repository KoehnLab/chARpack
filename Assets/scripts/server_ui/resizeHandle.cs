using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
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
    private myResizer resizer;
    public Vector2 minSize = new Vector2(200, 200);
    public Vector2 maxSize = new Vector2(1000, 1000);

    public void Start()
    {
        resizer = transform.parent.GetComponent<myResizer>();
    }

    [HideInInspector] public RectTransform rect;

    public void OnPointerDown(PointerEventData data)
    {
        originalSizeDelta = rect.sizeDelta;
        originalLocalPointerPosition = data.position;
    }

    public void OnDrag(PointerEventData data)
    {
        Vector2 localPointerPosition;
        localPointerPosition = data.position;
        Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;

        Vector2 sizeDelta = originalSizeDelta;
        movePivot(corner);
        switch (corner)
        {
            case Corner.UpperLeft:
                sizeDelta = originalSizeDelta + new Vector2(-offsetToOriginal.x, offsetToOriginal.y) * 1.5f;
                break;
            case Corner.UpperRight:
                sizeDelta = originalSizeDelta + new Vector2(offsetToOriginal.x, offsetToOriginal.y) * 1.5f;
                break;
            case Corner.LowerLeft:
                sizeDelta = originalSizeDelta + new Vector2(-offsetToOriginal.x, -offsetToOriginal.y) * 1.5f;
                break;
            case Corner.LowerRight:
                sizeDelta = originalSizeDelta + new Vector2(offsetToOriginal.x, -offsetToOriginal.y) * 1.5f;
                break;
        }
        sizeDelta = new Vector2(
            Mathf.Clamp(sizeDelta.x, minSize.x, maxSize.x),
            Mathf.Clamp(sizeDelta.y, minSize.y, maxSize.y)
        );

        rect.sizeDelta = sizeDelta;

        resizer.moveHandlesAndResize();
    }

    public void movePivot(Corner corner)
    {
        var worldPos = rect.position;
        var originalPivot = rect.pivot;
        switch (corner)
        {
            case Corner.UpperLeft:
                rect.pivot = new Vector2(1f, 0f);
                break;
            case Corner.UpperRight:
                rect.pivot = new Vector2(0f, 0f);
                break;
            case Corner.LowerLeft:
                rect.pivot = new Vector2(1f, 1f);
                break;
            case Corner.LowerRight:
                rect.pivot = new Vector2(0f, 1f);
                break;
        }
        //var offset = new Vector2((originalPivot.x - rect.pivot.x) * rect.rect.width, (originalPivot.y - rect.pivot.y) * rect.rect.height);
        var offset = originalPivot - rect.pivot;
        offset.Scale(rect.rect.size);
        rect.position = worldPos - rect.TransformVector(offset);
    }
}

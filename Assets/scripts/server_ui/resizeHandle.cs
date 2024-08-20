using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.EventSystems;


/// <summary>
/// This class contains the behaviour of a single resize handle used for structure formulas.
/// </summary>
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
    /// <summary>
    /// The minimum size the corresponding window can have.
    /// Since the aspect ratio of the window is kept constant, in most
    /// cases only one coordinate will actually attain the minimum value.
    /// </summary>
    public Vector2 minSize = new Vector2(200, 200);
    /// <summary>
    /// The maximum size the corresponding window can have.
    /// Since the aspect ratio of the window is kept constant, in most
    /// cases only one coordinate will actually attain the maximum value.
    /// </summary>
    public Vector2 maxSize = new Vector2(1000, 1000);

    public void Start()
    {
        resizer = transform.parent.GetComponent<myResizer>();
    }

    [HideInInspector] public RectTransform rect;

    /// <summary>
    /// Called on mouse click. Initializes data necessary for resizing.
    /// </summary>
    /// <param name="data">The data of the mouse click event.</param>
    public void OnPointerDown(PointerEventData data)
    {
        originalSizeDelta = rect.sizeDelta;
        originalLocalPointerPosition = data.position;
    }

    /// <summary>
    /// Called on mouse drag.
    /// Resizes the structure formula window and calls for updating handles and image.
    /// </summary>
    /// <param name="data">The data of the mouse drag.</param>
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

    /// <summary>
    /// Moves the pivot point of the window so the corner opposite the dragged one is
    /// kept at a constant position when resizing.
    /// </summary>
    /// <param name="corner">The corner that is being dragged.</param>
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

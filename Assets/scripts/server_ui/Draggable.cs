using UnityEngine;
using UnityEngine.EventSystems;


public class Draggable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private bool isMouseDown = false;
    private Vector3 startMousePosition;
    private Vector3 startPosition;
    public bool shouldReturn;

    public void OnPointerDown(PointerEventData dt)
    {
        isMouseDown = true;

        startPosition = transform.position;
        startMousePosition = Input.mousePosition;
    }

    public void OnPointerUp(PointerEventData dt)
    {
        isMouseDown = false;

        if (shouldReturn)
        {
            transform.position = startPosition;
        }
    }

    void Update()
    {
        if (isMouseDown)
        {
            Vector3 currentPosition = Input.mousePosition;

            Vector3 diff = currentPosition - startMousePosition;

            Vector3 pos = startPosition + diff;

            transform.position = pos;
        }
    }
}
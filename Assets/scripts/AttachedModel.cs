using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class AttachedModel : MonoBehaviour
{

    public GenericObject genericObject;

    #region mouse_interaction

#if UNITY_STANDALONE || UNITY_EDITOR
    public static bool anyArcball = false;
    private bool arcball;
    private Vector3 oldMousePosition;
    private Vector3 newMousePosition;
    public void Update()
    {
        if (SceneManager.GetActiveScene().name == "ServerScene" && genericObject.getIsInteractable())
        {
            if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftShift) && mouseOverObject())
            {
                arcball = true; anyArcball = true;
                oldMousePosition = Input.mousePosition;
                newMousePosition = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(1) || !Input.GetKey(KeyCode.LeftShift))
            {
                arcball = false; anyArcball = false;
            }

            if (arcball)
            {
                oldMousePosition = newMousePosition;
                newMousePosition = Input.mousePosition;
                if (newMousePosition != oldMousePosition)
                {
                    var vector2 = getArcballVector(newMousePosition);
                    var vector1 = getArcballVector(oldMousePosition);
                    float angle = (float)Math.Acos(Vector3.Dot(vector1, vector2));
                    var axis_cam = Vector3.Cross(vector1, vector2);

                    Matrix4x4 viewMatrix = Camera.main.worldToCameraMatrix;
                    Matrix4x4 modelMatrix = genericObject.transform.localToWorldMatrix;
                    Matrix4x4 cameraToObjectMatrix = Matrix4x4.Inverse(viewMatrix * modelMatrix);
                    var axis_world = cameraToObjectMatrix * axis_cam;

                    if (float.IsFinite(genericObject.transform.position.x))
                    {
                        genericObject.transform.RotateAround(genericObject.transform.position, axis_world, 2 * Mathf.Rad2Deg * angle);
                    }
                }
            }
        }
    }

    private bool isBlockedByUI()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);

        if (raysastResults.Count > 0)
        {
            if (raysastResults[0].gameObject.layer == LayerMask.NameToLayer("UI"))
            {
                return true;
            }
        }
        return false;
    }

    private bool mouseOverObject()
    {
        Ray ray = GlobalCtrl.Singleton.currentCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == GetComponent<MeshCollider>())
            {
                return true;
            }
        }
        return false;
    }

    private Vector3 getArcballVector(Vector3 inputPos)
    {
        Vector3 vector = CameraSwitcher.Singleton.currentCam.ScreenToViewportPoint(inputPos);
        vector = -vector;
        if (vector.x * vector.x + vector.y * vector.y <= 1)
        {
            vector.z = (float)Math.Sqrt(1 - vector.x * vector.x - vector.y * vector.y);
        }
        else
        {
            vector = vector.normalized;
        }
        return vector;
    }


    // offset for mouse interaction
    public Vector3 mouse_offset = Vector3.zero;
    private Stopwatch stopwatch;

    void OnMouseDown()
    {
        if (genericObject.getIsInteractable())
        {
            // Handle server GUI interaction
            if (EventSystem.current.IsPointerOverGameObject()) { return; }


            mouse_offset = genericObject.transform.position - GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(
             new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.1f));

            stopwatch = Stopwatch.StartNew();
            genericObject.isGrabbed = true;
            genericObject.processHighlights();
        }
    }

    void OnMouseDrag()
    {
        if (genericObject.getIsInteractable())
        {
            if (EventSystem.current.IsPointerOverGameObject()) { return; }
            Vector3 newPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.1f);
            genericObject.transform.position = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(newPosition) + mouse_offset;
        }
    }

    private void OnMouseUp()
    {
        if (genericObject.getIsInteractable())
        {
            if (EventSystem.current.IsPointerOverGameObject()) { return; }

            // reset outline
            genericObject.isGrabbed = false;


            stopwatch?.Stop();
            if (stopwatch?.ElapsedMilliseconds < 200)
            {
                genericObject.toggleMarkObject();
            }
            genericObject.processHighlights();
        }
    }


#endif
    #endregion
}

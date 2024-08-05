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
        //if (genericObject.getIsInteractable())
        //{
        //    if (EventSystem.current.IsPointerOverGameObject()) { return; }
        //    Vector3 newPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.1f);
        //    genericObject.transform.position = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(newPosition) + mouse_offset;
        //}
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

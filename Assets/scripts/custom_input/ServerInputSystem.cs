using RuntimeGizmos;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.UI;


// This script should be attached to the main camera of the server scene
public class ServerInputSystem : MonoBehaviour
{
    private float moveSpeed = 0.04f;
    private float turnSpeed = 1f;
    private Transform lastObjectClickedOn = null;
    private EventSystem system;

    private void Start()
    {
        var names = QualitySettings.names.ToList();
        QualitySettings.SetQualityLevel(names.IndexOf("Ultra"));
        var cursor_texture = Resources.Load<Texture2D>("customCursor/cursor");
        Cursor.SetCursor(cursor_texture, Vector3.zero, CursorMode.ForceSoftware);
        system = EventSystem.current;
    }

    private Transform mouseIsOverUIElement()
    {
        //check if mouse position is over any active and visible UI object
        foreach (MaskableGraphic uiElement in UICanvas.Singleton.GetComponentsInChildren<MaskableGraphic>())
        {
            if (uiElement.gameObject.activeInHierarchy && uiElement.enabled)
            {
                Vector2 tmpLocalPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(uiElement.rectTransform, Input.mousePosition, null, out tmpLocalPoint);
                if (uiElement.rectTransform.rect.Contains(tmpLocalPoint))
                {
                    return uiElement.transform;
                }
            }
        }
        return null;
    }

    private void getObjectClickedOn()
    {
        var target = mouseIsOverUIElement();
        if (target != null)
        {
            lastObjectClickedOn = target;
            return;
        }
        if (TransformGizmo.Singleton != null)
        {
            if (TransformGizmo.Singleton.hasAnyAxis())
            {
                lastObjectClickedOn = TransformGizmo.Singleton.transform;
                return;
            }
        }

        Ray ray = GlobalCtrl.Singleton.currentCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        for (int i = 0; i < hits.Length; i++)
        {
            lastObjectClickedOn = hits[i].transform;
            return;
            //var mol = hits[i].transform.GetComponentInParent<Molecule>();
            //var go = hits[i].transform.GetComponentInParent<GenericObject>();
            //if (mol != null)
            //{
            //    if (mol.getIsInteractable())
            //    {
            //        lastObjectClickedOn = mol.transform;
            //        return;
            //    }
            //}
            //if (go != null)
            //{
            //    if (go.getIsInteractable())
            //    {
            //        lastObjectClickedOn = go.transform;
            //        return;
            //    }
            //}
        }
        lastObjectClickedOn = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (system == null)
        {
            system = EventSystem.current;
            if (system == null) return;
        }
        GameObject currentObject = system.currentSelectedGameObject;
        if (currentObject != null)
        {
            InputField inputField = currentObject.GetComponent<InputField>();
            if (inputField != null)
            {
                if (inputField.isFocused)
                {
                    return;
                }
            }
            else
            {
                TMP_InputField tmpInput = currentObject.GetComponent<TMP_InputField>();
                if (tmpInput != null)
                {
                    if (tmpInput.isFocused)
                    {
                        return;
                    }
                }
            }
        }
        if (GlobalCtrl.Singleton.currentCamera == GlobalCtrl.Singleton.mainCamera)
        {
            if (!CreateInputField.Singleton.gameObject.activeSelf)
            {
                doCameraMovement();
            }
        }
        cameraMouseManipulation();
        createStuff();
        selectWholeMolecule();
        otherShortcuts();

        if (Input.GetKey(KeyCode.LeftShift))
        {
            moveSpeed = 0.1f;
        }
        else
        {
            moveSpeed = 0.04f;
        }
    }

    Dictionary<GameObject, bool> uiState = new Dictionary<GameObject, bool>();
    void otherShortcuts()
    {
        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl) && !CreateInputField.Singleton.gameObject.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (SettingsData.allowedTransitionInteractions.HasFlag(TransitionManager.InteractionType.BUTTON_PRESS))
                {
                    Transform obj;
                    if (SettingsData.hoverGazeAsSelection)
                    {
                        obj = GlobalCtrl.Singleton.getFirstHoveredObject();
                    }
                    else
                    {
                        obj = GlobalCtrl.Singleton.getFirstMarkedObject();
                    }

                    if (obj != null)
                    {
                        var from_id = NetworkManagerServer.Singleton.Server.Clients.First();
                        if (from_id == null) return;
                        TransitionManager.Singleton.initializeTransitionServer(obj, TransitionManager.InteractionType.BUTTON_PRESS, from_id.Id);
                    }
                    else
                    {
                        // Nothing is marked in the server scene
                        // send transition request to client
                        EventManager.Singleton.RequestTransition(TransitionManager.InteractionType.BUTTON_PRESS);
                    }

                }
            }
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                var list = GlobalCtrl.Singleton.getAllMarkedObjects();
                if (list.Count > 0)
                {
                    foreach(var obj in list)
                    {
                        var mol = obj.GetComponent<Molecule>();
                        if (mol != null)
                        {
                            GlobalCtrl.Singleton.deleteMoleculeUI(mol);
                        }
                        var go = obj.GetComponent<GenericObject>();
                        if (go != null)
                        {
                            GenericObject.delete(go);
                        }
                    }
                }
                EventManager.Singleton.ForwardDeleteMarkedRequest();
            }
            if (Input.GetKeyDown(KeyCode.H))
            {
                if (uiState.Count > 0) // return from hide
                {
                    foreach (var child in  uiState)
                    {
                        child.Key.SetActive(child.Value);
                    }
                    uiState.Clear();
                }
                else // do the hide
                {
                    foreach (Transform child in UICanvas.Singleton.transform)
                    {
                        if (child.gameObject.layer == LayerMask.NameToLayer("UI"))
                        {
                            uiState[child.gameObject] = child.gameObject.activeSelf;
                            child.gameObject.SetActive(false);
                        }
                    }
                }
            }
            if (!Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Return))
            {
                if (StudyTaskManager.Singleton != null)
                {
                    StudyTaskManager.Singleton.startAndFinishTask();
                }
            }
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Return))
            {
                if (StudyTaskManager.Singleton != null)
                {
                    StudyTaskManager.Singleton.restartTask();
                }

            }

            if (Input.GetMouseButtonDown(0))
            {
                getObjectClickedOn();
            }

            if (Input.GetMouseButtonUp(0) && lastObjectClickedOn == null)
            {
                foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
                {
                    mol.markMoleculeUI(false);
                }
                if (GenericObject.objects != null)
                {
                    foreach (var obj in GenericObject.objects.Values)
                    {
                        obj.isMarked = false;
                        obj.processHighlights();
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Transform targetMolecule = GlobalCtrl.Singleton.GetLastMarkedMoleculeTransform();
                if (TransformGizmo.Singleton.enabled && TransformGizmo.Singleton.mainTargetRoot) targetMolecule = TransformGizmo.Singleton.mainTargetRoot;

                TransformGizmo.Singleton.enabled = !TransformGizmo.Singleton.enabled;

                if(TransformGizmo.Singleton.enabled) TransformGizmo.Singleton.AddTarget(targetMolecule);
            }

            //if (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftAlt))
            //{
            //    if (lastObjectClickedOn != null)
            //    {
            //        var amount = 0.1f * Input.GetAxis("Mouse X");
            //        if (lastObjectClickedOn.transform.localScale.x > 0.1f)
            //        {
            //            lastObjectClickedOn.transform.localScale += amount * Vector3.one;
            //        }
            //        else
            //        {
            //            if (amount > 0f)
            //            {
            //                lastObjectClickedOn.transform.localScale += amount * Vector3.one;
            //            }
            //        }

            //    }
            //}
        }
    }

    /// <summary>
    /// Implements WASD movement and mouse-based turning.
    /// </summary>
    private void doCameraMovement()
    {
        if (Input.GetKey(KeyCode.LeftControl)) return;

        if (Input.GetKey(KeyCode.W))
        {
            transform.position += moveSpeed * transform.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.position -= moveSpeed * transform.forward;
        }
        if (Input.GetKey(KeyCode.D))
        {
            Vector3 rotated = Quaternion.AngleAxis(90, Vector3.up) * transform.forward;
            rotated.y = 0;
            transform.position += moveSpeed * rotated;
        }
        if (Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.RightShift))
        {
            Vector3 rotated = Quaternion.AngleAxis(90, Vector3.up) * transform.forward;
            rotated.y = 0f;
            transform.position -= moveSpeed * rotated;
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.position += moveSpeed * Vector3.up;
        }
        if (Input.GetKey(KeyCode.F))
        {
            transform.position -= moveSpeed * Vector3.up;
        }
    }

    private void cameraMouseManipulation()
    {
        if (Input.GetMouseButton(1))
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            if (!Atom.anyArcball)
#endif
            {
                float delta_x = Input.GetAxis("Mouse X") * turnSpeed;
                transform.RotateAround(transform.position, transform.up, delta_x);
                float delta_y = Input.GetAxis("Mouse Y") * turnSpeed;
                transform.RotateAround(transform.position, -transform.right, delta_y);
                transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
            }
        }
        if (Input.GetMouseButton(2))
        {
            float move_left_right = Input.GetAxis("Mouse X");
            float move_up_down = Input.GetAxis("Mouse Y");
            Vector3 rotated = Quaternion.AngleAxis(90, Vector3.up) * transform.forward;
            transform.position -= moveSpeed * (move_left_right * rotated + move_up_down * transform.up);
        }
    }

    private void createStuff()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            CreateInputField.Singleton.gameObject.SetActive(true);
            CreateInputField.Singleton.input_field.Select();
            CreateInputField.Singleton.input_field.ActivateInputField();
        }
    }

    /// <summary>
    /// Selects the whole molecule belonging to the last selected atom.
    /// </summary>
    private void selectWholeMolecule()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.A))
        {
            //get last marked atom
            if (Atom.markedAtoms.Count > 0)
            {
                Atom marked = Atom.markedAtoms[Atom.markedAtoms.Count - 1];
                if (marked != null)
                {
                    marked.m_molecule.markMoleculeUI(true);
                }
            }
        }
    }
}

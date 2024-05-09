using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// This script should be attached to the main camera of the server scene
public class ServerInputSystem : MonoBehaviour
{
    private float moveSpeed = 0.04f;
    private float turnSpeed = 1f;

    // Update is called once per frame
    void Update()
    {
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

        if (Input.GetKey(KeyCode.LeftShift))
        {
            moveSpeed = 0.1f;
        }
        else
        {
            moveSpeed = 0.04f;
        }
    }

    /// <summary>
    /// Implements WASD movement and mouse-based turning.
    /// </summary>
    private void doCameraMovement()
    {
        if (!Input.GetKey(KeyCode.LeftShift) || !Input.GetKey(KeyCode.LeftControl))
        {
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
            if (Input.GetKey(KeyCode.F))
            {
                transform.position += moveSpeed * Vector3.up;
            }
            if (Input.GetKey(KeyCode.C))
            {
                transform.position -= moveSpeed * Vector3.up;
            }
        }
    }

    private void cameraMouseManipulation()
    {
        if (Input.GetMouseButton(1))
        {
#if !WINDOWS_UWP
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
        if (Input.GetKeyDown(KeyCode.Space))
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
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.A))
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

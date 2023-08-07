using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// This script should be attached to the main camera of the server scene
public class ServerInputSystem : MonoBehaviour
{
    private float moveSpeed = 0.01f;
    private float turnSpeed = 1f;

    // Update is called once per frame
    void Update()
    {
        if (GlobalCtrl.Singleton.currentCamera == GlobalCtrl.Singleton.mainCamera)
        {
            doMovement();
        }
        createStuff();
        selectWholeMolecule();
    }

    private void doMovement()
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
            transform.position += moveSpeed * rotated;
        }
        if (Input.GetKey(KeyCode.A))
        {
            Vector3 rotated = Quaternion.AngleAxis(90, Vector3.up) * transform.forward;
            transform.position -= moveSpeed * rotated;
        }
        if (Input.GetKey(KeyCode.Space))
        {
            transform.position += moveSpeed * transform.up;
        }
        if (!Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.C))
        {
            transform.position -= moveSpeed * transform.up;
        }
        if (Input.GetMouseButton(1))
        {
            float delta_x = Input.GetAxis("Mouse X") * turnSpeed;
            transform.RotateAround(transform.position, transform.up, delta_x);
            float delta_y = Input.GetAxis("Mouse Y") * turnSpeed;
            transform.RotateAround(transform.position, -transform.right, delta_y);
            transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
        }
    }

    private void createStuff()
    {
        
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.C))
        {
            GlobalCtrl.Singleton.createAtomUI("C");
        }
    }

    private void selectWholeMolecule()
    {
        if (Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.A))
        {
            Atom marked = (Atom)GlobalCtrl.Singleton.getNextMarked(1);
            if (marked != null)
            {
                marked.m_molecule.markMoleculeUI(true);
            }
        }
    }
}

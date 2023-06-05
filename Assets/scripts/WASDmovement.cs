using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WASDmovement : MonoBehaviour
{
    private float moveSpeed = 0.01f;
    private float turnSpeed = 1f;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.position += moveSpeed * transform.forward;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            transform.position -= moveSpeed * transform.forward;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            Vector3 rotated = Quaternion.AngleAxis(90, Vector3.up) * transform.forward;
            transform.position += moveSpeed * rotated;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            Vector3 rotated = Quaternion.AngleAxis(90, Vector3.up) * transform.forward;
            transform.position -= moveSpeed * rotated;
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
}

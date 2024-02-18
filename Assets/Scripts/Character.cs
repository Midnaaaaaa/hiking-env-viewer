using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    float rotationX = 0f;
    float rotationY = 0f;
    public float velocity = 5f; 

    public float sensitivity = 15f;

    private void Update()
    {
        rotationY += Input.GetAxis("Mouse X") * sensitivity;
        rotationX += Input.GetAxis("Mouse Y") * -1 * sensitivity;
        transform.localEulerAngles = new Vector3(rotationX, rotationY, 0);

        if (Input.GetKey(KeyCode.W))
        {
            transform.position += transform.forward * velocity;
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.position += -transform.forward * velocity;
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.position += transform.right * velocity;
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.position += -transform.right * velocity;
        }
    }
}

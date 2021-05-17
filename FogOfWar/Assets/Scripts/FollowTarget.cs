using System;
using System.Collections.Generic;
using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    public GameObject target;
    [Tooltip("镜头缩放速率")]
    public float zoomSpeed = 30;
    [Tooltip("镜头移动速率")]
    public float movingSpeed = 1;
    [Tooltip("镜头旋转速率")]
    public float rotateSpeed = 1; 
    [Tooltip("设置距离角色的距离")]
    public float distance = 20;
    [Tooltip("设置镜头斜视的角度")]
    public float viewAngle = 30;

    void Start()
    {
        if (target)
        {
            transform.rotation = Quaternion.Euler(viewAngle, target.transform.rotation.eulerAngles.y, 0);
            transform.position = transform.rotation * new Vector3(0, 0, -distance) + target.transform.position;
        }
    }

    void Update()
    {
        if (target != null)
        {
            if (Input.GetMouseButton(0))
            {
                float deltaOffsetX = Input.GetAxis("Mouse X") * movingSpeed;
                float deltaOffsetY = Input.GetAxis("Mouse Y") * movingSpeed;
                Quaternion rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
                transform.position = rotation * new Vector3(-deltaOffsetX, 0, -deltaOffsetY) + transform.position;
            }
            else
            {
                if (Input.GetAxis("Mouse ScrollWheel") != 0)
                {
                    distance += -Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
                }
                if (Input.GetMouseButton(1))
                {
                    float deltaRotationX = Input.GetAxis("Mouse X") * rotateSpeed;
                    float deltaRotationY = -Input.GetAxis("Mouse Y") * rotateSpeed;
                    transform.Rotate(0, deltaRotationX, 0, Space.World);
                    transform.Rotate(deltaRotationY, 0, 0);
                }
                else
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.Euler(transform.rotation.eulerAngles.x, target.transform.rotation.eulerAngles.y, 0
                    ), Time.deltaTime * 2);
                }
                transform.position = transform.rotation * new Vector3(0, 0, -distance) + target.transform.position;
            }
        }
    }
}

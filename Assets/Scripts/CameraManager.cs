using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CameraManager : MonoBehaviour
{
    // PUBLIC FIELDS
    public GameObject target;
    public Vector3 trackingOffset = new Vector3(0, 0, 0);
    public Vector3 rotationOffset = new Vector3(10, 90, 0);

    // LIFECYCLE

    private void Update()
    {
        // Rounded position
        transform.position = target.transform.position + target.transform.rotation * trackingOffset;

        // Rounded angle
        transform.rotation = target.transform.rotation * Quaternion.Euler(rotationOffset.x, rotationOffset.y, rotationOffset.z);
    }
}

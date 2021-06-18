using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CameraManager : MonoBehaviour
{
    // PUBLIC FIELDS
    public GameObject target;
    public Vector3 trackingOffset = new Vector3(0, 0, 0);

    // LIFECYCLE

    private void Update()
    {
        transform.position = target.transform.position + trackingOffset;
    }
}

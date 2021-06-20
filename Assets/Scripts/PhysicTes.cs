using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicTes : MonoBehaviour
{
    // PROPERTIES
    public Vector3 Normal { get => transform.rotation * Vector3.up; }

    // PUBLIC FIELDS
    public float debugLineLength = 5;
    public Vector3 force = new Vector3(1, 0, 0);

    // LIFECYCLE

    private void OnDrawGizmos()
    {
        // Normal
        Debug.DrawLine(transform.position, transform.position + transform.rotation * Vector3.up * debugLineLength, Color.red);

        // Force
        Gizmos.DrawCube(transform.position - Vector3.right * 10, Vector3.one);
        Debug.DrawLine(transform.position - Vector3.right * 10, transform.position - Vector3.right * 10 + force * debugLineLength, Color.blue);

        // Cross
        var dot = Vector3.Dot(Normal, force);
        var sqrDot = Mathf.Pow(dot, 2);
        var cross = Vector3.Cross(force, Normal);
        Debug.Log("dot=" + sqrDot + ", cross=" + cross.sqrMagnitude);
        Debug.DrawLine(transform.position - Vector3.right * 10, transform.position - Vector3.right * 10 + Vector3.up * dot * debugLineLength, Color.yellow);
        Debug.DrawLine(transform.position - Vector3.right * 10, transform.position - Vector3.right * 10 + cross * debugLineLength, Color.green);

        // Resulting force
        if (dot < 0)
        {
            var resultingForce = force * Mathf.Abs(sqrDot);
            resultingForce = Normal * -sqrDot;
            Debug.DrawLine(transform.position, transform.position + resultingForce * debugLineLength, Color.green);
        }
    }
}

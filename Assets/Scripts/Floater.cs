using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Floater : MonoBehaviour
{
    // PUBLIC FIELDS
    public new Rigidbody rigidbody;
    public WaterManager waterManager;
    public float depthBeforeSubmerged = 1;
    public float displacementAmount = 3;
    public int floaterCount = 1;
    public float waterDrag = .99f;
    public float angularDrag = .5f;

    // LIFECYCLE

    private void FixedUpdate()
    {
        // Local gravity
        rigidbody.AddForceAtPosition(Physics.gravity / floaterCount, transform.position, ForceMode.Acceleration);

        // Get local grid coordinates
        waterManager.GetGridCoordinates(transform.position, out int x, out int y);

        // Get local water data
        float waterHeight = waterManager.WaterHeightGrid[x, y];
        Vector3 waterNormal = waterManager.WaterNormalGrid[x, y];
        Vector2 waterVelocity = waterManager.WaterVelocityGrid[x, y];

        // If under water
        if (transform.position.y < waterHeight)
        {
            // Push upward
            float displacementMultiplier = Mathf.Clamp01((waterHeight - transform.position.y) / depthBeforeSubmerged) * displacementAmount;
            rigidbody.AddForceAtPosition(waterNormal * Physics.gravity.magnitude * displacementMultiplier,
                                         transform.position,
                                         ForceMode.Acceleration);
            rigidbody.AddForce(displacementMultiplier * -rigidbody.velocity * waterDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);
            rigidbody.AddTorque(displacementMultiplier * -rigidbody.angularVelocity * angularDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);

            // Push according to water velocity
            rigidbody.AddForceAtPosition(new Vector3(waterVelocity.x, 0, waterVelocity.y) * 5, transform.position, ForceMode.Acceleration);
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshFilter))]
public class BoatManager : MonoBehaviour
{
    // STRUCT
    public struct FaceData
    {
        public FaceData(Vector3 position, Vector3 normal, float surface)
        {
            Position = position;
            Normal = normal;
            Surface = surface;
        }

        public Vector3 Position { get; }
        public Vector3 Normal { get; }
        public float Surface { get; }
    }

    // PUBLIC FIELDS (water physic)
    public WaterManager waterManager;
    public float depthBeforeSubmerged = 1;
    public float floatingHeight = 1;
    public float displacementAmount = 3;
    public float waterDrag = .99f;
    public float angularDrag = .5f;

    // PUBLIC FIELDS (player physics)
    public float forwardStrength = 50;
    public float backwardStrength = 25;
    public float lateralStrength = 5;
    public float rotativeStrength = 20;
    public float waterSurfaceDistToPaddle = 1;

    // PUBLIC FIELDS (debug)
    public bool debugMode = false;
    public float debugPositionRadius = .25f;
    public float debugNormalLength = 1;

    // PRIVATE FIELDS
    private new Rigidbody rigidbody;
    private FaceData[] facesData;
    private float totalSurface;

    // LIFECYCLE

    private void Awake()
    {
        // Get components
        rigidbody = GetComponent<Rigidbody>();

        // Get meshes data
        var mesh = GetComponent<MeshFilter>().mesh;
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;

        // Populate face list
        var faceList = new List<FaceData>();
        totalSurface = 0;
        var triangleVertices = new Vector3[3];
        int i = 0;
        foreach (var index in triangles)
        {
            triangleVertices[i] = vertices[index];

            if (++i > 2)
            {
                i = 0;
                var faceTrianglesGravity = (triangleVertices[0] + triangleVertices[1] + triangleVertices[2]) / 3f;
                var crossProduct = Vector3.Cross(triangleVertices[0] - triangleVertices[1], triangleVertices[0] - triangleVertices[2]);
                float surface = crossProduct.magnitude * .5f;
                faceList.Add(new FaceData(faceTrianglesGravity, crossProduct.normalized, surface));
                totalSurface += surface;
            }
        }
        facesData = faceList.ToArray();
    }

    private void FixedUpdate()
    {
        // Player inputs
        {
            // Get boat grid coordinates
            waterManager.GetGridCoordinates(transform.position, out int x, out int z);

            // Get local water data
            float waterHeight = waterManager.WaterHeightGrid[x, z] + waterManager.transform.position.y;

            // If near the water surface
            if (Mathf.Abs(transform.position.y - waterHeight) < waterSurfaceDistToPaddle)
            {
                // Paddle (forward direction)
                float forwardMovement = -Input.GetAxis("Vertical");
                forwardMovement *= forwardMovement < 0 ? forwardStrength : backwardStrength;
                // Paddle (lateral direction)
                float lateralMovement = Input.GetAxis("Lateral") * lateralStrength;
                // Apply force
                var movementDirection = transform.rotation * new Vector3(forwardMovement, 0, lateralMovement);
                rigidbody.AddForce(movementDirection, ForceMode.Force);

                // Paddle (rotative direction)
                float rotativeMovement = Input.GetAxis("Horizontal") * rotativeStrength;
                // Apply torque
                rigidbody.AddTorque(new Vector3(0, rotativeMovement, 0), ForceMode.Force);
            }
        }

        // Water physics
        {
            var localToWorld = transform.localToWorldMatrix;
            for (int i = 0; i < facesData.Length; ++i)
            {
                // Get face data
                var faceWorldPosition = localToWorld.MultiplyPoint3x4(facesData[i].Position);
                var faceWorldFloatingHeight = faceWorldPosition.y + floatingHeight;
                float faceSurface = facesData[i].Surface;
                float faceSurfaceRatio = faceSurface / totalSurface;

                // Local gravity
                rigidbody.AddForceAtPosition(Physics.gravity * faceSurfaceRatio, faceWorldPosition, ForceMode.Acceleration);

                // Get local grid coordinates
                waterManager.GetGridCoordinates(faceWorldPosition, out int faceX, out int faceZ);

                // Get local water height
                float waterHeight = waterManager.WaterHeightGrid[faceX, faceZ] + waterManager.transform.position.y;

                // If under water
                if (faceWorldFloatingHeight < waterHeight)
                {
                    // Get local water data
                    Vector3 waterNormal = waterManager.WaterNormalGrid[faceX, faceZ];
                    Vector2 waterVelocity = waterManager.WaterVelocityGrid[faceX, faceZ];
                    Vector3 waterForce = Vector3.ProjectOnPlane(new Vector3(waterVelocity.x, 0, waterVelocity.y), waterNormal);

                    // Push upward
                    float displacementMultiplier = Mathf.Clamp01((waterHeight - faceWorldFloatingHeight) / depthBeforeSubmerged) * displacementAmount;
                    rigidbody.AddForceAtPosition(waterNormal * Physics.gravity.magnitude * faceSurfaceRatio * displacementMultiplier,
                                                 faceWorldPosition,
                                                 ForceMode.Acceleration);
                    rigidbody.AddForce(faceSurfaceRatio * displacementMultiplier * -rigidbody.velocity * waterDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);
                    rigidbody.AddTorque(faceSurfaceRatio * displacementMultiplier * -rigidbody.angularVelocity * angularDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);

                    // Push according to water velocity
                    var faceWorldNormal = transform.rotation * facesData[i].Normal;
                    var dot = Vector3.Dot(faceWorldNormal, waterForce);
                    if (dot < 0)
                    {
                        var sqrDot = Mathf.Pow(dot, 2);
                        var resultingForce = faceWorldNormal * -sqrDot;
                        rigidbody.AddForceAtPosition(resultingForce * faceSurface * waterForce.magnitude, faceWorldPosition, ForceMode.Force);
                    }
                }
            }
        }
    }

    // PRIVATE METHODS

    private void OnDrawGizmos()
    {
        if (debugMode && facesData != null)
        {
            var localToWorld = transform.localToWorldMatrix;
            for (int i = 0; i < facesData.Length; ++i)
            {
                var faceWorldPosition = localToWorld.MultiplyPoint3x4(facesData[i].Position);
                var faceWorldNormal = transform.rotation * facesData[i].Normal;
                Gizmos.DrawSphere(faceWorldPosition, debugPositionRadius);
                Gizmos.DrawLine(faceWorldPosition, faceWorldPosition + faceWorldNormal * debugNormalLength);
                Handles.Label(faceWorldPosition, facesData[i].Surface.ToString());
            }
        }
    }
}

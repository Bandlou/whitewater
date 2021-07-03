using System;
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

    // PUBLIC FIELDS
    [Header("Air physics")]
    public float airLinearDrag = .005f;
    public float airAngularDrag = .5f;
    [Header("Water physics")]
    public float waterLinearDrag = .05f;
    public float waterAngularDrag = .5f;
    public WaterManager waterManager;
    public float depthBeforeSubmerged = 1;
    public float floatingHeight = 1;
    public float displacementAmount = 3;
    [Header("Player physics")]
    public float forwardStrength = 50;
    public float backwardStrength = 25;
    public float lateralStrength = 5;
    public float rotativeStrength = 20;
    public float waterSurfaceDistToPaddle = 1;
    [Header("Debug")]
    public bool debugMode = false;
    public float debugPositionRadius = .25f;
    public float debugNormalLength = 1;

    // PRIVATE FIELDS
    private new Rigidbody rigidbody;
    private bool canPaddle = false;
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

            // Check if can paddle
            canPaddle = false;
            if (waterManager.AreCoordinatesValid(x, z))
            {
                // Get local water data
                float waterHeight = waterManager.WaterGrid[x, z].height + waterManager.transform.position.y;

                // If near the water surface
                if (Mathf.Abs(transform.position.y - waterHeight) < waterSurfaceDistToPaddle)
                    canPaddle = true;
            }

            // Apply inputs if authorized
            if (canPaddle)
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

                // Check if underwater
                bool underwater = false;
                if (waterManager.AreCoordinatesValid(faceX, faceZ))
                {
                    // Get local water height
                    float waterHeight = waterManager.WaterGrid[faceX, faceZ].height + waterManager.transform.position.y;
                    // Update underwater status
                    underwater = faceWorldFloatingHeight < waterHeight;
                }

                // Apply physics
                Vector3 faceWorldNormal = transform.rotation * facesData[i].Normal;
                if (underwater) // Water physics
                {
                    // Get local water height
                    float waterHeight = waterManager.WaterGrid[faceX, faceZ].height + waterManager.transform.position.y;

                    // Get local water data
                    Vector3 waterNormal = waterManager.WaterGrid[faceX, faceZ].normal;
                    Vector2 waterVelocity = waterManager.WaterGrid[faceX, faceZ].velocity;
                    Vector3 waterForce = Vector3.ProjectOnPlane(new Vector3(waterVelocity.x, 0, waterVelocity.y), waterNormal);

                    // Buoyancy
                    float displacementMultiplier = Mathf.Clamp01((waterHeight - faceWorldFloatingHeight) / depthBeforeSubmerged) * displacementAmount;
                    rigidbody.AddForceAtPosition(waterNormal * Physics.gravity.magnitude * faceSurfaceRatio * displacementMultiplier,
                                                 faceWorldPosition,
                                                 ForceMode.Acceleration);

                    // Linear drag (DragCoeff * CrossSectionalArea * velocity^2)
                    float linearDragDot = Vector3.Dot(faceWorldNormal, rigidbody.velocity.normalized);
                    if (linearDragDot > 0)
                    {
                        float sqrDot = Mathf.Pow(linearDragDot, 2);
                        float crossSectionalArea = faceSurface * sqrDot;
                        rigidbody.AddForce(waterLinearDrag * crossSectionalArea * rigidbody.velocity.sqrMagnitude * -rigidbody.velocity.normalized * Time.fixedDeltaTime,
                                           ForceMode.VelocityChange);
                    }

                    // Angular drag (DragCoeff * CrossSectionalArea * velocity^2)
                    float angle = Mathf.Rad2Deg * rigidbody.angularVelocity.magnitude;
                    Vector3 axis = rigidbody.angularVelocity.normalized;
                    Vector3 nextFaceWorldPosition = Quaternion.AngleAxis(angle, axis) * (faceWorldPosition - transform.position);
                    Vector3 angularMovement = nextFaceWorldPosition - (faceWorldPosition - transform.position);

                    float angularDragDot = Vector3.Dot(faceWorldNormal, angularMovement.normalized);
                    if (angularDragDot > 0)
                    {
                        float sqrDot = Mathf.Pow(angularDragDot, 2);
                        float crossSectionalArea = faceSurface * sqrDot;
                        rigidbody.AddTorque(waterAngularDrag * crossSectionalArea * rigidbody.angularVelocity.sqrMagnitude * -rigidbody.angularVelocity.normalized * Time.fixedDeltaTime,
                                            ForceMode.VelocityChange);
                    }

                    // Current (rushing water)
                    float currentDot = Vector3.Dot(faceWorldNormal, waterForce.normalized);
                    if (currentDot < 0)
                    {
                        float sqrDot = Mathf.Pow(currentDot, 2);
                        Vector3 resultingForce = faceWorldNormal * -sqrDot;
                        rigidbody.AddForceAtPosition(resultingForce * faceSurface * waterForce.magnitude, faceWorldPosition, ForceMode.Force);
                    }
                }
                else // Air physics
                {
                    // Linear drag (DragCoeff * CrossSectionalArea * velocity^2)
                    float linearDragDot = Vector3.Dot(faceWorldNormal, rigidbody.velocity.normalized);
                    if (linearDragDot > 0)
                    {
                        float sqrDot = Mathf.Pow(linearDragDot, 2);
                        float crossSectionalArea = faceSurface * sqrDot;
                        rigidbody.AddForce(airLinearDrag * crossSectionalArea * rigidbody.velocity.sqrMagnitude * -rigidbody.velocity.normalized * Time.fixedDeltaTime, ForceMode.VelocityChange);
                    }

                    // Angular drag (DragCoeff * CrossSectionalArea * velocity^2)
                    float angle = Mathf.Rad2Deg * rigidbody.angularVelocity.magnitude;
                    Vector3 axis = rigidbody.angularVelocity.normalized;
                    Vector3 nextFaceWorldPosition = Quaternion.AngleAxis(angle, axis) * (faceWorldPosition - transform.position);
                    Vector3 angularMovement = nextFaceWorldPosition - (faceWorldPosition - transform.position);

                    float angularDragDot = Vector3.Dot(faceWorldNormal, angularMovement.normalized);
                    if (angularDragDot > 0)
                    {
                        float sqrDot = Mathf.Pow(angularDragDot, 2);
                        float crossSectionalArea = faceSurface * sqrDot;
                        rigidbody.AddTorque(airAngularDrag * crossSectionalArea * rigidbody.angularVelocity.sqrMagnitude * -rigidbody.angularVelocity.normalized * Time.fixedDeltaTime,
                                            ForceMode.VelocityChange);
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

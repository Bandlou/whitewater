using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class WaterManager : MonoBehaviour
{
    // PUBLIC FIELDS
    public Vector2Int gridSize = new Vector2Int(100, 50);

    // PROPERTIES
    public float[,] WaterHeightGrid { get => waterHeightGrid; }
    public Vector3[,] WaterNormalGrid { get => waterNormalGrid; }
    public Vector2[,] WaterVelocityGrid { get => waterVelocityGrid; }

    // PRIVATE FIELDS
    private float[,] waterHeightGrid;
    private Vector3[,] waterNormalGrid;
    private Vector2[,] waterVelocityGrid;

    // LIFECYCLE

    private void Awake()
    {
        // Initialize water height grid
        waterHeightGrid = new float[gridSize[0], gridSize[1]];
        for (int x = 0; x < waterHeightGrid.GetLength(0); ++x)
            for (int z = 0; z < waterHeightGrid.GetLength(1); ++z)
                waterHeightGrid[x, z] = (Mathf.Pow(x - gridSize[0] * .5f, 2) + Mathf.Pow(z - gridSize[1] * .5f, 2)) * .01f;

        // Initialize water velocity grid
        waterVelocityGrid = new Vector2[gridSize[0], gridSize[1]];
        for (int x = 0; x < waterHeightGrid.GetLength(0); ++x)
            for (int z = 0; z < waterHeightGrid.GetLength(1); ++z)
                waterVelocityGrid[x, z] = Vector2.right;
    }

    private void Start()
    {
        // Initialize water mesh to the height grid
        var verticeList = new List<Vector3>();
        var uvList = new List<Vector2>();
        var triangleList = new List<int>();

        for (int x = 0; x < gridSize[0]; ++x)
        {
            for (int z = 0; z < gridSize[1]; ++z)
            {
                verticeList.Add(new Vector3(x, waterHeightGrid[x, z], z));
                uvList.Add(Vector2.zero);
                if (x < gridSize[0] - 1 && z < gridSize[1] - 1)
                {
                    int i = z + x * gridSize[1];
                    // First
                    triangleList.Add(i);
                    triangleList.Add(i + 1);
                    triangleList.Add(i + gridSize[1]);
                    // Second
                    triangleList.Add(i + gridSize[1]);
                    triangleList.Add(i + 1);
                    triangleList.Add(i + gridSize[1] + 1);
                }
            }
        }
        GetComponent<MeshFilter>().sharedMesh = new Mesh
        {
            vertices = verticeList.ToArray(),
            uv = uvList.ToArray(),
            triangles = triangleList.ToArray()
        };

        //var vertices = mesh.vertices;
        //int cellSize = Mathf.RoundToInt(Mathf.Pow(vertices.Length, .5f)) / 2;
        //for (int i = 0; i < vertices.Length; ++i)
        //{
        //    int x = Mathf.RoundToInt(vertices[i].x + cellSize);
        //    int y = Mathf.RoundToInt(vertices[i].z + cellSize);
        //    vertices[i].y = waterHeightGrid[x, y];
        //}
        //mesh.vertices = vertices;

        // Initialize water normal grid
        var mesh = GetComponent<MeshFilter>().sharedMesh;
        mesh.RecalculateNormals();
        var vertices = mesh.vertices;
        var normals = mesh.normals;
        waterNormalGrid = new Vector3[gridSize[0], gridSize[1]];
        for (int i = 0; i < vertices.Length; ++i)
        {
            int x = Mathf.RoundToInt(vertices[i].x);
            int z = Mathf.RoundToInt(vertices[i].z);
            waterNormalGrid[x, z] = new Vector3(normals[i].x, normals[i].y, normals[i].z);
        }
    }

    // PUBLIC METHODS

    public void GetGridCoordinates(Vector3 position, out int x, out int y)
    {
        x = Mathf.RoundToInt(position.x - transform.position.x);
        y = Mathf.RoundToInt(position.z - transform.position.z);
    }
}

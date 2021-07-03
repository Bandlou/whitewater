using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class WaterManager : MonoBehaviour
{
    // STRUCT
    public struct WaterData
    {
        public float height;
        public Vector3 normal;
        public Vector2 velocity;
    }

    // PUBLIC FIELDS
    public Vector2Int tilesInGrid = new Vector2Int(1, 1);
    public Vector2Int cellsInTile = new Vector2Int(200, 200);
    public float cellSize = .05f;
    public bool buildMode = false;
    public bool debugMode = false;

    // PROPERTIES
    public Vector2Int GridSize { get => tilesInGrid * cellsInTile; }
    public WaterData[,] WaterGrid { get => waterGrid; }

    // PRIVATE FIELDS
    private WaterData[,] waterGrid;

    // LIFECYCLE

    private void Awake()
    {
        if (buildMode)
        {
            waterGrid = new WaterData[GridSize[0], GridSize[1]];

            for (int x = 0; x < GridSize[0]; ++x)
            {
                for (int z = 0; z < GridSize[1]; ++z)
                {
                    waterGrid[x, z].height = 0;// (x + z) * 0.1f; // Mathf.Pow(x - gridSize[0] * .5f, 2) * .001f;
                    waterGrid[x, z].velocity = Vector2.right * 0;
                }
            }
        }
    }

    private void Start()
    {
        if (buildMode)
        {
            // Initialize water mesh to the height grid
            var verticeList = new List<Vector3>();
            var uvList = new List<Vector2>();
            var triangleList = new List<int>();

            for (int x = 0; x < GridSize[0]; ++x)
            {
                for (int z = 0; z < GridSize[1]; ++z)
                {
                    verticeList.Add(new Vector3(x * cellSize, waterGrid[x, z].height, z * cellSize));
                    uvList.Add(Vector2.up);
                    if (x < GridSize[0] - 1 && z < GridSize[1] - 1)
                    {
                        int i = z + x * GridSize[1];
                        // First
                        triangleList.Add(i);
                        triangleList.Add(i + 1);
                        triangleList.Add(i + GridSize[1]);
                        // Second
                        triangleList.Add(i + GridSize[1]);
                        triangleList.Add(i + 1);
                        triangleList.Add(i + GridSize[1] + 1);
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
            for (int i = 0; i < vertices.Length; ++i)
            {
                int x = Mathf.RoundToInt(vertices[i].x * (1f / cellSize));
                int z = Mathf.RoundToInt(vertices[i].z * (1f / cellSize));
                waterGrid[x, z].normal = new Vector3(normals[i].x, normals[i].y, normals[i].z);
            }
        }
        else
        {
            // Initialize water data grids
            var meshFilters = GetComponentsInChildren<MeshFilter>();
            waterGrid = new WaterData[GridSize[0], GridSize[1]];

            // Loop through all tiles
            foreach (var meshFilter in meshFilters)
            {
                // Get the tile grid offset by its local position
                // Tiles should have a local position from (0, 0, 0)
                // to (cellsInTile[0] * cellSize * tilesInGrid[0], 0, cellsInTile[1] * cellSize * tilesInGrid[1])
                // with 200x200 and 0.05 cell size: (10 * tilesInGrid[0], 0, 10 * tilesInGrid[1])

                var localPosition = meshFilter.gameObject.transform.localPosition;
                int xOffset = Mathf.RoundToInt(localPosition.x / cellSize);
                int zOffset = Mathf.RoundToInt(localPosition.z / cellSize);

                var mesh = meshFilter.sharedMesh;
                var vertices = mesh.vertices;
                var normals = mesh.normals;

                for (int i = 0; i < vertices.Length; ++i)
                {
                    int x = xOffset + Mathf.RoundToInt(vertices[i].x * (1f / cellSize));
                    int z = zOffset + Mathf.RoundToInt(vertices[i].z * (1f / cellSize));

                    if (x < GridSize[0] && z < GridSize[1])
                    {
                        waterGrid[x, z].height = vertices[i].y;
                        waterGrid[x, z].normal = new Vector3(normals[i].x, normals[i].y, normals[i].z);
                        waterGrid[x, z].velocity = Vector2.right * 0;
                    }
                }
            }
        }
    }

    // PUBLIC METHODS

    public void GetGridCoordinates(Vector3 position, out int x, out int z)
    {
        x = Mathf.RoundToInt((position.x - transform.position.x) * (1f / cellSize));
        z = Mathf.RoundToInt((position.z - transform.position.z) * (1f / cellSize));
    }

    public bool AreCoordinatesValid(int x, int z)
    {
        return x >= 0 && x <= GridSize[0] && z >= 0 && z < GridSize[1];
    }

    // PRIVATE METHODS

    private void OnDrawGizmos()
    {
        if (debugMode && waterGrid != null)
        {
            for (int x = 0; x < GridSize[0]; x += 5)
            {
                for (int z = 0; z < GridSize[1]; z += 5)
                {
                    var position = transform.position + new Vector3(x, 0, z) * cellSize;

                    // Velocity
                    var velocity = waterGrid[x, z].velocity;
                    var velocity3 = new Vector3(velocity.x, 0, velocity.y);
                    Debug.DrawLine(position, position + velocity3 * cellSize, Color.red);

                    // Normal
                    var normal = waterGrid[x, z].normal;
                    Debug.DrawLine(position, position + normal * cellSize, Color.green);
                }
            }
        }
    }
}

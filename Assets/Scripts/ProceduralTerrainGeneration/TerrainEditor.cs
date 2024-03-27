using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[ExecuteAlways]
public class TerrainEditor : MonoBehaviour
{
    [SerializeField] private Terrain terrain;
    [SerializeField] private TerrainData terrainData;
    [SerializeField][Range(0.0001f, 0.05f)] private float perlinStretch;
    [SerializeField][Range(0f, 1f)] private float heightMultiplier;
    [SerializeField] private AnimationCurve perlinSlope;
    [SerializeField] private List<GameObject> waypoints = new List<GameObject>();
    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField][Range(1f, 20f)] private int pathWidth = 1;
    [SerializeField][Range(0f, 0.5f)] private float pathHeight = 0.1f;
    [SerializeField][Range(0, 90f)] private float maxSlope = 40f;
    [SerializeField] private float waterLevel = 0f;
    [SerializeField] private float seed = 0f;

    private int resolution;
    private float[,] mesh;
    private float[,,] map;

    private void OnEnable()
    {
        PullTerrain();
    }

    public void PullTerrain()
    {
        resolution = terrainData.heightmapResolution;
        mesh = new float[resolution, resolution];
        mesh = terrainData.GetHeights(0, 0, resolution, resolution);
    }

    public void OnValidate()
    {
        PullTerrain();
        RedrawTerrainMesh();
        UpdateHeightMap();
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CreatePath();
        }
    }
    public void RandomizeSeed()
    {
        waypoints.Clear();
        seed = UnityEngine.Random.Range(-9999, 9999);
        RedrawTerrainMesh();
        UpdateHeightMap();
    }
    public void CreatePath()
    {
        // Make sure mouse is not over UI
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        // Do a raycast to get the position of the mouse
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            // Create a new waypoint at the hit position
            GameObject waypoint = new GameObject("Waypoint");
            waypoint.transform.position = hit.point;
            waypoints.Add(waypoint);
            RedrawTerrainMesh();
            UpdateHeightMap();
        }
    }
    private void RedrawTerrainMesh()
    {
        // Generate the terrain heights using perlin noise
        GenerateTerrain();

        // Draw the terrain textures
        DrawSplatmap();

        // Create the paths
        CreatePaths();

        // Smooth the different textures
        SmoothSplatmap();

        terrainData.SetAlphamaps(0, 0, map);
    }

    private void CreatePaths()
    {
        if (waypoints.Count < 2)
        {
            return;
        }

        // Update the navmesh
        navMeshSurface.BuildNavMesh();

        // Get the waypoints
        List<Vector3> points = new List<Vector3>();
        foreach (GameObject waypoint in waypoints)
        {
            // Sample the navmesh to get the closest point to the waypoint
            NavMeshHit hit;
            if (NavMesh.SamplePosition(waypoint.transform.position, out hit, 100f, NavMesh.AllAreas))
            {
                points.Add(hit.position);
            }
        }

        // Use the navmesh to create a path between the waypoints
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            NavMeshPath path = new NavMeshPath();

            NavMesh.CalculatePath(a, b, NavMesh.AllAreas, path);

            // Iterate between the corners and make a path between each one
            for (int j = 0; j < path.corners.Length - 1; j++)
            {
                // Iterate between the corners and make a path between each one
                var cornerA = path.corners[j];
                var cornerB = path.corners[j + 1];
                // Get the distance between the corners and use it to determine how many points to add
                float distance = Vector3.Distance(cornerA, cornerB);
                int numPoints = Mathf.RoundToInt(distance * 10);
                for (int k = 0; k < numPoints; k++)
                {
                    Vector3 point = Vector3.Lerp(cornerA, cornerB, (float)k / numPoints);
                    Vector2 terrainPos = getTerrainPosition(point);
                    mesh[(int)(terrainPos.x * resolution), (int)(terrainPos.y * resolution)] -= (pathHeight / 100f);

                    DrawPath(terrainPos, pathWidth, (pathHeight / 100f));
                }
            }
        }
    }

    private void GenerateTerrain()
    {
        // Then create the terrain mesh using the seed to add some randomness
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                // Use perlin noise to create the terrain alongside the seed
                mesh[x, y] = perlinSlope.Evaluate(Mathf.PerlinNoise((x + seed) * perlinStretch, (y + seed) * perlinStretch)) * heightMultiplier;
            }
        }

        // Subtract from the height the further away from the center of the terrain
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(resolution / 2, resolution / 2));
                mesh[x, y] -= distance / resolution / 5f;
            }
        }

        // Make sure the terrain is flat within 20 units of the edges 
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                if (x < 20 || x > resolution - 20 || y < 20 || y > resolution - 20)
                {
                    // Gradually lower the height of the terrain towards the edges
                    mesh[x, y] -= (20 - Mathf.Min(x, resolution - x, y, resolution - y)) / 20f / 5f;
                }
            }
        }
    }

    private Vector2 getTerrainPosition(Vector3 worldpos)
    {
        // Subtract the terrain position from the world position
        Vector3 terrainPos = terrain.transform.position;
        Vector3 localPos = worldpos - terrainPos;
        float xCoord = localPos.x / terrainData.size.x;
        float yCoord = localPos.z / terrainData.size.z;
        return new Vector2(yCoord, xCoord);
    }

    private void UpdateHeightMap()
    {
        terrainData.SetHeights(0, 0, mesh);
    }

    private void DrawPath(Vector2 position, int width, float height)
    {
        // Set the center position and the surrounding positions based on the width
        int x = (int)(position.x * resolution);
        int y = (int)(position.y * resolution);
        for (int i = -width; i < width; i++)
        {
            for (int j = -width; j < width; j++)
            {
                if (x + i >= 0 && x + i < resolution && y + j >= 0 && y + j < resolution)
                {
                    mesh[x + i, y + j] -= height;

                    // Paint the terrain with dirt texture
                    map[x + i, y + j, 0] = 0;
                    map[x + i, y + j, 1] = 1;
                    map[x + i, y + j, 2] = 0;
                    map[x + i, y + j, 3] = 0;
                }
            }
        }
    }

    private void DrawSplatmap()
    {
        map = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, 4];

        for (int x = 0; x < terrainData.alphamapWidth; x++)
        {
            for (int y = 0; y < terrainData.alphamapHeight; y++)
            {
                float normX = (float)x / terrainData.alphamapWidth;
                float normY = (float)y / terrainData.alphamapHeight;
                float steepness = terrainData.GetSteepness(normX, normY);
                float height = terrainData.GetHeight(x, y);
                float[] splat = new float[4];

                // If it is steep make it rock
                if (steepness > maxSlope)
                {
                    splat[0] = 0;
                    splat[1] = 0;
                    splat[2] = 1;
                    splat[3] = 0;
                }
                else
                {
                    splat[0] = 1;
                    splat[1] = 0;
                    splat[2] = 0;
                    splat[3] = 0;
                }

                // If it is low enough make it sand
                if (height < waterLevel)
                {
                    splat[0] = 0;
                    splat[1] = 0;
                    splat[2] = 0;
                    splat[3] = 1;
                }

                map[y, x, 0] = splat[0];
                map[y, x, 1] = splat[1];
                map[y, x, 2] = splat[2];
                map[y, x, 3] = splat[3];
            }
        }
    }

    private void SmoothSplatmap()
    {
        // Go back through and smooth the textures between the different types
        for (int x = 0; x < terrainData.alphamapWidth; x++)
        {
            for (int y = 0; y < terrainData.alphamapHeight; y++)
            {
                float[] splat = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    float total = 0;
                    int count = 0;
                    for (int j = -1; j < 2; j++)
                    {
                        for (int k = -1; k < 2; k++)
                        {
                            if (x + j >= 0 && x + j < terrainData.alphamapWidth && y + k >= 0 && y + k < terrainData.alphamapHeight)
                            {
                                total += map[y + k, x + j, i];
                                count++;
                            }
                        }
                    }
                    splat[i] = total / count;
                }
                map[y, x, 0] = splat[0];
                map[y, x, 1] = splat[1];
                map[y, x, 2] = splat[2];
                map[y, x, 3] = splat[3];
            }
        }
    }
    public float GetPerlinStretch()
    {
        return perlinStretch;
    }
    public void SetPerlinStretch(float stretch)
    {
        perlinStretch = stretch;
        RedrawTerrainMesh();
        UpdateHeightMap();
    }
    public float GetHeightMultiplier()
    {
        return heightMultiplier;
    }
    public void SetHeightMultiplier(float multiplier)
    {
        heightMultiplier = multiplier;
        RedrawTerrainMesh();
        UpdateHeightMap();
    }
    public float GetMaxSlope()
    {
        return maxSlope;
    }
    public void SetMaxSlope(float slope)
    {
        maxSlope = slope;
        RedrawTerrainMesh();
        UpdateHeightMap();
    }
    public int GetPathWidth()
    {
        return pathWidth;
    }
    public void SetPathWidth(int width)
    {
        pathWidth = width;
        RedrawTerrainMesh();
        UpdateHeightMap();
    }
    public int GetResolution()
    {
        return resolution;
    }
}
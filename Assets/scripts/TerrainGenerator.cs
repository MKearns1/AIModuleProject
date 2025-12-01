using TreeEditor;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;
using System;
using UnityEditor.Experimental.GraphView;
using System.Data;
using Unity.VisualScripting;
using System.IO;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainGenerator : MonoBehaviour
{
    Mesh mesh;
    MeshCollider meshcollider;

    Vector3[] vertices;
    int[] triangles;

    [Header("General Settings")]
    public int xSize = 26;
    public int zSize = 20;
    public float heightMultiplier = 2f;
    Color[] colours;


    [Header("Perlin Noise FBM")]
    [Range(0.0f, .2f)] public float PerlinScale = 1;
    public int PerlinOctaves;
    public float PerlinPersistence;
    public float PerlinLacunarity;
    public bool PerlinInvert;
    [Range(0.0f, 1f)] public float PerlinStrength;

    [Header("WorleyNoise")]
    [Range(0.0f, .2f)] public float WorleyScale = 1;
    public int WorleyOctaves;
    public float WorleyPersistence;
    public float WorleyLacunarity;
    public bool WorleyInvert;
    [Range(0.0f, 1f)] public float Worleystrength;

    [Header("Water")]
    public float PoolSharpness;
    public float PoolDepthMultiplier;
    public float PoolScale;
    public float IslandFactor;
    [Range(-50f, 0)] public float IslandFalloffSmoothness;
    [Range(0.0f, 1f)] public float IslandShape;
    public float WaterHeight;

    [Header("Artefact Settings")]
    public int DesiredNumOfArtefacts;
    public int DesiredNumOfEachArtefact;
    public bool ArtefactsBlockPathfinding;
    public bool DebugArtefactPaths;
    public List<GameObject> Artefacts = new List<GameObject>();
    public List<GameObject> ArtefactsInLevel = new List<GameObject>();

    [ContextMenu("Rebuild Node Grid")]
    void RebuildNodeGrid()
    {
        tiles.GenerateGridFromTerrain(this);
        ArtefactPaths.Clear();

    }

    [ContextMenu("Regenerate Artefacts")]
    void RegenerateArtefacts()
    {
        foreach(GameObject A in ArtefactsInLevel)
        {
            if(A == null) continue;
            tiles.GetNodeFromWorldPosition(A.transform.position).occupied = false;
            Destroy(A);
        }
        ArtefactsInLevel.Clear();
        ArtefactPaths.Clear();

        List<Node> reachable = Pathfinding.GetReachableNodesFromPosition(tiles.GetNodeFromWorldPosition(transform.position), false);

        foreach(Node A in reachable)
        {
            A.occupied = false;
        }

        if (ArtefactsBlockPathfinding)
        {
            SpawnArtefactsWithOccupiedCheck();
        }
        else
        {
            SpawnArtefacts();
        }
    }

    [NonSerialized]public float minTerrainHeight = 0f;
    [NonSerialized] public float maxTerrainHeight = 0f;
    [NonSerialized] public float minHeight = 0f;
    public Gradient Colors;
    Tiles tiles;
    NavMeshSurface navMeshSurface;
    AStarPathfinding Pathfinding;
    [NonSerialized]public GameObject WaterPlane;
    public List<List<Node>> ArtefactPaths = new List<List<Node>>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        tiles = transform.GetComponent<Tiles>();
        navMeshSurface = GetComponent<NavMeshSurface>();
        Pathfinding = GetComponent<AStarPathfinding>();

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        meshcollider = GetComponent<MeshCollider>();

        WaterPlane = GameObject.Find("Plane");

        remakeMesh();
       // navMeshSurface.BuildNavMesh();
    }

    // Update is called once per frame
    void Update()
    {
        // remakeMesh();
        //navMeshSurface.BuildNavMesh();

    }

    void CreateShape()
    {
        minTerrainHeight = 999999;
        maxTerrainHeight = -999999;
        GameObject.Find("Plane").transform.position = new Vector3(0, WaterHeight, 0);

        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float y = 0;

                float WorleyFbm = FBM(x*WorleyScale, z*WorleyScale, WorleyOctaves, WorleyPersistence,WorleyLacunarity, "Worley" );
                if (WorleyInvert) WorleyFbm = 1 - WorleyFbm;
                WorleyFbm = Mathf.Lerp(1,WorleyFbm,Worleystrength);
               

                float Perlinfbm = FBM(x * PerlinScale, z * PerlinScale, PerlinOctaves, PerlinPersistence, PerlinLacunarity, "Perlin");
                Perlinfbm = Mathf.Clamp(Perlinfbm, 0f, 9999);
                if (PerlinInvert) Perlinfbm = 1 - Perlinfbm;
                Perlinfbm = Mathf.Lerp(1, Perlinfbm, PerlinStrength);


                float pools = Mathf.Clamp01(1 - (Mathf.PerlinNoise(x * PoolScale, z * PoolScale)));
                pools = MathF.Pow(pools, Mathf.Max(0.0001f, PoolSharpness));
                pools = 1 - pools * Mathf.Max(0.0001f, PoolDepthMultiplier);

                y = pools * Perlinfbm * WorleyFbm * heightMultiplier;
                //y =  Perlinfbm * WorleyFbm * heightMultiplier;


                /*                // ---------------------------
                                // 2. Island Falloff Mask
                                // ---------------------------
                                float centerX = xSize / 2f;
                                float centerZ = zSize / 2f;

                                float dx = x - centerX;
                                float dz = z - centerZ;

                                float distance = Mathf.Sqrt(dx * dx + dz * dz);
                                float maxDist = Mathf.Sqrt(centerX * centerX + centerZ * centerZ);

                                float mask = distance / maxDist;       // 0 center, 1 edge
                                float falloff = Mathf.Pow(mask, .25f);   // tweak exponent

                                // ---------------------------
                                // 3. Apply falloff
                                // ---------------------------
                                float coastNoise = Mathf.PerlinNoise(x * 0.15f, z * 0.15f);
                                y *= Mathf.Lerp(1f - falloff, 1f, coastNoise * 0.2f);
                                // Optional sea floor clamp
                                if (y < 0) y = 0;*/

                float cx = xSize * 0.5f;
                float cz = zSize * 0.5f;

                float nx = (x - cx) / cx;
                float nz = (z - cz) / cz;

                float Circledist = Mathf.Sqrt(nx * nx + nz * nz);
                float Squaredist = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(nz));

                float dist = Mathf.Lerp(Squaredist, Circledist, IslandShape);

                float falloff =  Mathf.Clamp01(Mathf.Pow(dist, - IslandFalloffSmoothness));
                falloff *= IslandFactor;

                if (float.IsNaN(y))
                {
                    y = 0;
                }

                else
                    y = Mathf.Lerp(y, 0f, falloff);

                y = Mathf.Clamp(y, -10, 50);

                if (y < minTerrainHeight)
                {
                    minTerrainHeight = y;
                }
                if (y > maxTerrainHeight)
                {
                    maxTerrainHeight = y;
                }
                vertices[i] = new Vector3(x, y, z);

                minHeight = Mathf.Max(minTerrainHeight, WaterHeight);

                


                i++;
            }

        }
        triangles = new int[xSize * zSize * 6];
        int vert = 0;
        int tris = 0;

        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;

                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }

    }

    void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        ColourTerrain();
        mesh.colors = colours;

        if (meshcollider != null)
        {
            meshcollider.sharedMesh = null;

            meshcollider.sharedMesh = mesh;
        }
    }

    void remakeMesh()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        Destroy(mesh);
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        maxTerrainHeight = heightMultiplier;
        colours = new Color[(xSize + 1) * (zSize + 1)];


        CreateShape();
        UpdateMesh();
        // navMeshSurface.BuildNavMesh();

        sw.Stop();
        Debug.Log("Terrain generation time: " + sw.ElapsedMilliseconds);
    }



    float WorleyNoise(Vector2 pos)
    {
        int cellX = Mathf.FloorToInt(pos.x);
        int cellY = Mathf.FloorToInt(pos.y);

        float minDist = float.MaxValue;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2 thisCell = new Vector2(cellX + x, cellY + y);

                float rx = Mathf.PerlinNoise(pos.x, pos.y);
                float ry = Mathf.PerlinNoise(pos.x, pos.y);

                Vector2 featurePoint = thisCell + new Vector2(rx, ry);

                float dist = Vector2.Distance(pos, featurePoint);

                if (dist < minDist)
                    minDist = dist;
            }
        }
        return minDist;
    }



    float FBM(float x, float y, int Octaves, float Persistence, float Lacunarity, string NoiseType)
    {
        float FBMnoise = 0;
        float a = 1;

        for (int i = 0; i < Octaves; i++)
        {
            switch (NoiseType)
            {
                case "Perlin":
                    FBMnoise += Mathf.PerlinNoise(x, y) * a;
                    break;

                case "Worley":
                    FBMnoise += WorleyNoise(new Vector2(x,y)) * a;
                    break;
            }
            a *= Persistence;
            x *= Lacunarity;
            y *= Lacunarity;
        }
        return FBMnoise;
    }

    void ColourTerrain()
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            float normalizedHeight = Mathf.InverseLerp(minHeight, maxTerrainHeight, vertices[i].y);


            Color baseColor = Colors.Evaluate(normalizedHeight);
            colours[i] = baseColor;

/*            float y = vertices[i].y;

            float normalizedHeight = Mathf.InverseLerp(minHeight, maxTerrainHeight, y);

            Vector3 normal = mesh.normals[i];
            float slope = 1f - normal.y; // 0 flat, 1 steep

            // Height-based color
            Color baseColor = Colors.Evaluate(normalizedHeight);

            // Add slope-based rock
            //Color rock = new Color(0.5f, 0.5f, 0.5f);
            Color rock = Color.black;
            Color slopeColor = Color.Lerp(baseColor, rock, slope * slope);

            // Add perlin patch variation
            float patch = Mathf.PerlinNoise(vertices[i].x * 0.2f, vertices[i].z * 0.2f);
            Color finalColor = Color.Lerp(slopeColor, baseColor * 0.8f, patch * 0.1f);

            colours[i] = finalColor;*/
        }
    }

    public void SpawnArtefacts()
    {
        ArtefactsInLevel.Clear();

        var sw = System.Diagnostics.Stopwatch.StartNew();

        int num = 1;

        List<Node> ReachableNodes = new List<Node>();

        Node startNode = tiles.GetNodeFromWorldPosition(GameObject.Find("Player").transform.position);

        ReachableNodes = Pathfinding.GetReachableNodesFromPosition(startNode, true);

        Debug.Log("Reachable Nodes: " + ReachableNodes.Count);

        int ActualNumOfArtefacts = ReachableNodes.Count;
        if (ActualNumOfArtefacts > DesiredNumOfArtefacts) { ActualNumOfArtefacts = DesiredNumOfArtefacts; }
        else
        {
            Debug.LogWarning("Desired Artefact amount is larger than the available space, filling the max possible");
        }

        int ActualNumOfEachArtefact = Mathf.FloorToInt(ActualNumOfArtefacts / Artefacts.Count);

        if (ActualNumOfArtefacts < Artefacts.Count) { ActualNumOfEachArtefact = 1;}
        else
        {
            if (ActualNumOfEachArtefact >= DesiredNumOfEachArtefact) { ActualNumOfEachArtefact = DesiredNumOfEachArtefact; }
            else
            {
                Debug.LogWarning("Desired Number of each Artefact is too large for the desired total Artefact amount. New amount is " + ActualNumOfEachArtefact + " per Artefact");
            }
        }

        /*        foreach (Node node in tiles.NodesGrid)
                {
                    float rand = UnityEngine.Random.Range(0f, 1f);
                    int randArt = UnityEngine.Random.Range(0, Artefacts.Count);
                    if(node.nodeTyoe == NodeType.Untraversable) continue;
                    if(node.nodeTyoe == NodeType.Heavy) continue;

                    if(UnreachableNodes.Contains(node)) continue;

                    List<Node> path = Pathfinding.GetPath(GameObject.Find("Player").transform.position, node.worldPos);

                    if (path.Count > 1)
                    {
                        //GameObject newArtefact = GameObject.Instantiate(Artefacts[randArt], node.worldPos, quaternion.identity);
                        ReachableNodes.Add(node);

                    }
                    else
                    {
                        UnreachableNodes.Add(node);
                    }

                    foreach (Node unreachableNode in UnreachableNodes)
                    {
                        List<Node> unreachNodeNeighbours = Pathfinding.GetNodeNeighbours(unreachableNode);
                        foreach(Node neighbour in unreachNodeNeighbours)
                        {
                            if (visitedNode[neighbour])continue;
                            if (neighbour.nodeTyoe == NodeType.Untraversable) continue;
                            visitedNode.Add(neighbour, true);

                        }
                    }

                    if (rand < .02f)
                    {
                    }
                }*/

        for (int a = 0; a < Artefacts.Count; a++)
        {
            for (int i = 0; i < ActualNumOfEachArtefact; i++)
            {
                if (ArtefactsInLevel.Count >= ActualNumOfArtefacts)
                {
                    break;
                }
                if (ReachableNodes.Count == 0)
                {
                    break;
                }

                int RandNodeIndex = UnityEngine.Random.Range(0, ReachableNodes.Count);

                GameObject newArtefact = GameObject.Instantiate(Artefacts[a], ReachableNodes[RandNodeIndex].worldPos, quaternion.identity);
                newArtefact.name += " - Allocated - " + num; num++;
                ReachableNodes[RandNodeIndex].occupied = true;
                ArtefactsInLevel.Add(newArtefact);
                ReachableNodes.RemoveAt(RandNodeIndex);
            }
        }

        int giveUp = 0;

        if (ArtefactsInLevel.Count < DesiredNumOfArtefacts && ReachableNodes.Count > 0)
        {
            Debug.Log("Adding random extra Artefacts to reach total desired amount");
            while (ArtefactsInLevel.Count < DesiredNumOfArtefacts && ReachableNodes.Count > 0 && giveUp < 100)
            {
                int RandNodeIndex = UnityEngine.Random.Range(0, ReachableNodes.Count);
                int RandArtefactIndex = UnityEngine.Random.Range(0, Artefacts.Count);
 
                giveUp = 0;
                GameObject newArtefact = GameObject.Instantiate(Artefacts[RandArtefactIndex], ReachableNodes[RandNodeIndex].worldPos, quaternion.identity);
                newArtefact.name += " - RandomAdditional - " + num; num++;
                ReachableNodes[RandNodeIndex].occupied = true;
                ArtefactsInLevel.Add(newArtefact);
                ReachableNodes.RemoveAt(RandNodeIndex);
            }
        }

        if (DesiredNumOfArtefacts == ArtefactsInLevel.Count)
        {
            Debug.Log("Desired total Artefact amount reached");
        }

        if (DebugArtefactPaths)
            CreateArtefactPathList();

        sw.Stop();
        Debug.Log("Artefact Placement time: " + sw.ElapsedMilliseconds);
    }

    public void SpawnArtefactsWithOccupiedCheck()
    {
        ArtefactsInLevel.Clear();

        var sw = System.Diagnostics.Stopwatch.StartNew();

        int num = 1;

        List<Node> ReachableNodes = new List<Node>();

        Node startNode = tiles.GetNodeFromWorldPosition(GameObject.Find("Player").transform.position);

        ReachableNodes = Pathfinding.GetReachableNodesFromPosition(startNode, true);

        Debug.Log("Reachable Nodes: " + ReachableNodes.Count);

        int ActualNumOfArtefacts = ReachableNodes.Count;
        if (ActualNumOfArtefacts > DesiredNumOfArtefacts) { ActualNumOfArtefacts = DesiredNumOfArtefacts; }
        else
        {
            Debug.LogWarning("Desired Artefact amount is larger than the available space, filling the max possible");
        }

        int ActualNumOfEachArtefact = Mathf.FloorToInt(ActualNumOfArtefacts / Artefacts.Count);

        if (ActualNumOfArtefacts < Artefacts.Count) { ActualNumOfEachArtefact = 1; }
        else
        {
            if (ActualNumOfEachArtefact >= DesiredNumOfEachArtefact) { ActualNumOfEachArtefact = DesiredNumOfEachArtefact; }
            else
            {
                Debug.LogWarning("Desired Number of each Artefact is too large for the desired total Artefact amount. New amount is " + ActualNumOfEachArtefact + " per Artefact");
            }
        }

        List<Node> PriorityNodes = new List<Node>();

        for (int a = 0; a < Artefacts.Count; a++)
        {
            for (int i = 0; i < ActualNumOfEachArtefact; i++)
            {
                if (ArtefactsInLevel.Count >= ActualNumOfArtefacts)
                {
                    break;
                }
                if (ReachableNodes.Count == 0)
                {
                    break;
                }

                ReachableNodes = Pathfinding.GetReachableNodesFromPosition(startNode, true);

                int RandNodeIndex = UnityEngine.Random.Range(0, ReachableNodes.Count);
                Node RandomNode = ReachableNodes[RandNodeIndex];
                if (PriorityNodes.Contains(RandomNode)) continue;

                bool BlocksAnotherArtefact = CheckSpawnBlocksArtefacts(startNode, RandomNode);

                if (BlocksAnotherArtefact)
                {
                    PriorityNodes.Add(RandomNode);
                    continue;
                }

                GameObject newArtefact = GameObject.Instantiate(Artefacts[a], ReachableNodes[RandNodeIndex].worldPos, quaternion.identity);
                newArtefact.name += " - Allocated - " + num; num++;
                ReachableNodes[RandNodeIndex].occupied = true;
                ArtefactsInLevel.Add(newArtefact);
                ReachableNodes.RemoveAt(RandNodeIndex);
            }
        }

        int giveUp = 0;

        if (ArtefactsInLevel.Count < DesiredNumOfArtefacts && ReachableNodes.Count > 0)
        {
            Debug.Log("Adding random extra Artefacts to reach total desired amount");
            while (ArtefactsInLevel.Count < DesiredNumOfArtefacts && ReachableNodes.Count > 0 && giveUp < 100)
            {
                ReachableNodes = Pathfinding.GetReachableNodesFromPosition(startNode, true);

                int RandNodeIndex = UnityEngine.Random.Range(0, ReachableNodes.Count);
                int RandArtefactIndex = UnityEngine.Random.Range(0, Artefacts.Count);
                Node RandomNode = ReachableNodes[RandNodeIndex];
                if (PriorityNodes.Contains(RandomNode))
                {
                    giveUp++;
                    continue;
                }

                bool BlocksAnotherArtefact = CheckSpawnBlocksArtefacts(startNode, RandomNode);

                if (BlocksAnotherArtefact)
                {
                    PriorityNodes.Add(RandomNode);
                    giveUp++;
                    continue;

                }
                //giveUp = 0;
                GameObject newArtefact = GameObject.Instantiate(Artefacts[RandArtefactIndex], ReachableNodes[RandNodeIndex].worldPos, quaternion.identity);
                newArtefact.name += " - RandomAdditional - " + num; num++;
                ReachableNodes[RandNodeIndex].occupied = true;
                ArtefactsInLevel.Add(newArtefact);
                ReachableNodes.RemoveAt(RandNodeIndex);
            }
        }

        if (DesiredNumOfArtefacts == ArtefactsInLevel.Count)
        {
            Debug.Log("Desired total Artefact amount reached");
        }

        if(DebugArtefactPaths)
        CreateArtefactPathList();

        sw.Stop();
        Debug.Log("Artefact Placement time: " + sw.ElapsedMilliseconds);
    }
    bool CheckSpawnBlocksArtefacts(Node StartNode, Node newArtefactPos)
    {
        foreach (GameObject g in ArtefactsInLevel)
        {
            bool reachable = Pathfinding.ArtefactStillReachable(StartNode, tiles.GetNodeFromWorldPosition(g.transform.position), true, newArtefactPos);
            if (!reachable)
            {
                return true;
            }
        }
        return false;
    }

    public void CreateArtefactPathList()
    {
            foreach (GameObject g in ArtefactsInLevel)
            {
                List<Node> path = Pathfinding.GetPath(GameObject.Find("Player").transform.position, g.transform.position);
                ArtefactPaths.Add(path);
            }
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            remakeMesh();
        }
    }

    private void OnDrawGizmos()
    {
        /*       if (vertices == null || mesh == null) return;
               var norms = mesh.normals;
               for (int i = 0; i < vertices.Length; i++)
               {
                   Gizmos.color = Color.yellow;
                   Gizmos.DrawLine(vertices[i], vertices[i] + norms[i]);
               }*/

        if (!Application.isPlaying) return;
        if(tiles== null)return;
        if(tiles.NodesGrid== null)return;
        if (ArtefactsInLevel == null) return;
        if (ArtefactPaths == null) return;
        if (!DebugArtefactPaths) return;

        foreach (List<Node> path in ArtefactPaths)
        {
            foreach (Node node in path)
            {
                Gizmos.color = Color.cyan;

                foreach (Node n in path)
                {
                    Gizmos.DrawCube(n.worldPos, Vector3.one);
                }
            }
        }
        
    }

}

[System.Serializable]
public class FBMNoise
{
    public float Persistence;
    public int Octaves;
    public float Lacunarity;
}

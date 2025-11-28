using TreeEditor;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;
using System;
using UnityEditor.Experimental.GraphView;
using System.Data;

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
    public float IslandFalloffStrength;

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
    [Range(0.0f, 1f)] public float IslandShape;
    public float WaterHeight;

    [ContextMenu("Rebuild Node Grid")]
    void RebuildNodeGrid()
    {
        tiles.GenerateGridFromTerrain(null, transform);
        
    }

    [ContextMenu("Regenerate Artefacts")]
    void RegenerateArtefacts()
    {
        foreach(GameObject A in ArtefactsInLevel)
        {
            Destroy(A);
        }
        ArtefactsInLevel.Clear();

        SpawnArtefacts();
    }
    float minTerrainHeight = 0f;
    float maxTerrainHeight = 0f;
    public Gradient Colors;
    Tiles tiles;
    NavMeshSurface navMeshSurface;
    AStarPathfinding Pathfinding;
    [NonSerialized]public GameObject WaterPlane;

    public List<GameObject> Artefacts = new List<GameObject>();
    public List<GameObject> ArtefactsInLevel = new List<GameObject>();

    public List<FBMNoise> Noises;

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

        remakeMesh(); UpdateMesh(); 
        //tiles.GenerateGridFromTerrain(null,transform); SpawnArtefacts();
    }

    // Update is called once per frame
    void Update()
    {
        // Debug.Log(tiles == null);

        // remakeMesh();
        //navMeshSurface.BuildNavMesh();

        //Debug.Log(Pathfinding == null);

    }

    void CreateShape()
    {
        minTerrainHeight = 999999;
        maxTerrainHeight = -999999;
        WaterPlane.transform.position = new Vector3(0, WaterHeight, 0);

        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float y = 0;

                //y = DomainWarp(x*scale,z*scale) * heightMultiplier;

                float WorleyFbm = FBM(x*WorleyScale, z*WorleyScale, WorleyOctaves, WorleyPersistence,WorleyLacunarity, "Worley" );
                if (WorleyInvert) WorleyFbm = 1 - WorleyFbm;
                WorleyFbm = Mathf.Lerp(1,WorleyFbm,Worleystrength);
               

                float Perlinfbm = FBM(x * PerlinScale, z * PerlinScale, PerlinOctaves, PerlinPersistence, PerlinLacunarity, "Perlin");
                Perlinfbm = Mathf.Clamp(Perlinfbm, 0f, 1);
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

                // ----- ISLAND FALLOFF -----

                float cx = xSize * 0.5f;
                float cz = zSize * 0.5f;

                float nx = (x - cx) / cx;
                float nz = (z - cz) / cz;

                float Circledist = Mathf.Sqrt(nx * nx + nz * nz);
                float Squaredist = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(nz));

                float dist = Mathf.Lerp(Squaredist, Circledist, IslandShape);

                float falloff = Mathf.Clamp01(Mathf.Pow(dist, IslandFalloffStrength));
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
                else if (y > maxTerrainHeight)
                {
                    maxTerrainHeight = y;
                }
                vertices[i] = new Vector3(x, y, z);

                float minHeight = Mathf.Max(minTerrainHeight, WaterHeight);

                float normalizedHeight = Mathf.InverseLerp(minHeight, maxTerrainHeight, y);


                Color baseColor = Colors.Evaluate(normalizedHeight);
                colours[i] = baseColor;


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
        // ColourTerrain();
        mesh.colors = colours;

        //if (meshcollider != null)
        {
            meshcollider.sharedMesh = null;

            meshcollider.sharedMesh = mesh;
        }
        // tiles.GenerateGridFromTerrain(mesh, transform);
    }

    void remakeMesh()
    {
        Destroy(mesh);
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        maxTerrainHeight = heightMultiplier;
        colours = new Color[(xSize + 1) * (zSize + 1)];


        CreateShape();
        UpdateMesh();
       // navMeshSurface.BuildNavMesh();
    }



    float WorleyNoise(Vector2 pos)
    {
        int cellX = Mathf.FloorToInt(pos.x);
        int cellY = Mathf.FloorToInt(pos.y);

        float minDist = float.MaxValue;

        // Check this cell + 8 neighbours
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2 thisCell = new Vector2(cellX + x, cellY + y);

                //float rx = UnityEngine.Random.Range(0f, 1f);
                //float rx = Mathf.PerlinNoise(pos.x,pos.y) + math.sin(Time.time * Mathf.PerlinNoise(pos.x, pos.y));
                //float rx = Mathf.PerlinNoise(pos.x,pos.y);
                float rx = Mathf.PerlinNoise(pos.x, pos.y);
                float ry = Mathf.PerlinNoise(pos.x, pos.y);

                // Random feature point inside cell
                Vector2 featurePoint = thisCell + new Vector2(
                    rx,
                    ry
                );

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

    float DomainWarp(float x, float y)
    {

        /*

                float fmb1 = FBM(x + 0, y + 0, PerlinOctaves, PerlinPersistence, .5f);
                float fmb2 = FBM(x + 5.2f, y + 1.3f, 1, 1, .5f);
                float fmb3 = FBM(x + 4 * fmb1 + 1.7f, y + 4 * fmb1 + 9.2f, 1, 1, .5f);
                float fmb4 = FBM(x + 4 * fmb2 + 8.3f, y + 4 * fmb2 + 2.8f, 1, 1, .5f);

                return FBM(fmb3, fmb4, 1, 1, .5f);*/
        return 1;
    }

    void ColourTerrain()
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            float y = vertices[i].y;

            float normalizedHeight = Mathf.InverseLerp(minTerrainHeight, maxTerrainHeight, y);

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

            colours[i] = finalColor;
        }
    }

    public void SpawnArtefacts()
    {
        List<Node> ReachableNodes = new List<Node>();

        foreach (Node node in tiles.NodesGrid)
        {
            float rand = UnityEngine.Random.Range(0f, 1f);
            int randArt = UnityEngine.Random.Range(0, Artefacts.Count);
            if(node.nodeTyoe == NodeType.Untraversable) continue;
            if(node.nodeTyoe == NodeType.Heavy) continue;

            List<Node> path = Pathfinding.GetPath(GameObject.Find("Player").transform.position, node.worldPos);

            if (path.Count > 1)
            {
                //GameObject newArtefact = GameObject.Instantiate(Artefacts[randArt], node.worldPos, quaternion.identity);
                ReachableNodes.Add(node);

            }

            if (rand < .02f)
            {
            }
        }

        for (int a = 0; a < Artefacts.Count; a++)
        {
            if (ReachableNodes.Count > 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    int RandNodeIndex = UnityEngine.Random.Range(0, ReachableNodes.Count);

                    GameObject newArtefact = GameObject.Instantiate(Artefacts[a], ReachableNodes[RandNodeIndex].worldPos, quaternion.identity);
                    ArtefactsInLevel.Add(newArtefact);
                    ReachableNodes.RemoveAt(RandNodeIndex);
                }
            }
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
        if (vertices == null || mesh == null) return;
        var norms = mesh.normals;
        for (int i = 0; i < vertices.Length; i++)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(vertices[i], vertices[i] + norms[i]);
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

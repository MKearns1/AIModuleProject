using Unity.Mathematics;
using UnityEngine;


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainGenerator : MonoBehaviour
{
    Mesh mesh;
    MeshCollider meshcollider;

    Vector3[] vertices;
    int[] triangles;

    public int xSize = 26;
    public int zSize = 20;
    public float scale = 0.3f;
    public float scale2 = 0.3f;
    public float f;
    public float exponent = 1;
    public float heightMultiplier = 2f;
    Color[] colours;
    float minTerrainHeight = 0f;
    float maxTerrainHeight = 0f;
    public Gradient Colors;
    Tiles tiles;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        meshcollider = GetComponent<MeshCollider>();
        tiles = transform.GetComponent<Tiles>();

        remakeMesh(); UpdateMesh();
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(tiles == null);

       remakeMesh();
    }

    void CreateShape()
    {
        
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float PerlinBase = Mathf.PerlinNoise(x * scale, z * scale);
                float p = Mathf.PerlinNoise(x * scale, z * scale) * 2f - 1f;
                float ridge = 1f - Mathf.Abs(p);
                ridge = ridge * ridge;

                float Worley = WorleyNoise(new Vector2(x*scale, z*scale));

                float combined = Mathf.Lerp(PerlinBase, ridge, ridge);


                float PerlinAdd = Mathf.PerlinNoise(x * scale2, z * scale2) * f;
                combined = (PerlinBase + PerlinAdd)/2;


                combined = math.round(combined * exponent) / exponent;

                combined = Mathf.Pow(combined, exponent);


                float y = combined * heightMultiplier;
                vertices[i] = new Vector3(x, y, z);

                float normalizedHeight = Mathf.InverseLerp(minTerrainHeight, maxTerrainHeight, y);

                Color color = Color.blue;
                Color colorb = Color.red;

                //if(normalizedHeight < .25)
                //{
                //    color = Color.yellow;
                //    colorb = Color.green;
                //}
                //else if (normalizedHeight < .5)
                //    {
                //        color = Color.green;
                //        colorb = Color.grey;
                //    }
                //else if(normalizedHeight <= 1)
                //{
                //    color = Color.grey;
                //    colorb = Color.white;
                //}

                Color HeightColor = Colors.Evaluate(normalizedHeight);
                    colours[i] = Color.Lerp(color, colorb, normalizedHeight);
                colours[i] = HeightColor;
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
        mesh.colors = colours;
        mesh.RecalculateNormals();

        meshcollider.sharedMesh = null;
        meshcollider.sharedMesh = mesh;
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

                float rx = UnityEngine.Random.Range(0f, 1f);
                float ry = UnityEngine.Random.Range(0f, 1f);

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


}

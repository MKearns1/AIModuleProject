using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using UnityEngine.UIElements;
using System.Collections.Generic;
using static TreeEditor.TreeEditorHelper;
using UnityEngine.SceneManagement;

public class Tiles : MonoBehaviour
{
    public int GridSize = 50;
    public float Scale = 1f;
    
    public Node[,] NodesGrid;
    Vector3 BottomLeft = Vector3.zero;

    public TerrainTypes TerrainData;

    public LayerMask ProceduralTerrainInclusionLayers;

    float scentDecayRate = .05f;
    
    [Header("Debug Settings")]

    public List<NodeType> DebugNodes = new List<NodeType>();

    public bool ShowPlayerScent;
    public bool ShowPlayerNode;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {        

        Vector3 GridCentre = Vector3.one *GridSize* Scale/2;GridCentre.y = 0;
        Vector3 offset = new Vector3(0, 15, -25);
        //Camera.main.transform.position = GridCentre+offset;

        //GameObject.FindGameObjectWithTag("NavMesh").gameObject.GetComponent<NavMeshSurface>().BuildNavMesh();
    }
    private void Awake()
    {
        if(SceneManager.GetActiveScene().name == "AIPlayground")
        {
            GenerateGrid();
        }

    }
    // Update is called once per frame
    void Update()
    {
        if(NodesGrid == null)
            return;

        foreach (var node in NodesGrid)
        {
            if(node.PlayerScentStrength > 0)
            {
                node.PlayerScentStrength -= Time.deltaTime * scentDecayRate;
            }
        }
    }

    GameObject CreateTile(Vector3 Position)
    {
        GameObject Tile = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Tile.transform.position = Position;

        Vector3 TileSize = Vector3.one * Scale/10;
        Tile.transform.localScale = TileSize;


        DestroyImmediate(Tile.GetComponent<MeshCollider>());
        var collider = Tile.AddComponent<MeshCollider>();
        collider.sharedMesh = Tile.GetComponent<MeshFilter>().sharedMesh;

        Tile.transform.SetParent(transform,true);
        return Tile;
    }

    void GenerateGrid()
    {
        BottomLeft = transform.position - (Vector3.right * GridSize * Scale / 2) - (Vector3.forward * GridSize * Scale / 2);
        NodesGrid = new Node[GridSize, GridSize];

        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                Vector3 direction = new Vector3(x * Scale, 0, y * Scale);
                Vector3 TilePosition = direction + BottomLeft;

                Vector2Int gridpos = new Vector2Int(x, y);

                NodeType nodeType = DetectNodeType(TilePosition);

                Node node = new Node(gridpos, TilePosition, nodeType);

                NodesGrid[x, y] = node;

            }
        }
    }

    public void GenerateGridFromTerrain(TerrainGenerator terrainGen)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        BottomLeft = terrainGen.transform.position;
        float numNodes = terrainGen.xSize / Scale;

        GridSize = Mathf.FloorToInt(numNodes);

        NodesGrid = new Node[GridSize, GridSize];


        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                Vector3 worldPos = BottomLeft + new Vector3(x * Scale, 100f, y * Scale);

                NodeType type = NodeType.Default;

                if (Physics.Raycast(worldPos, Vector3.down, out RaycastHit hit, 200f, ProceduralTerrainInclusionLayers))
                {
                    float slopeDotProduct = Vector3.Dot(hit.normal, Vector3.up);

                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Unwalkable") || slopeDotProduct < .5f)
                    {
                        type = NodeType.Untraversable;
                    }
                    else
                    {
                        worldPos = hit.point;

                        float normalizedHeight = Mathf.InverseLerp(terrainGen.minHeight, terrainGen.maxTerrainHeight, worldPos.y);

                        if ((normalizedHeight < .1))
                        {
                            type = NodeType.Light;
                        }
                        else if(normalizedHeight > .8f || slopeDotProduct < .7f)
                        {
                            type = NodeType.Heavy;
                        }
                        else
                        {
                            type = NodeType.Default;
                        }
                    }

                }
                else
                {
                    type = NodeType.Untraversable;

                }


                NodesGrid[x, y] = new Node(new Vector2Int(x, y), worldPos, type);
            }
        }

        sw.Stop();
        Debug.Log("Node Placement time: " + sw.ElapsedMilliseconds);
    }


    public Node GetNodeFromWorldPosition(Vector3 WorldPosition)
    {
        float worldOffsetX = WorldPosition.x - BottomLeft.x;
        float worldOffsetY = WorldPosition.z - BottomLeft.z;

        int NodePosX = Mathf.FloorToInt(worldOffsetX / Scale);
        int NodePosY = Mathf.FloorToInt(worldOffsetY / Scale);

        NodePosX = Mathf.Clamp(NodePosX, 0, GridSize - 1);
        NodePosY = Mathf.Clamp(NodePosY, 0, GridSize - 1);

        return NodesGrid[NodePosX, NodePosY];
    }

    public NodeType DetectNodeType(Vector3 WorldPos)
    {
        NodeType type = NodeType.Default;
        int highestPriority = -1;

        foreach (var rule in TerrainData.rules)
        {
            if (Physics.CheckBox(WorldPos, Vector3.one * Scale * 0.4f, Quaternion.identity, rule.LayerMask))
            {
                if (rule.priority > highestPriority)
                {
                    highestPriority = rule.priority;
                    type = rule.Type;
                }

            }
        }

        return type;
    }

    private void OnDrawGizmos()
    {
        if (true) { 
        Gizmos.DrawCube(transform.position - (Vector3.right * GridSize * Scale / 2) - (Vector3.forward * GridSize * Scale / 2),Vector3.one);

            if (NodesGrid != null)
            {

                Node playersNode = GetNodeFromWorldPosition(GameObject.Find("Player").transform.position);
                for (int i = 0; i < NodesGrid.GetLength(0); i++)
                {
                    for (int j = 0; j < NodesGrid.GetLength(1); j++)
                    {
                        NodeType nodeType = NodesGrid[i,j].nodeTyoe;
                        Vector3 pos = NodesGrid[i, j].worldPos;
                        Vector3 scale = Vector3.one * Scale;

                        if (NodesGrid[i, j].PlayerScentStrength > 0 && ShowPlayerScent)
                        {
                            Color color = Color.white;
                            color *= Color.magenta * NodesGrid[i, j].PlayerScentStrength;

                            Gizmos.color = color; Gizmos.DrawCube(pos, scale * .8f);
                            continue;
                        }
                        if (NodesGrid[i, j] == playersNode && ShowPlayerNode)
                        {
                            Gizmos.color = Color.red;
                        }

                        if (!DebugNodes.Contains(nodeType)) { continue; }

                        Gizmos.color = Color.white;

                        switch (nodeType)
                        {
                            case NodeType.Untraversable:
                                Gizmos.color = Color.black; break;
                            case NodeType.Heavy:
                                Gizmos.color = Color.red; break;
                            case NodeType.Light:
                                Gizmos.color = Color.yellow; break;
                            case NodeType.Default:
                                Gizmos.color = Color.white; break;
                        }

                        if (NodesGrid[i, j].occupied)
                        {
                            Gizmos.color = Color.yellow;
                        }
                        
                        Gizmos.DrawCube(pos, scale * .7f);
                    }
                }

                //foreach (Node n in transform.GetComponent<AStarPathfinding>().GetNodeNeighbours(playersNode))
                //{
                //   // Gizmos.color = Color.blue;

                //   // Gizmos.DrawCube(n.worldPos, Vector3.one * Scale * .9f);

                //}

                //List<Node> Path = transform.GetComponent<AStarPathfinding>().GetPath(Vector3.zero, playersNode.worldPos);
                //if (Path.Count > 0)
                //{
                //    Color pathColor = Color.green;
                //    float pp = 1/ (float)Path.Count;
                //    foreach (Node n in Path)
                //    {
                //        pathColor += new Color(0, -pp, pp);
                //        Gizmos.color = pathColor;

                //        Gizmos.DrawCube(n.worldPos, Vector3.one * Scale * .9f);

                //    }
                //}
            }
        }

        if (true)
        {




        }
    }

}

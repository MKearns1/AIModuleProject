using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class Tiles : MonoBehaviour
{
    public int GridSize = 50;
    public float Scale = 1f;
    
    public Node[,] NodesGrid;
    Vector3 BottomLeft = Vector3.zero;

    public TerrainTypes TerrainData;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       

        //Camera.main.orthographicSize = GridSize*Scale;
        

        Vector3 GridCentre = Vector3.one *GridSize* Scale/2;GridCentre.y = 0;
        Vector3 offset = new Vector3(0, 15, -25);
        //Camera.main.transform.position = GridCentre+offset;

        //GameObject.FindGameObjectWithTag("NavMesh").gameObject.GetComponent<NavMeshSurface>().BuildNavMesh();
    }
    private void Awake()
    {
        GenerateGrid();

    }
    // Update is called once per frame
    void Update()
    {
        // Debug.Log(NodesGrid.GetLength(0).ToString() + " "+NodesGrid.GetLength(1).ToString());

        //transform.GetComponent<AStarPathfinding>().GetPath(Vector3.zero, GameObject.Find("Player").transform.position);
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
                //direction += BottomLeft;
                //BottomLeft += direction;
                Vector3 TilePosition = direction + BottomLeft;
              //  GameObject newTile = CreateTile(TilePosition);

                Vector2Int gridpos = new Vector2Int(x, y);

                bool NodeWalkable = true;


                NodeType nodeType = DetectNodeType(TilePosition);


                Node node = new Node(gridpos, TilePosition, nodeType);

                NodesGrid[x, y] = node;

               // newTile.GetComponent<Renderer>().material.color = Color.white;


                if (x % 2 == 0)
                {
                    if (y % 2 != 0)
                    {
                       // newTile.GetComponent<Renderer>().material.color = Color.gray;
                    }
                }
                else
                {
                    if (y % 2 == 0)
                    {
                        //newTile.GetComponent<Renderer>().material.color = Color.gray;
                    }
                }
                //newTile.GetComponent<Renderer>().material
            }
        }
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



    //    float PercentAcrossXaxis =
    //(WorldPosition.x - BottomLeft.x) / (GridSize * Scale);

    //    float PercentAcrossYaxis =
    //        (WorldPosition.z - BottomLeft.z) / (GridSize * Scale);


    //    PercentAcrossXaxis = Mathf.Clamp01(PercentAcrossXaxis);
    //    PercentAcrossYaxis = Mathf.Clamp01(PercentAcrossYaxis);

    //    int NodePosX = Mathf.RoundToInt((GridSize-1) *PercentAcrossXaxis);
    //    int NodePosY = Mathf.RoundToInt((GridSize - 1) * PercentAcrossYaxis);

    //    return NodesGrid[NodePosX+0, NodePosY+0];
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
                        Vector3 pos = NodesGrid[i, j].worldPos;
                        //pos.z = pos.y;

                        Vector3 scale = Vector3.one * Scale;
                        Gizmos.color = Color.white;

                        if (NodesGrid[i, j] == playersNode)
                        {
                            Gizmos.color = Color.red;
                        }

                        switch (DetectNodeType(NodesGrid[i, j].worldPos))
                        {
                            case NodeType.Untraversable:
                                Gizmos.color = Color.black; break;
                            case NodeType.Heavy:
                                Gizmos.color = Color.red; break;
                        }

                        if(NodesGrid[i, j].occupied)
                        {
                            Gizmos.color = Color.yellow;
                        }
                        //else if (!NodesGrid[i, j].walkable)
                        //{
                        //    Gizmos.color = Color.black;
                        //}
                        Gizmos.DrawCube(pos, scale * .9f);
                    }
                }

                foreach (Node n in transform.GetComponent<AStarPathfinding>().GetNodeNeighbours(playersNode))
                {
                   // Gizmos.color = Color.blue;

                   // Gizmos.DrawCube(n.worldPos, Vector3.one * Scale * .9f);

                }

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
    }
}

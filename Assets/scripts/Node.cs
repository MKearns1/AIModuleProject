using UnityEngine;

public class Node
{
    public Vector2Int GridPos;
    public Vector3 worldPos;

    public Node ParentNode;

    public int gCost;
    public int hCost;

    public int traversalPenalty;
    public NodeType nodeTyoe;

    public bool occupied;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Node(Vector2Int gridpos, Vector3 wp, NodeType Type)
    {
        GridPos = gridpos;
        worldPos = wp;
        this.nodeTyoe = Type;

        switch (Type)
        {
            case NodeType.Default:
                traversalPenalty = 0;
                break;

            case NodeType.Untraversable:
                traversalPenalty = 99999999;
                break;

            case NodeType.Light:
                traversalPenalty = 10;
                break;

            case NodeType.Heavy:
                traversalPenalty = 100;
                break;

        }
    }

    public int GetFcost()
    {
        return gCost + hCost + traversalPenalty;
    }
}

public enum NodeType
{
    Default,
    Light,
    Heavy,
    Untraversable
}

[System.Serializable]

public struct Terrain
{
    public NodeType Type;
    public LayerMask LayerMask;
    public int priority;
}
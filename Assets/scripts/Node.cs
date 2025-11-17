using UnityEngine;

public class Node
{
    public Vector2 GridPos;
    public bool walkable;
    public Vector3 worldPos;

    public Node ParentNode;

    public int gCost;
    public int hCost;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Node(Vector2 gridpos, bool w, Vector3 wp)
    {
        GridPos = gridpos;
        walkable = w;
        worldPos = wp;
    }

    public int GetFcost()
    {
        return gCost + hCost;
    }
}

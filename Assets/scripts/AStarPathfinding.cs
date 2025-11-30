using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class AStarPathfinding : MonoBehaviour
{
    Tiles TilesScript;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        while (TilesScript == null)
        {
            TilesScript = GameObject.FindFirstObjectByType<Tiles>();
        }
        // TilesScript= GameObject.Find("Terrain").GetComponent<Tiles>();
        Debug.Log(TilesScript == null);

    }
    private void Awake()
    {
        TilesScript = GetComponent<Tiles>();

    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(TilesScript == null);
    }

    public List<Node> GetNodeNeighbours(Node node)
    {
        List<Node> Neighbours = new List<Node>();
        int NodeX = (int)node.GridPos.x;
        int NodeY = (int)node.GridPos.y;

        for (int x = -1; x <= 1; x++)
        {

            for (int y = -1; y <= 1; y++)
            {

                if(x == 0  && y == 0)
                {
                    continue;       //That would be the node itself
                }

                int NeighbourPosX = NodeX + x;
                int NeighbourPosY = NodeY + y;

                if(NeighbourPosX >= TilesScript.GridSize|| NeighbourPosX < 0 || NeighbourPosY >= TilesScript.GridSize || NeighbourPosY < 0)
                {
                    continue;
                }


               Node curNeighbour = TilesScript.NodesGrid[NeighbourPosX, NeighbourPosY];                
                Neighbours.Add(curNeighbour);

            }


        }



        return Neighbours;
    }


    public int GetDistanceBetweenNodes(Node Node1, Node Node2)
    {



        int Distance = 0;

        int Xdist = (int)Mathf.Abs(Node1.GridPos.x - Node2.GridPos.x);
        int Ydist = (int)Mathf.Abs(Node1.GridPos.y - Node2.GridPos.y);

        Distance = (int)Mathf.Sqrt((Xdist*10)*(Xdist*10) + (Ydist*10) * (Ydist*10));
            
        float d = Vector3.Distance(Node1.worldPos, Node2.worldPos);
        //  return (int)d;

        //Distance = (Xdist * 10) + (Ydist * 10);

        return Distance;


        int dstX = (int)Mathf.Abs(Node1.GridPos.x - Node2.GridPos.x);
        int dstY = (int)Mathf.Abs(Node1.GridPos.y - Node2.GridPos.y);

        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    public List<Node> GetPath(Vector3 StartPosition, Vector3 EndPosition)
    {
        foreach (Node n in TilesScript.NodesGrid)
        {
            n.gCost = int.MaxValue;
            n.hCost = 0;
        }

        List<Node> path = new List<Node>();
        Dictionary<Node, Node> ThisNodesParent = new Dictionary<Node, Node>();

        List<Node> AvailableNodes = new List<Node>();
        HashSet<Node> ExploredNodes = new HashSet<Node>();

        Node StartingNode = TilesScript.GetNodeFromWorldPosition(StartPosition);
        Node EndNode = TilesScript.GetNodeFromWorldPosition(EndPosition);

        AvailableNodes.Add(StartingNode);

        while (AvailableNodes.Count > 0)
        {
            Node CurrentNode = AvailableNodes[0];
            int LowestFcost = int.MaxValue;

            foreach (Node AvailableNode in AvailableNodes)
            {
                if(AvailableNode.GetFcost() < LowestFcost)
                {
                    LowestFcost = AvailableNode.GetFcost();
                    CurrentNode = AvailableNode;
                }

            }

            AvailableNodes.Remove(CurrentNode);
            ExploredNodes.Add(CurrentNode);

            if(CurrentNode == EndNode)
            {
                //return path;
                break;
            }
            
            foreach (Node CurrentNodeNeighbour in GetNodeNeighbours(CurrentNode))
            {

                bool shouldSkip = CurrentNodeNeighbour.nodeTyoe == NodeType.Untraversable || ExploredNodes.Contains(CurrentNodeNeighbour);
/*                bool occupied = true;
                if(CurrentNodeNeighbour.Occupier == gameObject || CurrentNodeNeighbour.occupied == false) { occupied = false; }*/

                bool occupied = CurrentNodeNeighbour.occupied && CurrentNodeNeighbour.Occupier != this.gameObject;

/*
                if (CurrentNodeNeighbour.occupied)
                {
                    if (CurrentNodeNeighbour != TilesScript.GetNodeFromWorldPosition(transform.position))
                    {
                        occupied = false;
                    }

                }
                else
                {
                    occupied = false;
                }*/
                bool isGoal = EndNode == CurrentNodeNeighbour;

                if (!isGoal && (shouldSkip || occupied))
                {
                    continue;
                }

                int NewGcost = CurrentNode.gCost + GetDistanceBetweenNodes(CurrentNode, CurrentNodeNeighbour);

                if(NewGcost < CurrentNodeNeighbour.gCost || !AvailableNodes.Contains(CurrentNodeNeighbour))
                {
                    CurrentNodeNeighbour.gCost = NewGcost;
                    CurrentNodeNeighbour.hCost = GetDistanceBetweenNodes(CurrentNodeNeighbour, EndNode);

                    ThisNodesParent[CurrentNodeNeighbour] = CurrentNode;

                    if (!AvailableNodes.Contains(CurrentNodeNeighbour))
                    {
                        AvailableNodes.Add(CurrentNodeNeighbour);
                    }
                }
            }
        }

        if (!ThisNodesParent.ContainsKey(EndNode))
        {
            return path;
        }

        Node cur = EndNode;

        while (cur != StartingNode)
        {
            if (ThisNodesParent.ContainsKey(cur))
            {
                path.Add(cur);
                cur = ThisNodesParent[cur];
            }
            else
            {
                break;
            }
        }

        path.Add(StartingNode);
        path.Reverse();
        return path;

    }

    public List<Node> GetReachableNodesFromPosition(Node StartNode, bool excludeOccupied)
    {
        List<Node> Reachable = new List<Node>();
        
        List<Node> Queue = new List<Node>();
        HashSet<Node> visited = new HashSet<Node>();

        Queue.Add(StartNode);
        visited.Add(StartNode);

        while (Queue.Count > 0)
        {
            Node current = Queue[0];
            Queue.RemoveAt(0);

            Reachable.Add(current);

            foreach (Node neighbour in GetNodeNeighbours(current))
            {
                if(visited.Contains(neighbour))continue;
                if(neighbour.nodeTyoe == NodeType.Untraversable)continue;
                if(neighbour.occupied && excludeOccupied)continue;

                visited.Add(neighbour);
                Queue.Add(neighbour);
            }
        }

        return Reachable;
    }

    public bool ArtefactStillReachable(Node StartNode, Node TargetNode, bool excludeOccupied, Node newArtefactPos)
    {
        bool reachable = false;

        List<Node> Reachable = new List<Node>();

        List<Node> Queue = new List<Node>();
        HashSet<Node> visited = new HashSet<Node>();

        Queue.Add(StartNode);
        visited.Add(StartNode);

        while (Queue.Count > 0)
        {
            Node current = Queue[0];
            Queue.RemoveAt(0);

            Reachable.Add(current);

            if(current == TargetNode)
            {
                reachable = true;
                break;
            }

            foreach (Node neighbour in GetNodeNeighbours(current))
            {
                if(neighbour == TargetNode)
                {
                    reachable = true;
                    break;
                }
                if (visited.Contains(neighbour)) continue;
                if (neighbour.nodeTyoe == NodeType.Untraversable) continue;
                if (neighbour.occupied && excludeOccupied) continue;
                //if (neighbour == newArtefactPos) continue;

                visited.Add(neighbour);
                Queue.Add(neighbour);
            }
        }

        return reachable;
    }
}

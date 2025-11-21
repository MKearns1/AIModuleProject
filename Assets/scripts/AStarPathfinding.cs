using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class AStarPathfinding : MonoBehaviour
{
    Tiles TilesScript;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TilesScript= GameObject.Find("Tiles").GetComponent<Tiles>();
    }

    // Update is called once per frame
    void Update()
    {
        
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

                bool shouldSkip = CurrentNodeNeighbour.nodeTyoe == NodeType.Untraversable || CurrentNodeNeighbour.occupied || ExploredNodes.Contains(CurrentNodeNeighbour);
                //bool occupied = true;

                //if (CurrentNodeNeighbour.occupied)
                //{
                //    if (CurrentNodeNeighbour != TilesScript.GetNodeFromWorldPosition(transform.position))
                //    {
                //        occupied = false;
                //    }

                //}
                //else
                //{
                //    occupied = false ;
                //}
                bool isGoal = EndNode == CurrentNodeNeighbour;

                if (!isGoal && shouldSkip)
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
}

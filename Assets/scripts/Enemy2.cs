using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class Enemy2 : MonoBehaviour
{
    AStarPathfinding Pathfinder;
    public List<Node> CurrentPath = new List<Node>();
    TileMovement TileMover;
    Vector3 StartPos;
    Vector3 MoveDirection;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Pathfinder = GetComponent<AStarPathfinding>();
        TileMover = GetComponent<TileMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.F))
        {
            ChasePlayer();
        }

        FollowPath();
        
        Debug.Log(CurrentPath.Count);
    }

    public void FollowPath()
    {
        if (CurrentPath.Count > 0)
        {
            float DistanceToPathPoint = Vector3.Distance(transform.position, CurrentPath[0].worldPos);
            Vector3 MoveDirection = CurrentPath[0].worldPos - StartPos;
            MoveDirection.y = CurrentPath[0].worldPos.y;
            //MoveDirection += transform.position;
            if (DistanceToPathPoint > .1f)
            {
                if(!TileMover.moving)
                {
                    TileMover.TileMove(MoveDirection.normalized * 1f);
                    StartPos = TileMover.PrevPos;                   
                }
            }
        }
    }
    public void ChasePlayer()
    {
        
        CurrentPath = Pathfinder.GetPath(transform.position, GameObject.FindWithTag("Player").transform.position);
        StartPos = transform.position;
        Vector3 p = transform.position;
        p.x = CurrentPath[0].worldPos.x;
        p.z = CurrentPath[0].worldPos.z;
        transform.position = p;

    }

    public void TileMoveFinished()
    {
        CurrentPath.RemoveAt(0);
    }

    private void OnDrawGizmos()
    {
        if (CurrentPath.Count > 0)
        {
            Color pathColor = Color.green;
            float pp = 1 / (float)CurrentPath.Count;
            foreach (Node n in CurrentPath)
            {
                pathColor += new Color(0, -pp, pp);
                Gizmos.color = pathColor;

                Gizmos.DrawCube(n.worldPos, Vector3.one * 1f * .9f);

            }

            //MoveDirection += transform.position;

            Gizmos.color = Color.red;

            Gizmos.DrawCube(MoveDirection + transform.position, Vector3.one * 1 * .9f);
        }
    }
}

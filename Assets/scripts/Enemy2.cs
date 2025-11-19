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
    Player player;
    CapsuleCollider capsuleCollider;

    float repathTimer = 0;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Pathfinder = GetComponent<AStarPathfinding>();
        TileMover = GetComponent<TileMovement>();
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
        capsuleCollider = GetComponent<CapsuleCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.F))
        {
            ChasePlayer();
        }

        repathTimer += Time.deltaTime;

        if (repathTimer > 0.5f && !TileMover.moving)
        {
            repathTimer = 0f;
            ChasePlayer();
        }


        FollowPath();
        
    }

    public void FollowPath()
    {
        if (CurrentPath.Count == 0)
            return;
        {

            Node targetNode = CurrentPath[0];
            Vector3 targetPos = targetNode.worldPos + Vector3.up * capsuleCollider.height / 2f;

            //float DistanceToPathPoint = Vector3.Distance(transform.position, CurrentPath[0].worldPos);
            //Vector3 MoveDirection = CurrentPath[0].worldPos - StartPos;
            //MoveDirection.y = CurrentPath[0].worldPos.y;
            ////MoveDirection += transform.position;
            ///
           // if (DistanceToPathPoint > .1f)
            {
                if(!TileMover.moving)
                {
                    //TileMover.MoveInDirection(MoveDirection.normalized * 1f);
                    TileMover.MoveToPoint(targetPos);
                    StartPos = transform.position;                   
                }
            }
        }
    }
    public void ChasePlayer()
    {

        Vector3 playerPos = player.transform.position;
        CurrentPath = Pathfinder.GetPath(transform.position, playerPos);
        StartPos = transform.position;



        //if (!TileMover.moving)
        //{
        //    CurrentPath = Pathfinder.GetPath(transform.position, GameObject.FindWithTag("Player").transform.position);
        //    StartPos = transform.position;
        //    Vector3 p = transform.position;
        //    p.x = CurrentPath[0].worldPos.x;
        //    p.z = CurrentPath[0].worldPos.z;
        //    transform.position = p;
        //}
    }

    public void TileMoveFinished()
    {
        if (CurrentPath.Count > 0)
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
        else
        {
            if (player != null)
            {
                List<Node> path = Pathfinder.GetPath(transform.position, GameObject.FindWithTag("Player").transform.position);
                Color pathColor = Color.green;
                float pp = 1 / (float)CurrentPath.Count;
                if (path.Count > 0)
                {
                    foreach (Node n in path)
                    {
                        pathColor += new Color(0, -pp, pp);
                        Gizmos.color = pathColor;

                        Gizmos.DrawCube(n.worldPos, Vector3.one * 1f * .9f);
                    }
                }
            }
        } 
    }
}

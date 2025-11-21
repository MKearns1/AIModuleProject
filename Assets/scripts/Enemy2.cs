using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class Enemy2 : EnemyBase
{
    AStarPathfinding Pathfinder;
    public List<Node> CurrentPath = new List<Node>();
    TileMovement TileMover;
    Vector3 StartPos;
    Vector3 MoveDirection;
    CapsuleCollider capsuleCollider;

    float repathTimer = 0;

    Transform CurrentPatrolPoint;
    Tiles tiles;
    Node CurrentNode;

    bool Waiting;

    float TimeSinceSeenPlayer = 0;

    GameObject AgentInfo;
    Vector3 PlayerLastSeenLocation;

    float SearchTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Pathfinder = GetComponent<AStarPathfinding>();
        TileMover = GetComponent<TileMovement>();
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        //tiles = GameObject.Find("Tiles").GetComponent<Tiles>();
        tiles = GameObject.Find("Terrain").GetComponent<Tiles>();
        vision = GetComponent<Vision>();
        AgentInfo = transform.Find("AgentInfo").gameObject;

        tiles.GetNodeFromWorldPosition(transform.position).occupied = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            StartChase();
        }
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartPatrol();
            CurrentState = EnemyStates.Patrol;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartSearch();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            StartChase();
            CurrentState = EnemyStates.Chase;

        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            StartSearch();
            CurrentState = EnemyStates.Search;

        }
        repathTimer += Time.deltaTime;

        if (repathTimer > 0.5f && !TileMover.moving)
        {
            repathTimer = 0f;
            //ChasePlayer();
        }


        if (vision.CanSeeGameObject(player.gameObject))
        {
            TimeSinceSeenPlayer = 0f;
            PlayerLastSeenLocation = player.transform.position;

            if (CurrentState != EnemyStates.Chase)
            {
                ChangeState(EnemyStates.Chase);
            }
        }
        else
        {
            TimeSinceSeenPlayer += Time.deltaTime;
        }
        Debug.Log(TimeSinceSeenPlayer);

        FollowPath();

        float DistanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        //if(DistanceToPlayer < 15)
        //{
        //    if (Time.time > NextAttack)
        //    {
        //        NextAttack = Time.time + AttackRate;
        //      //  Shoot(player.transform.position);
        //    }
        //}

        switch (CurrentState)
        {
            case EnemyStates.Idle:
                Idle();
                break;



            case EnemyStates.Patrol:
                PatrolUpdate();
                break;


            case EnemyStates.Chase:
                ChaseUpdate();
                break;


            case EnemyStates.Attacking:

                break;


            case EnemyStates.Search:
                SearchUpdate();
                break;


            case EnemyStates.Retreat:

                break;


        }

        AgentInfo.transform.rotation = Camera.main.transform.rotation;
        AgentInfo.transform.Find("StateText").transform.GetComponent<TextMeshPro>().text = "State: " + CurrentState.ToString();
        AgentInfo.transform.Find("HealthText").transform.GetComponent<TextMeshPro>().text = "Health: " + Health.ToString();

    }

    public void FollowPath()
    {
        CurrentNode = tiles.GetNodeFromWorldPosition(transform.position);

        if (CurrentPath.Count == 0 || Waiting)
            return;


        if (CurrentPath.Count == 1 && CurrentPath[0].occupied)
        {
            CurrentPath.Clear();
            return;
        }
        if (CurrentPath[CurrentPath.Count - 1].occupied) CurrentPath.RemoveAt(CurrentPath.Count-1);

        if (CurrentPath[0] == CurrentNode) CurrentPath.RemoveAt(0);

        if (CurrentPath.Count == 0) return;

        Node targetNode = CurrentPath[0];
        Vector3 targetPos = targetNode.worldPos + Vector3.up * capsuleCollider.height / 2f;

        //float DistanceToPathPoint = Vector3.Distance(transform.position, CurrentPath[0].worldPos);
        //Vector3 MoveDirection = CurrentPath[0].worldPos - StartPos;
        //MoveDirection.y = CurrentPath[0].worldPos.y;
        ////MoveDirection += transform.position;
        ///
        // if (DistanceToPathPoint > .1f)
        {
            if (!TileMover.moving)
            {
                //TileMover.MoveInDirection(MoveDirection.normalized * 1f);

                tiles.GetNodeFromWorldPosition(targetPos).occupied = true;
                tiles.GetNodeFromWorldPosition(transform.position).occupied = false;
                TileMover.MoveToPoint(targetPos);
                StartPos = transform.position;
            }
        }

    }
    public void StartChase()
    {
        Debug.Log("START CHASE");
        Vector3 playerPos = player.transform.position;
        CurrentPath = Pathfinder.GetPath(transform.position, playerPos);
        TileMover.movementSpeed = 6;

        // StartPos = transform.position;



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

        if (CurrentPath.Count == 0)
        {
            switch (CurrentState)
            {
                case EnemyStates.Patrol:
                    StartCoroutine(Wait());

                    StartPatrol();
                    break;
            }
        }
    }

    void Idle()
    {

    }


    void StartPatrol()
    {
        Transform PatrolPoints = GameObject.Find("PatrolPoints").transform;

        List<Transform> Point = new List<Transform>();

        foreach (Transform t in PatrolPoints)
        {
            if (t.gameObject.activeInHierarchy)
            {
                Point.Add(t);
            }
        }

        Vector3 RandomOffset = Random.insideUnitSphere * 2;
       
      //  Pathfinder.GetPath(transform.position, randomPatrolPoint.position + RandomOffset)

        Transform randomPatrolPoint = Point[Random.Range(0, Point.Count)];

        Node TargetNode = tiles.GetNodeFromWorldPosition(randomPatrolPoint.position + RandomOffset);
        CurrentNode = tiles.GetNodeFromWorldPosition(transform.position);

        if (CurrentNode == TargetNode) { StartPatrol(); return; }

        CurrentPath = Pathfinder.GetPath(transform.position, TargetNode.worldPos);

        if (!CurrentPath.Contains(TargetNode)) { StartPatrol(); return; }

        StartPos = transform.position;
        CurrentPatrolPoint = randomPatrolPoint;

        TileMover.movementSpeed = 3;

    }

    void PatrolUpdate()
    {
        if (Vector3.Distance(transform.position, CurrentPatrolPoint.position) < 0.5f)
        {
            StartPatrol();
        }

        if (PlayerVisible())
        {
            ChangeState(EnemyStates.Chase);
            StartChase();
        }
    }

    void ChaseUpdate()
    {
        if(TimeSinceSeenPlayer > 3)
        {
            Stop();
            ChangeState(EnemyStates.Search);
            return;
        }
        //if(Vector3.Distance(transform.position, player.transform.position) < 10 && TimeSinceSeenPlayer < 1)
        if(Vector3.Distance(transform.position, player.transform.position) < 10 && TimeSinceSeenPlayer < 1)
        {
            Stop();

            Vector3 playerpos = player.transform.position;
            playerpos.y = 0;
            Vector3 me = transform.position;
            me.y = 0;

            Vector3 LookDir = (playerpos - me).normalized;

            transform.rotation = Quaternion.LookRotation(LookDir);

            if (Time.time > NextAttack)
            {
                NextAttack = Time.time + AttackRate;
                //  Shoot(player.transform.position);
                Shoot(player.transform.position);
                Debug.Log("SHOOT");

            }
        }
        else
        {
            StartChase();
        }
    }

    void StartSearch()
    {

        Vector3 RandomPoint = Vector3.zero;

        for (int i = 0; i < 100; i++)
        {
            Vector3 RandomOffset = Random.insideUnitSphere*20;
            RandomOffset.y = 0;

            RandomPoint = PlayerLastSeenLocation + RandomOffset;

            Node node = tiles.GetNodeFromWorldPosition(RandomPoint);

            if (node.nodeTyoe != NodeType.Untraversable)
            {
                break;
            }

        }
        

        CurrentPath = Pathfinder.GetPath(transform.position, RandomPoint);
        TileMover.movementSpeed = 4;

    }

    void SearchUpdate()
    {
        if(CurrentPath.Count == 0)
        {
            StartSearch();
        }

        SearchTimer += Time.deltaTime;

        if (SearchTimer > 15)
        {
            SearchTimer = 0;
            ChangeState(EnemyStates.Patrol);
            return;
        }
    }

    void ChangeState(EnemyStates NewState)
    {
        switch (NewState)
        {
            case EnemyStates.Chase:
                StartChase();
                break;

            case EnemyStates.Search:
                StartSearch();
                break;
        }
        CurrentState = NewState;
    }

    bool PlayerVisible()
    {
        bool Visible = false;

        return Visible;
    }


    IEnumerator Wait()
    {
        Waiting = true;

        yield return new WaitForSeconds(3);

        Waiting = false;

    }

    void Stop()
    {
        CurrentPath.Clear();
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

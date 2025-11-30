using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;
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

    public GameObject PlayerLastSeenPrefab;
    GameObject PlayerLastSeenObject;

    List<Node> PotentialRetreatPoints = new List<Node>();

    float RetreatProbability;
    float RetreatChoice = -1;

    float LookAroundSpeed = 100;
    float LookAroundProgress = 0;

    Quaternion idleStartRot;
    float idleTimer = 0;

    public SuperStates CurrentSuperState;
    public EnemyStates CurrentState;


    public EnemyDog CurrentDog;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Pathfinder = GetComponent<AStarPathfinding>();
        TileMover = GetComponent<TileMovement>();
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        tiles = GameObject.FindFirstObjectByType<Tiles>();
        //tiles = GameObject.Find("Terrain").GetComponent<Tiles>();
        vision = GetComponent<Vision>();
        AgentInfo = transform.Find("AgentInfo").gameObject;


        tiles.GetNodeFromWorldPosition(transform.position).SetOccupied(true, gameObject);


        ChangeState(EnemyStates.Explore_Idle, SuperStates.Explore);
    }

    // Update is called once per frame
    void Update()
    {
        if (tiles.NodesGrid == null) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            StartChase();
        }
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartPatrol();
            CurrentState = EnemyStates.Explore_Patrol;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartSearch();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            StartChase();
            CurrentState = EnemyStates.Alert_Chase;

        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
           // StartSearch();
           // CurrentState = EnemyStates.Alert_Search;
            ChangeState(EnemyStates.Alert_Search, SuperStates.Alert);


        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            ChangeState(EnemyStates.Combat_Retreat, SuperStates.Combat);

        }
        repathTimer += Time.deltaTime;

        if (repathTimer > 0.5f && !TileMover.moving)
        {
            repathTimer = 0f;
            //ChasePlayer();
        }

        CanSeePlayer = vision.CanSeeGameObject(player.gameObject);

        if (!CanSeePlayer)
        {
            TimeSinceSeenPlayer += Time.deltaTime;
        }
        else
        {
            SawPlayer();
        }
           // Debug.Log(CanSeePlayer);

            //Debug.Log(TimeSinceSeenPlayer);

            FollowPath();

        float DistanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        RetreatProbability = 1 - ((float)Health / (float)MaxHealth);

/*        //if(DistanceToPlayer < 15)
        //{
        //    if (Time.time > NextAttack)
        //    {
        //        NextAttack = Time.time + AttackRate;
        //      //  Shoot(player.transform.position);
        //    }
        //}

        //switch (CurrentState)
        //{
        //    case EnemyStates.Idle:
        //        Idle();
        //        break;



        //    case EnemyStates.Patrol:
        //        PatrolUpdate();
        //        break;


        //    case EnemyStates.Chase:
        //        ChaseUpdate();
        //        break;


        //    case EnemyStates.Attacking:

        //        break;


        //    case EnemyStates.Search:
        //        SearchUpdate();
        //        break;


        //    case EnemyStates.Retreat:
        //        RetreatUpdate();
        //        break;


        //}*/


        switch (CurrentSuperState)
        {
            case SuperStates.Explore:
                ExplorationUpdate();
                break;

            case SuperStates.Alert:
                AlertUpdate();
                break;

            case SuperStates.Combat:
                CombatUpdate();
                break;
        }


        AgentInfo.transform.rotation = Camera.main.transform.rotation;
        AgentInfo.transform.Find("StateText").transform.GetComponent<TextMeshPro>().text = "State: " + CurrentState.ToString();
        AgentInfo.transform.Find("HealthText").transform.GetComponent<TextMeshPro>().text = "Health: " + Health.ToString();
        AgentInfo.transform.Find("BotID").transform.GetComponent<TextMeshPro>().text = "ID: " + ID;

     //   Debug.Log("tIMESINCESEENPLAYER: "+ TimeSinceSeenPlayer);

    }

    public void FollowPath()
    {
        if(tiles.NodesGrid == null)return;
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

        {
            if (!TileMover.moving)
            {

                tiles.GetNodeFromWorldPosition(transform.position).SetOccupied(false, gameObject);
                tiles.GetNodeFromWorldPosition(targetPos).SetOccupied(true, gameObject);

                TileMover.MoveToPoint(targetPos);
                StartPos = transform.position;
            }
        }

    }

    void ExplorationUpdate()
    {
        switch (CurrentState)
        {
            case EnemyStates.Explore_Idle:
                IdleUpdate();
                break;

            case EnemyStates.Explore_Patrol:
                PatrolUpdate();
                break;

        }
        if (CanSeePlayer)
        {    
            ChangeState(EnemyStates.Alert_Chase, SuperStates.Alert);
        }
    }

    void AlertUpdate()
    {
        if (TimeSinceSeenPlayer > 5)
        {
            if (CurrentState == EnemyStates.Alert_LookAround || CurrentState == EnemyStates.Alert_Search) { }
            else
            {
                ChangeState(EnemyStates.Alert_LookAround, SuperStates.Alert);
            }
            
        }

        switch (CurrentState)
        {
            case EnemyStates.Alert_Search:
                SearchUpdate();
                break;

            case EnemyStates.Alert_LookAround:
                LookAroundUpdate();
                break;

            case EnemyStates.Alert_Chase:
                ChaseUpdate();
                break;

        }

        if (TimeSinceSeenPlayer < .1f)
        {
            if (CurrentState == EnemyStates.Alert_Search || CurrentState == EnemyStates.Alert_LookAround)
            {
                ChangeState(EnemyStates.Alert_Chase, SuperStates.Alert);
            }
            return;
        }
        

    }

    void LookAroundUpdate()
    {
        float step = LookAroundSpeed * Time.deltaTime;

        transform.Rotate(0, step, 0);

        LookAroundProgress += step;

       // Debug.Log(LookAroundProgress);

        if (LookAroundProgress >= 360f)
        {
            ChangeState(EnemyStates.Alert_Search, SuperStates.Alert);
        }
    }
   

    void SawPlayer()
    {
        TimeSinceSeenPlayer = 0f;
        PlayerLastSeenLocation = player.transform.position;

        Destroy(PlayerLastSeenObject);
        PlayerLastSeenObject = GameObject.Instantiate(PlayerLastSeenPrefab, player.transform.position, player.transform.rotation);

    }

    public void TileMoveFinished()
    {
        if (CurrentPath.Count > 0)
            CurrentPath.RemoveAt(0);

        if (CurrentPath.Count == 0)
        {
            switch (CurrentState)
            {
                case EnemyStates.Explore_Patrol:
                    StartCoroutine(Wait(.5f));

                    StartPatrol();
                    break;
            }
        }
    }

    void IdleUpdate()
    {
        float lookAmount = Mathf.Sin(Time.time * 3f) * 45;

        Quaternion offset = Quaternion.Euler(0, lookAmount, 0);

        transform.rotation = idleStartRot * offset;

        idleTimer += Time.deltaTime;

        if(idleTimer > 3)
        {
            ChangeState(EnemyStates.Explore_Patrol, SuperStates.Explore);
        }
    }

    void StartIdle()
    {
        Stop();
        Node n = vision.GetFurthestVisibleNode();
        if ((n==null))
        {
            return;
        }
        Vector3 FurthestNode = n.worldPos;
        FurthestNode.y = 0;
        Vector3 me = transform.position;
        me.y = 0;

        Vector3 LookDir = (FurthestNode - me).normalized;

        transform.rotation = Quaternion.LookRotation(LookDir);

        idleStartRot = transform.rotation;
        idleTimer = 0;
        //GameObject m = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //m.transform.position = FurthestNode;
    }

    void StartPatrol()
    {

        GameObject PatrolPoints = GameObject.Find("PatrolPoints");

        if (PatrolPoints == null)
        {
            if (GameObject.Find("AutoPatrolPoint" + ID) != null) Destroy(GameObject.Find("AutoPatrolPoint" + ID));
            List<Node> ReachableNodes = Pathfinder.GetReachableNodesFromPosition(tiles.GetNodeFromWorldPosition(transform.position),false);
            int randomPoint = Random.Range(0, ReachableNodes.Count);

            GameObject patrolpoint = new GameObject();
            patrolpoint.transform.position = ReachableNodes[randomPoint].worldPos;
            patrolpoint.name = "AutoPatrolPoint"+ID;
            CurrentPath = Pathfinder.GetPath(transform.position, patrolpoint.transform.position);

            StartPos = transform.position;
            CurrentPatrolPoint = patrolpoint.transform;
            return;
        }

        List<Transform> Point = new List<Transform>();

        foreach (Transform t in PatrolPoints.transform)
        {
            if (t.gameObject.activeInHierarchy)
            {
                Point.Add(t);
            }
        }

        Vector3 RandomOffset = Random.insideUnitSphere * 2;
       
        Transform randomPatrolPoint = Point[Random.Range(0, Point.Count)];

        Node TargetNode = tiles.GetNodeFromWorldPosition(randomPatrolPoint.position + RandomOffset);
        CurrentNode = tiles.GetNodeFromWorldPosition(transform.position);

        if (CurrentNode == TargetNode) { StartPatrol(); return; }

        CurrentPath = Pathfinder.GetPath(transform.position, TargetNode.worldPos);

        if (!CurrentPath.Contains(TargetNode)) { StartPatrol(); return; }

        StartPos = transform.position;
        CurrentPatrolPoint = randomPatrolPoint;

    }

    void PatrolUpdate()
    {
        if(CurrentPatrolPoint == null) { StartPatrol(); }

        if (Vector3.Distance(transform.position, CurrentPatrolPoint.position) < 0.5f)
        {
            if(CurrentPatrolPoint.name == "AutoPatrolPoint"+ID) Destroy(CurrentPatrolPoint.gameObject);
            StartPatrol();
        }

    }
   

    void StartSearch()
    {
        List<(Node node, float weight)> candidates = new List<(Node, float)>();


        for (int i = 0; i < 40; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 20;
            offset.y = 0;

            Vector3 testPos = PlayerLastSeenLocation + offset;
            Node node = tiles.GetNodeFromWorldPosition(testPos);

            if (node.nodeTyoe == NodeType.Untraversable)
                continue;

            float distance = Vector3.Distance(testPos, PlayerLastSeenLocation);

            float weight = 1 / (1 + distance);

            candidates.Add((node, weight));
        }

        if (candidates.Count == 0)
        {
            ChangeState(EnemyStates.Explore_Patrol, SuperStates.Explore);
            return;
        }

        float total = 0;
        foreach(var n in candidates) {total += n.weight;}
        float roll = Random.Range(0,total);
        float cumulative = 0;

        Node picked = null;

        foreach (var c in candidates)
        {
            cumulative += c.weight;
            if (roll <= cumulative)
            {
                picked = c.node;
                break;
            }
        }

        if (picked == null)
        {
            picked = candidates[0].node;
        }

        CurrentPath = Pathfinder.GetPath(transform.position, picked.worldPos);

    }

    void SearchUpdate()
    {
        if (SearchTimer > 15)
        {
            SearchTimer = 0;
            TimeSinceSeenPlayer = 999999;
            ChangeState(EnemyStates.Explore_Patrol, SuperStates.Explore);
            Destroy(PlayerLastSeenObject);
            return;
        }

        SearchTimer += Time.deltaTime;

        Debug.Log(SearchTimer.ToString());

        if (CurrentPath.Count == 0)
        {
            StartCoroutine(Wait(.5f));
            StartSearch();
        }      
    }

/*    void ExitSuperState(SuperStates oldSuperState)
    {
        switch (oldSuperState)
        {
            case SuperStates.Alert:
                break;
        }
    }*/

    void EnterSuperState(SuperStates newSuperState)
    {
        switch (newSuperState)
        {
            case SuperStates.Explore:
                TileMover.movementSpeed = 2;
                setColour(Color.green);
                break;

            case SuperStates.Alert:
                TileMover.movementSpeed = 4;
                setColour(Color.yellow);
                break;

            case SuperStates.Combat:
                TileMover.movementSpeed = 5;
                setColour(Color.red);
                break;
        }

    }

    void ChangeState(EnemyStates NewState, SuperStates NewSuperState)
    {
        if (NewSuperState != CurrentSuperState)
        {
            EnterSuperState(NewSuperState);
            CurrentSuperState = NewSuperState;
        }

        CurrentState = NewState;
        RetreatChoice = Random.value;

        switch (NewState)
        {
            case EnemyStates.Explore_Idle: StartIdle(); break;
            case EnemyStates.Explore_Patrol: StartPatrol(); break;
            case EnemyStates.Alert_Search: StartSearch(); break;
            case EnemyStates.Alert_Chase: StartChase(); break;
            case EnemyStates.Combat_Attacking: StartAttack(); break;
            case EnemyStates.Combat_Retreat: StartRetreat(); break;
            case EnemyStates.Alert_LookAround: StartLookAround(); break;
        }
        return;
    }

    void StartLookAround()
    {
        Stop();

        LookAroundProgress = 0;
    }

    public void StartChase()
    {
        Debug.Log("START CHASE");
        Vector3 playerPos = player.transform.position;
        CurrentPath = Pathfinder.GetPath(transform.position, playerPos);
    }

    void ChaseUpdate()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distToPlayer > 5 || TimeSinceSeenPlayer > 1f)
        {
            StartChase();
        }
        else 
        {
            Stop();

            if (RetreatChoice < RetreatProbability)
            {
                ChangeState(EnemyStates.Combat_Retreat, SuperStates.Combat);
            }
            else
            {
                ChangeState(EnemyStates.Combat_Attacking, SuperStates.Combat);
            }
        }
        return;
    }

    void CombatUpdate()
    {
        switch (CurrentState)
        {
            case EnemyStates.Combat_Retreat:
                RetreatUpdate();
                break;

            case EnemyStates.Combat_Attacking:
                AttackUpdate();
                break;

        }

        if (TimeSinceSeenPlayer > 3 || Vector3.Distance(transform.position, player.transform.position) > 10)
        {
            if (CurrentState == EnemyStates.Combat_Attacking)
            {
                ChangeState(EnemyStates.Alert_Chase, SuperStates.Alert);
            }
            return;
        }

    }

    void AttackUpdate()
    {
        if(CanSeePlayer)
        {
            Vector3 playerpos = player.transform.position;
            playerpos.y = 0;
            Vector3 me = transform.position;
            me.y = 0;

            Vector3 LookDir = (playerpos - me).normalized;

            transform.rotation = Quaternion.LookRotation(LookDir);

            if (Time.time > NextAttack)
            {
                NextAttack = Time.time + AttackRate;
                Shoot(player.transform.position, CalculateDamageAmount());
                Debug.Log("SHOOT");

            }
        }

    }

    void StartAttack()
    {
        Debug.Log("StartAttack");

        Stop();
    }

    void StartRetreat()
    {
        TileMover.movementSpeed = 7;

        List<Node> candidates = new List<Node>();

        Node myNode = tiles.GetNodeFromWorldPosition(transform.position);
        int searchRadius = 20;

        // --- STEP 1: Populate candidates ---
        for (int x = -searchRadius; x <= searchRadius; x++)
        {
            for (int y = -searchRadius; y <= searchRadius; y++)
            {
                int nx = myNode.GridPos.x + x;
                int ny = myNode.GridPos.y + y;

                // Bounds check
                if (nx < 0 || ny < 0 || nx >= tiles.GridSize || ny >= tiles.GridSize)
                    continue;

                Node n = tiles.NodesGrid[nx, ny];

                if (n.nodeTyoe == NodeType.Untraversable)
                    continue;

                if (n.occupied)
                    continue;

                if (CanPlayerSeePosition(n.worldPos))
                    continue;

                candidates.Add(n);
            }
        }

        // No candidates? fall back
        if (candidates.Count == 0)
        {
            Debug.LogWarning("No retreat nodes found. Falling back.");
            Destroy(gameObject);
            ChangeState(EnemyStates.Explore_Idle, SuperStates.Explore);
            return;
        }

        // --- STEP 2: Utility scoring ---
        Node bestNode = null;
        float bestScore = Mathf.NegativeInfinity;

        foreach (Node n in candidates)
        {
            //List<Node> path = Pathfinder.GetPath(transform.position, n.worldPos);
            //if (path.Count == 0) continue;

            float distanceScore = Vector3.Distance(n.worldPos, player.transform.position);
            int coverScore =1;

            float utility = distanceScore * 1.5f + coverScore * 3f;

            if (utility > bestScore)
            {
                bestScore = utility;
                bestNode = n;
            }
        }

        // Path found?
        if (bestNode != null)
        {
            CurrentPath = Pathfinder.GetPath(transform.position, bestNode.worldPos);
            GameObject n = GameObject.CreatePrimitive(PrimitiveType.Cube);
            n.transform.position = bestNode.worldPos;
            Debug.Log("Retreating to best utility node: " + bestNode.GridPos);
            return;
        }

        // Final fallback
        Debug.LogWarning("No valid retreat path found.");
        ChangeState(EnemyStates.Explore_Idle, SuperStates.Explore);
    }


    void RetreatUpdate()
    {
        if (CurrentPath.Count > 0)
        {
            return;
        }
        else
        {
            ChangeState(EnemyStates.Explore_Idle, SuperStates.Explore);

        }

        if (CanPlayerSeeMe() && CanSeePlayer)
        {
            RetreatChoice = Random.value;
            if (RetreatChoice < RetreatProbability)
            {
                StartRetreat();
            }
            else
            {
                ChangeState(EnemyStates.Combat_Attacking, SuperStates.Combat);
            }

        }
    }


    IEnumerator Wait(float time)
    {
        Waiting = true;

        yield return new WaitForSeconds(time);

        Waiting = false;

    }

    void Stop()
    {
        CurrentPath.Clear();
    }


    void setColour(Color newColor)
    {
        GetComponent<Renderer>().material.color = newColor;

        foreach (Transform t in transform.Find("Arms"))
        {
            t.GetComponent<Renderer>().material.color = newColor;
        }
    }


    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        Vector3 playerpos = player.transform.position;
        playerpos.y = 0;
        Vector3 me = transform.position;
        me.y = 0;

        Vector3 LookDir = (playerpos - me).normalized;

        transform.rotation = Quaternion.LookRotation(LookDir);


        RetreatChoice = Random.value;
        if (RetreatChoice < RetreatProbability)
        {
            ChangeState(EnemyStates.Combat_Retreat, SuperStates.Combat);
        }
        else
        {
            ChangeState(EnemyStates.Combat_Attacking, SuperStates.Combat);
        }
    }

    public override float CalculateDamageAmount()
    {
        float BaseDamage = 1;

        float healthpercent = (float)Health / (float)MaxHealth;
        float LowHealthBonusChance = 1 - healthpercent;

        int EnemiesRemaining = GameObject.FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
        int MaxEnemies = 15;
        EnemiesRemaining = Mathf.Clamp(EnemiesRemaining, 1, MaxEnemies);

        float LowEnemyBonusChance = 1 - ((float)EnemiesRemaining / (float)MaxEnemies);

        float LowHealthBonus = 0;
        float LowEnemyBonus = 0;

        if (Random.Range(0f, 1f) < LowHealthBonusChance) LowHealthBonus = 1f;
        if (Random.Range(0f, 1f) < LowEnemyBonusChance) LowEnemyBonus = 1f;

        float FinalDamage = BaseDamage + LowHealthBonus + LowEnemyBonus;

        return FinalDamage;
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
            if (player != null && tiles.NodesGrid != null)
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

        if (PotentialRetreatPoints.Count > 0)
        {

            foreach (Node n in PotentialRetreatPoints)
            {
                Gizmos.color = Color.magenta;

                Gizmos.DrawCube(n.worldPos, Vector3.one * 1f * .9f);

            }
        }
    }
}

using NUnit.Framework;
using System.Net;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyDog : EnemyBase
{
    NavMeshAgent agent;
    TextMeshPro HealthText;
    DogEnemyStates CurrentState;
    DogSuperStates CurrentSuperState;
    [SerializeField]
    public Enemy2 CurrentOwner;
    float MovementSpeed = 5;
    Tiles TileScript;
    bool isMovingToTarget;
    GameObject AgentInfo;
    List<Node> NearbyNodes;
    float TimeSinceSeenPlayer = 0;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
        HealthText = transform.Find("HealthText").GetComponent<TextMeshPro>();
        TileScript = GameObject.FindFirstObjectByType<Tiles>();
        AgentInfo = transform.Find("AgentInfo").gameObject;
        vision = GetComponent<Vision>();
        agent.speed = MovementSpeed;

        Enemy2 newowner = FindNewOwner();
        if (newowner != null)
        {
            CurrentOwner = newowner;
            CurrentOwner.CurrentDog = this;
        }

        ChangeState(DogEnemyStates.FollowOwner, DogSuperStates.Passive);
    }

    // Update is called once per frame
    void Update()
    {
        CanSeePlayer = vision.CanSeeGameObject(player.gameObject);

        if (CanSeePlayer)
        {
            TimeSinceSeenPlayer = 0;
        }
        else
        {
            TimeSinceSeenPlayer += Time.deltaTime;
        }

            NearbyNodes = GetNearbyNodes();

        switch (CurrentSuperState)
        {
            case DogSuperStates.Passive:
                PassiveUpdate(); break;

            case DogSuperStates.Tracking:
                TrackingUpdate(); break;

            case DogSuperStates.Combat:
                CombatUpdate(); break;
        }


        

        AgentInfo.transform.rotation = Camera.main.transform.rotation;
        AgentInfo.transform.Find("StateText").transform.GetComponent<TextMeshPro>().text = "State: " + CurrentState.ToString();
        AgentInfo.transform.Find("HealthText").transform.GetComponent<TextMeshPro>().text = "Health: " + Health.ToString();
    }


    void PassiveUpdate()
    {
        switch (CurrentState)
        {
            case DogEnemyStates.Idle:
                IdleUpdate();
                break;

            case DogEnemyStates.FollowOwner:
                FollowOwnerUpdate();
                break;
            case DogEnemyStates.Wander:
                WanderUpdate();
                break;
        }

        if (CanSeePlayer)
        {
            ChangeState(DogEnemyStates.Charge, DogSuperStates.Combat);
            return;
        }

        if (NearbyNodes.Count > 0)
        {
            foreach (var node in NearbyNodes)
            {
                if(node.PlayerScentStrength > 0)
                {
                    ChangeState(DogEnemyStates.FollowTrail, DogSuperStates.Tracking);
                    return;
                }
            }
        }


        if (CurrentOwner != null && CurrentOwner.isActiveAndEnabled)
        {
            float DistToOwner = Vector3.Distance(CurrentOwner.transform.position, transform.position);

            if (DistToOwner < 3)
            {
                if (CurrentState != DogEnemyStates.Idle)
                    ChangeState(DogEnemyStates.Idle, DogSuperStates.Passive);
            }
            else if (DistToOwner > 4)
            {
                if (CurrentState != DogEnemyStates.FollowOwner)
                    ChangeState(DogEnemyStates.FollowOwner, DogSuperStates.Passive);
            }

        }
        else
        {
            if (CurrentState != DogEnemyStates.Wander)
                ChangeState(DogEnemyStates.Wander, DogSuperStates.Passive);
        }
    }

    void TrackingUpdate()
    {
        switch (CurrentState)
        {
            case DogEnemyStates.FollowTrail:
                FollowTrailUpdate();
                break;


        }

        if (CanSeePlayer)
        {
            ChangeState(DogEnemyStates.Charge, DogSuperStates.Combat);
            return;
        }


        if (NearbyNodes.Count > 0)
        {
            List<Node> SmellyNodes = new List<Node>();
            foreach (Node n in NearbyNodes)
            {
                if(n.PlayerScentStrength > 0)
                {
                    SmellyNodes.Add(n);
                }
            }

            if(SmellyNodes.Count == 0)
            {
                ChangeState(DogEnemyStates.FollowOwner, DogSuperStates.Passive);
            }
        }
    }

    void CombatUpdate()
    {
        switch (CurrentState)
        {
            case DogEnemyStates.Charge:
                ChargeUpdate();
                break;

            case DogEnemyStates.Attack:
                AttackUpdate();
                break;

        }

        float DistToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (TimeSinceSeenPlayer > 2)
        {
            ChangeState(DogEnemyStates.FollowTrail, DogSuperStates.Tracking);
            return;
        }

        if (DistToPlayer < 5 && CanSeePlayer)
        {

            if (CurrentState == DogEnemyStates.Attack) return;
            ChangeState(DogEnemyStates.Attack, DogSuperStates.Combat);

        }
        else if (DistToPlayer > 7)
        {
            if (CurrentState == DogEnemyStates.Charge) return;
            ChangeState(DogEnemyStates.Charge, DogSuperStates.Combat);
        }
    }

    void ChangeState(DogEnemyStates newState, DogSuperStates newSuper)
    {
        switch (newState)
        {

        }

        CurrentState = newState;
        CurrentSuperState = newSuper;
        agent.ResetPath();
    }
    void IdleUpdate()
    {
        agent.isStopped = true;
        agent.ResetPath();
    }

    void FollowOwnerUpdate()
    {
        if (CurrentOwner == null || !CurrentOwner.isActiveAndEnabled) return;
       agent.SetDestination(CurrentOwner.transform.position);
    }

    void WanderUpdate()
    {
        if (!agent.hasPath)
        {
            int Max = TileScript.GridSize;

            int randx = Random.Range(0, Max);
            int randy = Random.Range(0, Max);

            agent.SetDestination(TileScript.NodesGrid[randx, randy].worldPos);
        }

    }

    void FollowTrailUpdate()
    {
        Node BestNode = TileScript.GetNodeFromWorldPosition(transform.position);

        foreach (var node in NearbyNodes)
        {
            if (node.PlayerScentStrength > 0)
            {
                if(node.PlayerScentStrength > BestNode.PlayerScentStrength)
                {
                    BestNode = node;
                }
            }
        }

        if (BestNode != TileScript.GetNodeFromWorldPosition(transform.position))
        {
            agent.SetDestination(BestNode.worldPos);
        }

    }

    void ChargeUpdate()
    {
        agent.SetDestination(player.transform.position);
    }

    void AttackUpdate()
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
            Shoot(player.transform.position);
            Debug.Log("SHOOT");

        }
    }

    Enemy2 FindNewOwner()
    {
        Enemy2[] PossibleOwners = GameObject.FindObjectsByType<Enemy2>(FindObjectsSortMode.None);

        Enemy2 Candidate = null;

        foreach (Enemy2 p in PossibleOwners)
        {
            if (p == null) continue;
            if (p.CurrentDog == null) Candidate = p;
        }


        return Candidate;
    }

    List<Node> GetNearbyNodes()
    {
        List<Node> nearbyNodes = new List<Node>();

        Node CurNode = TileScript.GetNodeFromWorldPosition(transform.position);

        int radius = 5;

        for (int x = -radius; x < radius; x++)
        {
            for (int y = -radius; y < radius; y++)
            {
                int nextX = CurNode.GridPos.x + x;
                int nextY = CurNode.GridPos.y + y;

                if(nextX > TileScript.GridSize-1 || nextX < 0 || nextY > TileScript.GridSize-1 || nextY < 0)continue;
                if (TileScript.NodesGrid[nextX, nextY].nodeTyoe == NodeType.Untraversable) continue;

                nearbyNodes.Add(TileScript.NodesGrid[nextX, nextY]);

            }

        }

        return nearbyNodes;
    }

    private void OnDrawGizmos()
    {
        if(NearbyNodes == null) return; 
        if (NearbyNodes.Count > 0)
        {
            foreach (Node n in NearbyNodes)
            {
                Gizmos.color = Color.cyan;

                if(n.PlayerScentStrength > 0) { Gizmos.color = Color.blue; }
                Gizmos.DrawCube(n.worldPos, Vector3.one);
            }
        }
    }
}

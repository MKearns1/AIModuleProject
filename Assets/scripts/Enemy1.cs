using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class Enemy1 : EnemyBase
{
    NavMeshAgent agent;
    TextMeshPro HealthText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
        agent.SetDestination(player.transform.position);
        HealthText = transform.Find("HealthText").GetComponent<TextMeshPro>();

    }

    // Update is called once per frame
    void Update()
    {
        agent.SetDestination(player.transform.position);

        float DistFromPlayer = Vector3.Distance(agent.transform.position,player.transform.position);

        if(DistFromPlayer < 2)
        {
            Attack();
        }

        HealthText.text = Health.ToString();
    }

   
}

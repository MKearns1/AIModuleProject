using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class Enemy1 : MonoBehaviour
{
    NavMeshAgent agent;
    Player player;
    float BiteRate = 1f;
    float NextAttack;
    int Health = 5;
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

    void Attack()
    {
        if (Time.time > NextAttack)
        {
            NextAttack = Time.time + BiteRate;

            player.TakeDamage(1);
        }
    }

    public void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health <= 0)
        {
            Death();
        }
    }

    public void Death()
    {
        gameObject.SetActive(false);
    }
}

using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    public Player player;
    public float BiteRate = 1f;
    public float NextAttack;
    public int Health = 5;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Attack()
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

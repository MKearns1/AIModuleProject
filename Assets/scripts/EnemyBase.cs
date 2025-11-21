using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    public Player player;
    public float AttackRate = 1f;
    public float NextAttack;
    public int Health = 5;
    public GameObject BulletPrefab;
    public Transform BulletSpawnPos;
    public EnemyStates CurrentState;
    protected Vision vision;

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
            NextAttack = Time.time + AttackRate;

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

    public void Shoot(Vector3 Target)
    {
        GameObject newBullet = Instantiate(BulletPrefab, BulletSpawnPos.position, Quaternion.identity);

        EnemyBulletScript bullet = newBullet.GetComponent<EnemyBulletScript>();

        Vector3 Dir = Target - BulletSpawnPos.position;
        Dir = Dir.normalized;

        bullet.Initialize(Dir, this.gameObject);
        //bullet.Initialize(transform.forward);
        newBullet.transform.rotation = transform.rotation;
    }
}

public enum EnemyStates
{
    Idle,
    Patrol,
    Chase,
    Attacking,
    Retreat,
    Search,
}
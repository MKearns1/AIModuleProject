using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    public Player player;
    public float AttackRate = 1f;
    public float NextAttack;
    public int Health = 5;
    public int MaxHealth = 5;
    public string ID;
    public GameObject BulletPrefab;
    public Transform BulletSpawnPos;

    protected Vision vision;
    protected bool CanSeePlayer;

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

    public virtual void TakeDamage(int amount)
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

    public void Shoot(Vector3 Target, float DamageAmount)
    {
        GameObject newBullet = Instantiate(BulletPrefab, BulletSpawnPos.position, Quaternion.identity);

        EnemyBulletScript bullet = newBullet.GetComponent<EnemyBulletScript>();

        Vector3 Dir = Target - BulletSpawnPos.position;
        Dir = Dir.normalized;

        bullet.Initialize(Dir, this.gameObject, DamageAmount);
        newBullet.transform.rotation = transform.rotation;
    }


    public bool CanPlayerSeePosition(Vector3 Position)
    {
        Vector3 Dir = (Position - player.transform.position).normalized;
        float Dist = Vector3.Distance(Position, player.transform.position);

        if(Physics.Raycast(player.transform.position, Dir, out RaycastHit hit, Dist*100))
        {
            if (hit.collider.gameObject.tag == "Ground")
            {
                return true;

            }
            return false;

        }
        return true;
    }

    public bool CanPlayerSeeMe()
    {
        Vector3 Dir = (transform.position - player.transform.position).normalized;
        float Dist = Vector3.Distance(transform.position, player.transform.position);

        if (Physics.Raycast(player.transform.position, Dir, out RaycastHit hit, Dist))
        {
            if (hit.collider.gameObject == this.gameObject)
            {
                return true;

            }
            return false;

        }
        return false;
    }

    public virtual float CalculateDamageAmount()
    {
        float DamageAmount = 1;




        return DamageAmount;
    }
}

public enum EnemyStates
{
    Explore_Idle,
    Explore_Patrol,
    Alert_Chase,
    Combat_Attacking,
    Combat_Retreat,
    Alert_Search,
    Alert_LookAround
}

public enum SuperStates
{
    Explore,
    Alert,
    Combat,
}

public enum DogEnemyStates
{
    Idle,
    FollowOwner,
    Wander,
    FollowTrail,
    Charge,
    Attack,

}

public enum DogSuperStates
{
    Passive,
    Tracking,
    Combat,
}
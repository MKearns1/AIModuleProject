using UnityEngine;

public class EnemyBulletScript : MonoBehaviour
{
    Rigidbody rb;
    float BulletSpeed = 20;
    public float BulletDamage = 1;
    GameObject owner;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize(Vector3 Direction, GameObject owner, float DamageAmount)
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Direction*BulletSpeed;
        BulletDamage = DamageAmount;
        this.owner = owner;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == owner) return;
        if(other.gameObject.tag == "Player")
        {
            Player player = other.gameObject.GetComponent<Player>();
            player.TakeDamage((int)BulletDamage);

        }
        Destroy(gameObject);

    }
}

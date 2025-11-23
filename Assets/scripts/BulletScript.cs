using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class BulletScript : MonoBehaviour
{
    Rigidbody rb;
    float BulletSpeed = 20;
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

    public void Initialize(Vector3 Direction, GameObject owner)
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Direction*BulletSpeed;
        this.owner = owner;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == owner) return;

        if (other.gameObject.tag == "Enemy")
        {
            EnemyBase enemy = other.gameObject.GetComponent<EnemyBase>();
            enemy.TakeDamage(1);

        }
        Destroy(gameObject);

    }
}

using UnityEngine;

public class BulletScript : MonoBehaviour
{
    Rigidbody rb;
    float BulletSpeed = 20;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize(Vector3 Direction)
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Direction*BulletSpeed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Enemy")
        {
            Enemy1 enemy = other.gameObject.GetComponent<Enemy1>();
            enemy.TakeDamage(1);

            Destroy(gameObject);
        }
    }
}

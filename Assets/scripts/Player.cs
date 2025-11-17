using TMPro;
using Unity.AI.Navigation;
using UnityEngine;

public class Player : MonoBehaviour
{
    Rigidbody rb;
    float movementSpeed = 10;
    int Health = 5;
    float moveAmount;
    Tiles tilescript;
    Vector3 MoveToPos;
    Vector3 PrevPos;
    public GameObject BulletPrefab;
    Transform BulletSpawnPos;
    TextMeshPro HealthText;
    NavMeshSurface navMesh;
    TileMovement TileMover;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        tilescript = GameObject.Find("Tiles").GetComponent<Tiles>();
        moveAmount = tilescript.Scale;
        BulletSpawnPos = transform.Find("BulletSpawnPos");
        HealthText = transform.Find("HealthText").GetComponent<TextMeshPro>();
        navMesh = GameObject.FindGameObjectWithTag("NavMesh").gameObject.GetComponent<NavMeshSurface>();
        TileMover = GetComponent<TileMovement>();

    }

    // Update is called once per frame
    void Update()
    {
        var horizontalInput = Input.GetAxis("Horizontal");
        var verticalInput = Input.GetAxis("Vertical");

        //if (verticalInput > 0)
        //{
        //    TileMovement(Vector3.forward);
        //}
        //else if (verticalInput < 0)
        //{
        //    TileMovement(Vector3.back);

        //}
        //else if(horizontalInput > 0)
        //{
        //    TileMovement(Vector3.right);

        //}
        //else if (horizontalInput < 0)
        //{
        //    TileMovement(Vector3.left);

        //}

        CheckInputs();

        


        HealthText.transform.rotation = Camera.main.transform.rotation;
        HealthText.text = Health.ToString();

        //Move();
    }

    void Move()
    {
        if (Input.GetAxisRaw("Horizontal") == 0 && Input.GetAxisRaw("Vertical") == 0)
            return;
        var horizontalInput = Input.GetAxis("Horizontal");
        var verticalInput = Input.GetAxis("Vertical");

        var rotation = Quaternion.LookRotation(new Vector3(horizontalInput, 0, verticalInput));

        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * 20);
        //transform.rotation = rotation;

        Vector3 movementDir = transform.forward * Time.deltaTime * movementSpeed;
        rb.MovePosition(rb.position + movementDir);
    }

   

    void CheckInputs()
    {
        if (!TileMover.moving)
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                TileMover.TileMove(Vector3.forward * moveAmount);
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                TileMover.TileMove(Vector3.back * moveAmount);

            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                TileMover.TileMove(Vector3.right * moveAmount);

            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                TileMover.TileMove(Vector3.left * moveAmount);

            }

            if(Input.GetKeyDown(KeyCode.Space))
            {
                Shoot();
            }
        }
    }

    void Shoot()
    {
        GameObject newBullet = Instantiate(BulletPrefab, BulletSpawnPos.position, Quaternion.identity);

        BulletScript bullet = newBullet.GetComponent<BulletScript>();

        bullet.Initialize(transform.forward);
        newBullet.transform.rotation = transform.rotation;
    }

    public void TakeDamage(int amount)
    {
        Health-=amount;
        if(Health <= 0)
        {
            Death();
        }
    }

    public void Death()
    {
        gameObject.SetActive(false);
    }

    public void MoveFinished()
    {
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(MoveToPos, .1f);
    }
}

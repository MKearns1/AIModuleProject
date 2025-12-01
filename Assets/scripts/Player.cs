using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    Rigidbody rb;
    float movementSpeed = 10;
    int Health = 20;
    float moveAmount;
    Tiles tilescript;
    Vector3 MoveToPos;
    Vector3 PrevPos;
    public GameObject BulletPrefab;
    Transform BulletSpawnPos;
    TextMeshPro HealthText;
    NavMeshSurface navMesh;
    TileMovement TileMover;
    CapsuleCollider capsuleCollider;

    Vector3 CamOffset;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        tilescript = GameObject.FindAnyObjectByType<Tiles>();
       // tilescript = GameObject.Find("Terrain").GetComponent<Tiles>();
        moveAmount = tilescript.Scale;
        BulletSpawnPos = transform.Find("BulletSpawnPos");
        HealthText = transform.Find("HealthText").GetComponent<TextMeshPro>();
        //navMesh = GameObject.FindGameObjectWithTag("NavMesh").gameObject.GetComponent<NavMeshSurface>();
        TileMover = GetComponent<TileMovement>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        CamOffset = Camera.main.transform.position - transform.position;

    }

    // Update is called once per frame
    void Update()
    {
        if (tilescript.NodesGrid == null) return;

        Vector3 CamPos = (transform.position / 1) + CamOffset;
        CamPos.y = Camera.main.transform.position.y;
        Camera.main.transform.position = CamPos;

        CheckInputs();

        tilescript.GetNodeFromWorldPosition(transform.position).PlayerScentStrength = 1;


        HealthText.transform.rotation = Camera.main.transform.rotation;
        HealthText.text = "Health: " + Health.ToString();

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
        if (TileMover.moving)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Shoot();
        }

        var horizontalInput = Input.GetAxis("Horizontal");
        var verticalInput = Input.GetAxis("Vertical");

        //if (horizontalInput > 0) horizontalInput = 1;
        //if (horizontalInput < 0) horizontalInput = -1;
        //if (verticalInput > 0) verticalInput = 1;
        //if (verticalInput < 0) verticalInput = -1;

        if (Input.GetKey(KeyCode.D)) horizontalInput = 1;
        if (Input.GetKey(KeyCode.A)) horizontalInput = -1;
        if (Input.GetKey(KeyCode.W)) verticalInput = 1;
        if (Input.GetKey(KeyCode.S)) verticalInput = -1;

        Vector2Int MoveDir = new Vector2Int((int)horizontalInput,(int)verticalInput);


        if (MoveDir == Vector2Int.zero)
            return;


        Node CurrentNode = tilescript.GetNodeFromWorldPosition(transform.position);
        int CurNodeX = (int)CurrentNode.GridPos.x;
        int CurNodeY = (int)CurrentNode.GridPos.y;
        Node NextNode;

        int NextX = CurNodeX + MoveDir.x;
        int NextY = CurNodeY + MoveDir.y;

        if (NextX < 0 || NextY < 0 || NextX >= tilescript.GridSize || NextY >= tilescript.GridSize)
            return;


        NextNode = tilescript.NodesGrid[NextX, NextY];

        if (NextNode.nodeTyoe == NodeType.Untraversable || NextNode.occupied)
            return;


        Vector3 TargetPos = NextNode.worldPos + Vector3.up * capsuleCollider.height / 2;


        tilescript.NodesGrid[NextX,NextY].SetOccupied(true, gameObject);
        tilescript.NodesGrid[CurNodeX,CurNodeY].SetOccupied(false,null);
        TileMover.MoveToPoint(TargetPos);
        
    }

    void Shoot()
    {
        GameObject newBullet = Instantiate(BulletPrefab, BulletSpawnPos.position, Quaternion.identity);

        BulletScript bullet = newBullet.GetComponent<BulletScript>();

        bullet.Initialize(transform.forward, gameObject);
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

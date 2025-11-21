using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class TileMovement : MonoBehaviour
{

    public Vector3 PrevPos;
    public Vector3 MoveToPos;
    public bool moving;
    public float moveProgress;
    public float movementSpeed;

    public UnityEvent TileMoveFinished = new UnityEvent();

    CapsuleCollider Capsulecollider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Capsulecollider = GetComponent<CapsuleCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        if (moving)
        {
            MoveTransition(PrevPos, MoveToPos, moveProgress);

            float Dist = Vector3.Distance(transform.position, MoveToPos);
            if (Dist < .1f)
            {
                transform.position = MoveToPos;
                moving = false;
                moveProgress = 0;
                // navMesh.BuildNavMesh();
                MoveFinished();
               TileMoveFinished.Invoke();
            }
        }
    }

    public void MoveInDirection(Vector3 Direction)
    {
        PrevPos = transform.position;
        MoveToPos = PrevPos + Direction;
        var rotation = Quaternion.LookRotation(Direction);
        transform.rotation = Quaternion.Lerp(transform.rotation,rotation,Time.deltaTime *1);

        moving = true;
    }

    public void MoveToPoint(Vector3 Point)
    {
        PrevPos = transform.position;
        MoveToPos = Point;
       // MoveToPos = Point + Vector3.up*Capsulecollider.height/2;

        Vector3 Direction = new Vector3(MoveToPos.x,0,MoveToPos.z) - new Vector3(PrevPos.x,0,PrevPos.z);
        Direction = Vector3.Normalize(Direction);

        var rotation = Quaternion.LookRotation(Direction);

        moving = true;
    }

    public void MoveTransition(Vector3 startPos, Vector3 EndPos, float amount)
    {
        moveProgress += Time.deltaTime * movementSpeed;
        float t = Mathf.Clamp01(moveProgress);

        Vector3 flatPos = Vector3.Lerp(startPos, EndPos, t);


        float Fulldist = Vector3.Distance(PrevPos, EndPos);
        float curdist = Vector3.Distance(transform.position, EndPos);

        float maxJumpHeight = 1;
        float height = 4 * maxJumpHeight * t * (1 - t);
        flatPos.y += height;
        transform.position = flatPos;

        Vector3 Direction = new Vector3(MoveToPos.x, 0, MoveToPos.z) - new Vector3(PrevPos.x, 0, PrevPos.z);
        Direction = Vector3.Normalize(Direction);

        var rotation = Quaternion.LookRotation(Direction);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, moveProgress);
    }

    public void MoveFinished()
    {
        moving = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(MoveToPos, .1f);
    }
}

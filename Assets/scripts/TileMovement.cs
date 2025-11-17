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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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

    public void TileMove(Vector3 Direction)
    {
        PrevPos = transform.position;
        MoveToPos = PrevPos + Direction;
        var rotation = Quaternion.LookRotation(Direction);
        transform.rotation = rotation;

        moving = true;
    }

    public void MoveTransition(Vector3 startPos, Vector3 EndPos, float amount)
    {
        moveProgress += Time.deltaTime * movementSpeed;
        transform.position = Vector3.Lerp(PrevPos, EndPos, moveProgress);
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

using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class Vision : MonoBehaviour
{
    [Header("Vision Settings")]
    public float viewDistance = 20f;
    public float viewAngle = 120;
    public LayerMask obstacleMask;
    public Transform VisionStartPoint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool CanSeeGameObject(GameObject target)
    {
        Transform eyes = VisionStartPoint;

        Vector3 targetPos = target.transform.position + Vector3.up * 1.0f;
        Vector3 eyePos = eyes.position + Vector3.up * 0.2f;

        Vector3 direction = (targetPos - eyePos).normalized;

        Vector3 flatDir = targetPos - eyePos;
        flatDir.y = 0;
        if (Vector3.Angle(eyes.forward, flatDir) > viewAngle / 2f)
            return false;

        float dist = Vector3.Distance(eyePos, targetPos);
        if (dist > viewDistance)
            return false;

        if (Physics.Raycast(eyePos, direction, out RaycastHit hit, viewDistance))
        {
            if (hit.collider.gameObject != target)
            {
                Debug.Log(hit.collider.gameObject);

                return false;
            }
            else
            {
               // Debug.Log(hit.collider.gameObject);

                return true;
            }
        }


        return false;
    }


    public Node GetFurthestVisibleNode()
    {
        Tiles tilescript = GameObject.Find("Tiles").GetComponent<Tiles>();

        int xRange = 10;
        int yRange = 10;

        Node furthestNode = null;
        float FurthestPoint = 0f;

        Node myNode = tilescript.GetNodeFromWorldPosition(transform.position);

        for (int dx = -xRange; dx <= xRange; dx++)
        {
            for (int dy = -yRange; dy <= yRange; dy++)
            {
                int x = myNode.GridPos.x + dx;
                int y = myNode.GridPos.y + dy;

                // Correct bounds check
                if (x < 0 || y < 0 || x >= tilescript.GridSize || y >= tilescript.GridSize)
                    continue;

                Node target = tilescript.NodesGrid[x, y];

                if (target.nodeTyoe == NodeType.Untraversable)
                    continue;

                float curDist = Vector3.Distance(target.worldPos, transform.position);

                // Path must exist
                //List<Node> PathToNode = GetComponent<AStarPathfinding>().GetPath(transform.position, target.worldPos);
                //if (PathToNode.Count == 0)
                //    continue;

                // Visibility check
                Vector3 dir = (target.worldPos - VisionStartPoint.position).normalized;
                float dist = Vector3.Distance(VisionStartPoint.position, target.worldPos);

                if (Physics.Raycast(VisionStartPoint.position, dir, dist))
                    continue;

                // Pick furthest visible node
                if (curDist > FurthestPoint)
                {
                    FurthestPoint = curDist;
                    furthestNode = target;
                }
            }
        }

        return furthestNode;
    }


    public int CountCoverLayers(Vector3 target)
    {
        int layers = 0;
        Vector3 dir = (VisionStartPoint.position - target).normalized;
        float dist = Vector3.Distance(VisionStartPoint.position, target);

        if (Physics.Raycast(VisionStartPoint.position, dir, out RaycastHit hit, dist))
        {
            layers++;
        }
        return layers;
    }



    void OnDrawGizmos()
    {
        if (VisionStartPoint == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(VisionStartPoint.position, viewDistance);

        Vector3 leftDir = Quaternion.Euler(0, -viewAngle / 2f, 0) * VisionStartPoint.forward;
        Vector3 rightDir = Quaternion.Euler(0, viewAngle / 2f, 0) * VisionStartPoint.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(VisionStartPoint.position, VisionStartPoint.position + leftDir * viewDistance);
        Gizmos.DrawLine(VisionStartPoint.position, VisionStartPoint.position + rightDir * viewDistance);
    }
}

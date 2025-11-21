using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.XR;

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

        Vector3 Direction = (target.transform.position - VisionStartPoint.position).normalized;

        if (Vector3.Angle(VisionStartPoint.forward, Direction) > viewAngle / 2f)
            return false;

        float dist = Vector3.Distance(VisionStartPoint.position, target.transform.position);
        if (dist > viewDistance)
            return false;

        if (Physics.Raycast(VisionStartPoint.position, Direction, out RaycastHit hit, viewDistance, obstacleMask))
        {
            return false;

        }
        if (Physics.Raycast(VisionStartPoint.position, Direction, out RaycastHit hit1, viewDistance))
        {
            if (hit1.collider.gameObject == target)
                return true;
        }

            return false;
    }

    void OnDrawGizmos()
    {
        if (VisionStartPoint == null)
            return;
        // View distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(VisionStartPoint.position, viewDistance);

        // Cone lines
        Vector3 leftDir = Quaternion.Euler(0, -viewAngle / 2f, 0) * VisionStartPoint.forward;
        Vector3 rightDir = Quaternion.Euler(0, viewAngle / 2f, 0) * VisionStartPoint.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(VisionStartPoint.position, VisionStartPoint.position + leftDir * viewDistance);
        Gizmos.DrawLine(VisionStartPoint.position, VisionStartPoint.position + rightDir * viewDistance);
    }
}

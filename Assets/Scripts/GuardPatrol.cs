using UnityEngine;
using UnityEngine.AI;

public class GuardPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform[] waypoints; // Waypoint 배열
    public float patrolSpeed = 3.5f;
    public float waypointReachedDistance = 1.0f; // Waypoint 도착 판정 거리
    public float waitTime = 2.0f; // Waypoint에서 대기 시간
    
    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = patrolSpeed;
        
        if (waypoints.Length == 0)
        {
            Debug.LogError("Waypoints가 설정되지 않았습니다!");
            enabled = false;
            return;
        }
        
        // 첫 번째 Waypoint로 이동
        SetDestination();
    }

    void Update()
    {
        // 대기 중이면
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                // 다음 Waypoint로
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                SetDestination();
            }
            return;
        }
        
        // Waypoint 도착 확인
        if (agent.remainingDistance <= waypointReachedDistance && !agent.pathPending)
        {
            // 도착하면 대기
            isWaiting = true;
            waitTimer = waitTime;
        }
    }

    void SetDestination()
    {
        if (waypoints.Length > 0)
        {
            agent.destination = waypoints[currentWaypointIndex].position;
        }
    }

    // Scene 뷰에서 경로 시각화
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        
        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                // Waypoint 그리기
                Gizmos.DrawWireSphere(waypoints[i].position, 0.5f);
                
                // 다음 Waypoint로 선 그리기
                int nextIndex = (i + 1) % waypoints.Length;
                if (waypoints[nextIndex] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[nextIndex].position);
                }
            }
        }
    }
}

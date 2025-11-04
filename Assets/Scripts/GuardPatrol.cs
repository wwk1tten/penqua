using UnityEngine;
using UnityEngine.AI;

public class GuardPatrol : MonoBehaviour
{
    // 상태 정의
    public enum GuardState
    {
        Patrol,
        Alert
    }

    [Header("State")]
    public GuardState currentState = GuardState.Patrol;
    public float alertTimeout = 5f;

    [Header("Patrol Settings")]
    public Transform[] waypoints;
    public float patrolSpeed = 3.5f;
    public float waypointReachedDistance = 1.0f;
    public float waitTime = 2.0f;

    [Header("Alert Settings")]
    public float alertSpeed = 2f;
    
    [Header("UI")]
    public GameObject alertUI; // "!" 느낌표
    
    private NavMeshAgent agent;
    private AISensor sensor;
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private float alertTimer = 0f;
    private Transform playerTarget = null;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        sensor = GetComponent<AISensor>();
        agent.speed = patrolSpeed;
        
        if (alertUI != null)
        {
            alertUI.SetActive(false);
        }
        
        if (waypoints.Length == 0)
        {
            Debug.LogError("Waypoints가 설정되지 않았습니다!");
            enabled = false;
            return;
        }
        
        SetDestination();
    }

    void Update()
    {
        // 플레이어 감지 확인
        DetectPlayer();
        
        // 상태에 따른 행동
        switch (currentState)
        {
            case GuardState.Patrol:
                UpdatePatrol();
                break;
            
            case GuardState.Alert:
                UpdateAlert();
                break;
        }
    }

    /// <summary>
    /// 플레이어 감지 및 상태 전환
    /// </summary>
    void DetectPlayer()
    {
        if (sensor == null) return;
        
        // 감지된 객체 확인
        if (sensor.Objects.Count > 0)
        {
            foreach (var obj in sensor.Objects)
            {
                if (obj != null && obj.CompareTag("Player"))
                {
                    playerTarget = obj.transform;
                    
                    // Alert 상태로 전환
                    if (currentState != GuardState.Alert)
                    {
                        ChangeState(GuardState.Alert);
                    }
                    
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 순찰 상태 업데이트
    /// </summary>
    void UpdatePatrol()
    {
        // UI 비활성화
        if (alertUI != null)
        {
            alertUI.SetActive(false);
        }

        // 대기 중이면
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                SetDestination();
                Debug.Log($"다음 Waypoint로 이동: {currentWaypointIndex}");
            }
            return;
        }
        
        // Waypoint 도착 확인
        if (agent.remainingDistance <= waypointReachedDistance && !agent.pathPending)
        {
            isWaiting = true;
            waitTimer = waitTime;
            Debug.Log($"Waypoint {currentWaypointIndex} 도착, {waitTime}초 대기");
        }
    }

    /// <summary>
    /// 경보 상태 업데이트
    /// </summary>
    void UpdateAlert()
    {
        // UI 활성화
        if (alertUI != null)
        {
            alertUI.SetActive(true);
        }

        // 플레이어를 향해 이동
        if (playerTarget != null)
        {
            agent.destination = playerTarget.position;
        }

        // Alert 타이머
        alertTimer -= Time.deltaTime;
        
        if (alertTimer <= 0)
        {
            Debug.Log("Alert 타임아웃, Patrol로 복귀");
            ChangeState(GuardState.Patrol);
        }
    }

    /// <summary>
    /// 상태 변경
    /// </summary>
    void ChangeState(GuardState newState)
    {
        if (currentState == newState) return;

        Debug.Log($"[{gameObject.name}] 상태 변경: {currentState} → {newState}");
        currentState = newState;

        switch (newState)
        {
            case GuardState.Patrol:
                agent.speed = patrolSpeed;
                isWaiting = false;
                SetDestination();
                break;

            case GuardState.Alert:
                agent.speed = alertSpeed;
                alertTimer = alertTimeout;
                isWaiting = false;
                break;
        }
    }

    void SetDestination()
    {
        if (waypoints.Length > 0)
        {
            agent.destination = waypoints[currentWaypointIndex].position;
        }
    }

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        
        // 상태에 따른 색상
        Gizmos.color = (currentState == GuardState.Alert) ? Color.red : Color.yellow;
        
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawWireSphere(waypoints[i].position, 0.5f);
                
                int nextIndex = (i + 1) % waypoints.Length;
                if (waypoints[nextIndex] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[nextIndex].position);
                }
            }
        }
        
        // Guard 위치 (상태에 따른 색상)
        Gizmos.color = (currentState == GuardState.Alert) ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}

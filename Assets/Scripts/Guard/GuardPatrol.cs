using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GuardPatrol : MonoBehaviour
{
    // 상태 정의
    public enum GuardState
    {
        Patrol,
        Suspicious,
        Alert,
        Chase,
        Return

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
    private Animator animator;

    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private float alertTimer = 0f;
    private Transform playerTarget = null;

    [Header("Detection")]
    public float visionRange = 15f;
    public float visionAngle = 90f;
    public float hearingRange = 10f;
    
    [Header("Chase")]
    public float chaseSpeed = 5f;
    [Header("Wetness")]
    public float maxWetness = 100f;
    public float stunWetness = 80f;
    private float currentWetness = 0f;
    public float wetnessDecayRate = 5f; // 초당 회복 속도
    private List<Material> wetnessMaterials = new List<Material>();
    private Material guardMaterial;
    [Header("Stun")]
    public float puddleSpeedMultiplier = 0.5f; // 50% 속도로 감소
    public float fallStunDuration = 1.5f; // 넘어졌을 때 스턴 시간
    private bool isInPuddle = false;
    private bool isFalling = false; // 넘어지는 중

    // 기본 속도 저장
    private float basePatrolSpeed;
    private float baseAlertSpeed;
    private float baseChaseSpeed;

    private Vector3 lastKnownPosition;
    private Vector3 originalPosition;
    // 애니메이터 파라미터
    private int _animIDFall;
    private int _animIDHit;

    void Start(){
        agent = GetComponent<NavMeshAgent>();
        sensor = GetComponent<AISensor>();
        animator = GetComponent<Animator>();

        // 기본 속도 저장
        basePatrolSpeed = patrolSpeed;
        baseAlertSpeed = alertSpeed;
        basePatrolSpeed = chaseSpeed;

        // 애니메이터 파라미터 캐싱
        _animIDFall = Animator.StringToHash("Fall");
        _animIDHit = Animator.StringToHash("Hit");
        
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

        SkinnedMeshRenderer[] allRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        
        if (allRenderers.Length > 0)
        {
            foreach (var renderer in allRenderers)
            {
                // 각 렌더러의 머티리얼에 대한 인스턴스를 생성하고 리스트에 추가
                // 이렇게 하면 원본 머티리얼 에셋은 건드리지 않게 됨
                wetnessMaterials.Add(renderer.material); 
            }
        }
        else
        {
            Debug.LogError($"[{gameObject.name}]에서 SkinnedMeshRenderer를 찾을 수 없습니다!");
        }

        SetDestination();
    }

    void Update(){
        // 넘어지는 중이면 아무것도 안 함
        if (isFalling) return;

        // 젖음 회복 (시간이 지나면서 감소)
        if (currentWetness > 0)
        {
            currentWetness -= wetnessDecayRate * Time.deltaTime;
            currentWetness = Mathf.Max(0, currentWetness);
        }
        
        // 젖음에 따라 속도, 시각화 조정
        UpdateWetnessEffect();
        UpdateSpeedByWetness();
        // 플레이어 감지 확인
        DetectPlayer();
        
        // 상태에 따른 행동
        switch (currentState)
        {
            case GuardState.Patrol:
                UpdatePatrol();
                break;
            
            case GuardState.Suspicious:
                UpdateSuspicious();
                break;
            
            case GuardState.Alert:
                UpdateAlert();
                break;
                
            case GuardState.Chase:
                UpdateChase();
                break;
                
            case GuardState.Return:
                UpdateReturn();
                break;
        }

        UpdateAnimator();
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
    void UpdatePatrol(){
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
            }
            return;
        }
        
        // Waypoint 도착 확인
        if (agent.remainingDistance <= waypointReachedDistance && !agent.pathPending)
        {
            isWaiting = true;
            waitTimer = waitTime;
        }
    }

    /// <summary>
    /// 경보 상태 업데이트
    /// </summary>
    void UpdateAlert(){
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
        
        if (alertTimer <= 0.5)
        {
            ChangeState(GuardState.Chase);
        }
    }

    /// <summary>
    /// 의심 상태 업데이트 (소리 발생 지점으로 이동)
    /// </summary>
    void UpdateSuspicious(){
        // UI 활성화
        if (alertUI != null)
        {
            alertUI.SetActive(true);
        }
        
        // 목적지 도착 확인
        if (!agent.pathPending && agent.remainingDistance <= waypointReachedDistance)
        {
            ChangeState(GuardState.Return);
        }
    }

    /// <summary>
    /// 추격 상태 업데이트
    /// </summary>
    void UpdateChase(){
        // UI 활성화
        if (alertUI != null)
        {
            alertUI.SetActive(true);
        }
        
        // 플레이어를 향해 이동
        if (playerTarget != null)
        {
            agent.destination = playerTarget.position;
            lastKnownPosition = playerTarget.position;
        }
        
        // 플레이어를 놓쳤는지 확인
        if (sensor != null && sensor.Objects.Count == 0)
        {
            ChangeState(GuardState.Return);
        }
    }

    /// <summary>
    /// 복귀 상태 업데이트
    /// </summary>
    void UpdateReturn()
    {
        // UI 비활성화
        if (alertUI != null)
        {
            alertUI.SetActive(false);
        }
        
        // 원래 순찰 경로로 복귀
        if (!agent.pathPending && agent.remainingDistance <= waypointReachedDistance)
        {
            ChangeState(GuardState.Patrol);
        }
    }


    /// <summary>
    /// 애니메이터 파라미터 업데이트
    /// </summary>
    void UpdateAnimator()
    {
        if (animator == null) return;
        // Patrol이면 false, Alert이면 true
        animator.SetBool("isAlert", currentState == GuardState.Alert);
    }


    /// <summary>
    /// 상태 변경
    /// </summary>
    void ChangeState(GuardState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (newState)
        {
            case GuardState.Patrol:
                agent.speed = basePatrolSpeed * (1f - (currentWetness / maxWetness));
                isWaiting = false;
                SetDestination();
                
                if (animator != null)
                {
                    animator.SetBool("isAlert", false);
                }
                break;

            case GuardState.Suspicious:
                agent.speed = basePatrolSpeed * (1f - (currentWetness / maxWetness));
                isWaiting = false;
                
                if (animator != null)
                {
                    animator.SetBool("isAlert", true);
                }
                break;

            case GuardState.Alert:
                agent.speed = baseAlertSpeed * (1f - (currentWetness / maxWetness));
                alertTimer = alertTimeout;
                isWaiting = false;

                if (animator != null)
                {
                    animator.SetBool("isAlert", true);
                }
                break;
                
            case GuardState.Chase:
                agent.speed = chaseSpeed * (1f - (currentWetness / maxWetness));
                isWaiting = false;
                
                if (animator != null)
                {
                    animator.SetBool("isAlert", true);
                }
                break;
                
            case GuardState.Return:
                agent.speed = basePatrolSpeed * (1f - (currentWetness / maxWetness));
                isWaiting = false;
                
                int closestWaypointIndex = FindClosestWaypoint();
                currentWaypointIndex = closestWaypointIndex;
                agent.SetDestination(waypoints[currentWaypointIndex].position);
                
                if (animator != null)
                {
                    animator.SetBool("isAlert", false);
                }
                break;
        }
    }


    /// <summary>
    /// 가장 가까운 waypoint 찾기
    /// </summary>
    int FindClosestWaypoint()
    {
        int closestIndex = 0;
        float closestDistance = float.MaxValue;
        
        for (int i = 0; i < waypoints.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, waypoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        
        return closestIndex;
    }

    void SetDestination()
    {
        if (waypoints.Length > 0)
        {
            agent.destination = waypoints[currentWaypointIndex].position;
        }
    }


    public void OnSoundHeard(Vector3 soundPosition)
    {
        float distance = Vector3.Distance(transform.position, soundPosition);
        
        // Patrol 상태일 때만 소리에 반응
        if (distance <= hearingRange && currentState == GuardState.Patrol)
        {
            lastKnownPosition = soundPosition;
            agent.SetDestination(soundPosition);
            ChangeState(GuardState.Suspicious);
        }
    }


    /// <summary>
    /// 젖음 수치에 따라 속도 감소
    /// </summary>
    void UpdateSpeedByWetness()
    {
        // 젖음에 따른 속도 감소 비율 계산 (0 ~ 1)
        float speedMultiplier = 1f - (currentWetness / maxWetness);
        speedMultiplier = Mathf.Clamp01(speedMultiplier);
        
        // 현재 상태에 맞는 속도 적용
        switch (currentState)
        {
            case GuardState.Patrol:
            case GuardState.Suspicious:
            case GuardState.Return:
                agent.speed = basePatrolSpeed * speedMultiplier;
                break;
            
            case GuardState.Alert:
                agent.speed = baseAlertSpeed * speedMultiplier;
                break;
            
            case GuardState.Chase:
                agent.speed = baseChaseSpeed * speedMultiplier;
                break;
        }
    }
    void UpdateWetnessEffect()
    {
        if (wetnessMaterials.Count == 0) return;
        
        float wetnessRatio = currentWetness / maxWetness;
        
        // 리스트에 있는 모든 머티리얼의 _Wetness 값을 업데이트
        foreach (var mat in wetnessMaterials)
        {
            mat.SetFloat("_Wetness", wetnessRatio);
        }
    }


    /// <summary>
    /// 물총에 맞음 (외부에서 호출됨)
    /// </summary>
    public void TakeWaterDamage(float damage, Vector3 hitPoint){
        if (animator != null)
        {
            animator.SetTrigger(_animIDHit);
        }
        currentWetness = Mathf.Min(currentWetness + damage, maxWetness);
    
        // 필요시 경비원을 잠시 혼란 상태로 만들기
        if (currentWetness >= maxWetness)
        {
            Debug.Log($"[{gameObject.name}] 완전히 젖었습니다!");
            // 상태를 Suspicious로 전환하거나 시간 증가 등의 패널티 추가 가능
        }
    }

    /// <summary>
    /// 젖음 정보 조회
    /// </summary>
    public float GetWetnessPercent()
    {
        return (currentWetness / maxWetness) * 100f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WaterPuddle") && !isInPuddle)
        {
            isInPuddle = true;
            currentWetness = stunWetness;
            
            // 넘어지는 애니메이션 + 스턴
            if (!isFalling)
            {
                StartCoroutine(FallInPuddleCoroutine());
            }
        }
        else if (other.CompareTag("Player"))
        {
            // 인터페이스를 통해 안전하게 접근
            IDamageable damageable = other.GetComponent<IDamageable>();
            
            if (damageable != null)
            {
                // 데미지 1을 주고, 내 위치(transform.position)를 전달하여 넉백 방향 계산
                damageable.TakeDamage(1, transform.position);
            }
        }
    }
    private void OnTriggerStay(Collider other)
    {
        // 웅덩이 안에 계속 있으면 젖음 100 유지
        if (other.CompareTag("WaterPuddle"))
        {
            currentWetness = stunWetness;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("WaterPuddle"))
        {
            isInPuddle = false;
        }
    }
    
    /// <summary>
    /// 넘어지는 코루틴
    /// </summary>
    private IEnumerator FallInPuddleCoroutine()
    {
        isFalling = true;
        agent.isStopped = true; // NavMesh 정지
        
        // 넘어지는 애니메이션 트리거
        if (animator != null)
        {
            animator.SetTrigger(_animIDFall);
        }

 
        // 넘어져 있는 시간
        yield return new WaitForSeconds(fallStunDuration);

        isFalling = false;
        agent.isStopped = false;

    }
    void OnDrawGizmos(){
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GuardPatrol : MonoBehaviour
{
    // =========================================================
    // 1. 설정 및 변수 (기존 유지)
    // =========================================================
    public enum GuardState { Patrol, Suspicious, Alert, Chase, Return }

    [Header("State")]
    public GuardState currentState = GuardState.Patrol;
    public float alertTimeout = 5f;

    [Header("Patrol Settings")]
    public Transform[] waypoints;
    public float patrolSpeed = 3.5f;
    public float chaseSpeed = 5f; // [수정] 오타 방지를 위해 순서 정리
    public float waypointReachedDistance = 1.0f;
    public float waitTime = 2.0f;

    [Header("Alert Settings")]
    public float alertSpeed = 2f;
    public GameObject alertUI; 

    [Header("Detection")]
    public float visionRange = 15f; // Gizmo용
    public float hearingRange = 10f;
    
    [Header("Attack")]
    public float attackRange = 1.5f; 
    public int attackDamage = 1;   
    public float attackCooldown = 1.5f; 
    public float knockbackForce = 5f;
    
    [Header("Wetness & Debuff")]
    public float maxWetness = 100f;
    public float stunWetness = 80f;
    public float wetnessDecayRate = 5f;
    public float puddleSpeedMultiplier = 0.5f;
    public float fallStunDuration = 1.5f;

    // 내부 변수
    private NavMeshAgent agent;
    private AISensor sensor;
    private Animator animator;
    private Transform playerTarget = null;
    
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private float alertTimer = 0f;
    
    private float lastAttackTime = -999f;
    private float currentWetness = 0f;
    private bool isInPuddle = false;
    private bool isFalling = false;
    
    private List<Material> wetnessMaterials = new List<Material>();
    private float basePatrolSpeed, baseAlertSpeed, baseChaseSpeed;
    private Vector3 lastKnownPosition;

    // 애니메이션 해시 (최적화)
    private int _animIDFall;
    private int _animIDHit;
    private int _animIDAlert;
    private int _animIDAttack;

    // =========================================================
    // 2. 초기화 (Start)
    // =========================================================
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        sensor = GetComponent<AISensor>();
        animator = GetComponent<Animator>();

        // 속도 백업
        basePatrolSpeed = patrolSpeed;
        baseAlertSpeed = alertSpeed;
        baseChaseSpeed = chaseSpeed; // [중요] 기존 코드 오타 수정됨

        // 애니메이션 ID 캐싱
        _animIDFall = Animator.StringToHash("Fall");
        _animIDHit = Animator.StringToHash("Hit");
        _animIDAlert = Animator.StringToHash("isAlert");
        _animIDAttack = Animator.StringToHash("Attack");

        agent.speed = patrolSpeed;
        if (alertUI != null) alertUI.SetActive(false);

        // 플레이어 찾기
        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTarget = playerObj.transform;
        }

        // 머티리얼 캐싱
        SkinnedMeshRenderer[] allRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var renderer in allRenderers)
        {
            wetnessMaterials.Add(renderer.material);
        }

        SetDestination();
    }

    // =========================================================
    // 3. Update (여기가 핵심 리팩토링)
    // =========================================================
    void Update()
    {
        if (playerTarget == null) return;
        if (isFalling) return; // 넘어져 있으면 아무것도 안 함

        // 1. 젖음 회복 및 효과 처리
        HandleWetnessRecovery();
        UpdateWetnessEffect();
        UpdateSpeedByWetness();

        // 2. 플레이어 감지 (센서)
        DetectPlayer();

        // 3. 공격 체크 (상태와 무관하게 사거리+시야 되면 공격)
        CheckAndAttack();

        // 4. 상태 머신 실행
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

    // =========================================================
    // 4. 기능별 분리 함수들 (깔끔하게 정리됨)
    // =========================================================

    // [핵심] 공격 가능 여부 체크 및 실행
    void CheckAndAttack()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // A. 사거리 체크
        if (distanceToPlayer <= attackRange)
        {
            // B. 쿨타임 체크
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                // C. [중요] 시야(Mesh) 체크 추가
                // 센서가 정상 작동 중이고, 플레이어가 센서 시야각(부채꼴) 안에 있을 때만 공격
                if (sensor != null && sensor.IsInSight(playerTarget.gameObject))
                {
                    AttackPlayer();
                }
            }
        }
    }

    void HandleWetnessRecovery()
    {
        if (currentWetness > 0)
        {
            currentWetness -= wetnessDecayRate * Time.deltaTime;
            currentWetness = Mathf.Max(0, currentWetness);
        }
    }

    void DetectPlayer()
    {
        if (sensor == null) return;
        
        // 센서가 감지한 물체들 중 플레이어가 있는지 확인
        if (sensor.Objects.Count > 0)
        {
            foreach (var obj in sensor.Objects)
            {
                if (obj != null && obj.CompareTag("Player"))
                {
                    playerTarget = obj.transform;
                    if (currentState != GuardState.Alert && currentState != GuardState.Chase)
                    {
                        ChangeState(GuardState.Alert);
                    }
                    return;
                }
            }
        }
    }

    // =========================================================
    // 5. 상태별 업데이트 (UpdateXXX)
    // =========================================================

    void UpdatePatrol()
    {
        if (alertUI != null) alertUI.SetActive(false);

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

        if (!agent.pathPending && agent.remainingDistance <= waypointReachedDistance)
        {
            isWaiting = true;
            waitTimer = waitTime;
        }
    }

    void UpdateAlert()
    {
        if (alertUI != null) alertUI.SetActive(true);
        if (playerTarget != null) agent.destination = playerTarget.position;

        alertTimer -= Time.deltaTime;
        if (alertTimer <= 0.5f) // 약간의 딜레이 후 추격
        {
            ChangeState(GuardState.Chase);
        }
    }

    void UpdateChase()
    {
        if (alertUI != null) alertUI.SetActive(true);

        if (playerTarget == null)
        {
            ChangeState(GuardState.Return);
            return;
        }

        float dist = Vector3.Distance(transform.position, playerTarget.position);

        // 사거리 진입 시: 이동 멈추고 회전만 함 (공격은 CheckAndAttack에서 처리)
        if (dist <= attackRange)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
            agent.updateRotation = false; // 수동 회전

            Vector3 dir = (playerTarget.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }
        }
        else // 사거리 밖: 추격 이동
        {
            agent.updateRotation = true;
            if (agent.isStopped) agent.isStopped = false;
            
            agent.SetDestination(playerTarget.position);
            lastKnownPosition = playerTarget.position;
        }

        // 시야에서 사라졌을 때
        if (sensor != null && sensor.Objects.Count == 0)
        {
            agent.updateRotation = true;
            agent.isStopped = false;
            ChangeState(GuardState.Return);
        }
    }

    void UpdateSuspicious()
    {
        if (alertUI != null) alertUI.SetActive(true);
        if (!agent.pathPending && agent.remainingDistance <= waypointReachedDistance)
        {
            ChangeState(GuardState.Return);
        }
    }

    void UpdateReturn()
    {
        if (alertUI != null) alertUI.SetActive(false);
        if (!agent.pathPending && agent.remainingDistance <= waypointReachedDistance)
        {
            ChangeState(GuardState.Patrol);
        }
    }

    // =========================================================
    // 6. 상태 전환 및 공통 기능 (수정 없음, 정리만 함)
    // =========================================================

    void ChangeState(GuardState newState)
    {
        if (currentState == newState) return;
        currentState = newState;

        // 상태 변경 시 공통 초기화
        agent.updateRotation = true;
        agent.isStopped = false;

        // 속도 적용 (젖음 고려는 UpdateSpeedByWetness에서 매 프레임 처리됨)
        float baseSpeed = patrolSpeed; 

        switch (newState)
        {
            case GuardState.Patrol:
                baseSpeed = basePatrolSpeed;
                isWaiting = false;
                SetDestination();
                if (animator) animator.SetBool(_animIDAlert, false);
                break;

            case GuardState.Suspicious:
                baseSpeed = basePatrolSpeed;
                isWaiting = false;
                if (animator) animator.SetBool(_animIDAlert, true);
                break;

            case GuardState.Alert:
                baseSpeed = baseAlertSpeed;
                alertTimer = alertTimeout;
                isWaiting = false;
                if (animator) animator.SetBool(_animIDAlert, true);
                break;

            case GuardState.Chase:
                baseSpeed = baseChaseSpeed;
                isWaiting = false;
                if (animator) animator.SetBool(_animIDAlert, true);
                break;

            case GuardState.Return:
                baseSpeed = basePatrolSpeed;
                isWaiting = false;
                MoveToClosestWaypoint();
                if (animator) animator.SetBool(_animIDAlert, false);
                break;
        }
    }

    // 공격 실행 함수
    private void AttackPlayer()
    {
        lastAttackTime = Time.time;
        if (animator != null) animator.SetTrigger(_animIDAttack);

        Vector3 dir = (playerTarget.position - transform.position).normalized;
        dir.y = 0;

        if (playerTarget.TryGetComponent<IDamageable>(out var dmg))
        {
            Debug.Log("공격 성공");
            dmg.TakeDamage(attackDamage, playerTarget.position, dir, knockbackForce);
        }
    }

    // =========================================================
    // 7. 유틸리티 및 이벤트 (TakeWaterDamage 유지)
    // =========================================================

    public void TakeWaterDamage(float damage, Vector3 hitPoint)
    {
        if (animator != null) animator.SetTrigger(_animIDHit);
        currentWetness = Mathf.Min(currentWetness + damage, maxWetness);

        if (currentWetness >= maxWetness)
        {
            Debug.Log($"[{gameObject.name}] 완전히 젖었습니다!");
        }
    }

    public float GetWetnessPercent()
    {
        return (currentWetness / maxWetness) * 100f;
    }

    void UpdateWetnessEffect()
    {
        if (wetnessMaterials.Count == 0) return;
        float wetnessRatio = currentWetness / maxWetness;
        foreach (var mat in wetnessMaterials)
        {
            mat.SetFloat("_Wetness", wetnessRatio);
        }
    }

    void UpdateSpeedByWetness()
    {
        float speedMultiplier = 1f - (currentWetness / maxWetness);
        speedMultiplier = Mathf.Clamp01(speedMultiplier);

        float currentBaseSpeed = patrolSpeed;
        switch (currentState)
        {
            case GuardState.Alert: currentBaseSpeed = baseAlertSpeed; break;
            case GuardState.Chase: currentBaseSpeed = baseChaseSpeed; break;
            default: currentBaseSpeed = basePatrolSpeed; break;
        }
        
        agent.speed = currentBaseSpeed * speedMultiplier;
    }

    void SetDestination()
    {
        if (waypoints.Length > 0) agent.destination = waypoints[currentWaypointIndex].position;
    }

    void MoveToClosestWaypoint()
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
        currentWaypointIndex = closestIndex;
        Vector3 targetPos = waypoints[currentWaypointIndex].position;
        agent.SetDestination(targetPos);
        
        Vector3 dir = (targetPos - transform.position).normalized;
        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
    }

    void UpdateAnimator()
    {
        if (animator == null) return;
        // isAlert 파라미터는 ChangeState에서 처리하므로 여기선 생략 가능하지만,
        // 안전을 위해 상태 확인용으로 남겨둠
        bool isAlertState = (currentState == GuardState.Alert || currentState == GuardState.Chase);
        animator.SetBool(_animIDAlert, isAlertState);
    }

    // 소리 듣기 이벤트
    public void OnSoundHeard(Vector3 soundPosition)
    {
        float distance = Vector3.Distance(transform.position, soundPosition);
        if (distance <= hearingRange && currentState == GuardState.Patrol)
        {
            lastKnownPosition = soundPosition;
            agent.SetDestination(soundPosition);
            ChangeState(GuardState.Suspicious);
        }
    }

    // 충돌 이벤트 (물 웅덩이 등)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WaterPuddle") && !isInPuddle)
        {
            isInPuddle = true;
            currentWetness = stunWetness;
            if (!isFalling) StartCoroutine(FallInPuddleCoroutine());
        }
        else if (other.TryGetComponent<IDamageable>(out IDamageable target))
        {
            // 몸으로 부딪히는 데미지 (유지)
            target.TakeDamage(1);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("WaterPuddle")) currentWetness = stunWetness;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("WaterPuddle")) isInPuddle = false;
    }

    private IEnumerator FallInPuddleCoroutine()
    {
        isFalling = true;
        agent.isStopped = true;
        if (animator != null) animator.SetTrigger(_animIDFall);

        yield return new WaitForSeconds(fallStunDuration);

        isFalling = false;
        agent.isStopped = false;
        // 넘어진 직후 바로 공격하지 않도록 쿨타임 살짝 갱신 가능 (선택사항)
    }

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        Gizmos.color = (currentState == GuardState.Alert) ? Color.red : Color.yellow;
        
        foreach (var wp in waypoints)
        {
            if (wp) Gizmos.DrawWireSphere(wp.position, 0.5f);
        }
        
        Gizmos.color = (currentState == GuardState.Alert || currentState == GuardState.Chase) ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, hearingRange);
        
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
}
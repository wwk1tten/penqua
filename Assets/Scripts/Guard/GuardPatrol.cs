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
    public enum WetnessStage { Dry, Damp, Soaked, Overloaded }

    [Header("State")]
    public GuardState currentState = GuardState.Patrol;
    public float alertTimeout = 0.5f;

    [Header("Patrol Settings")]
    public Transform[] waypoints;
    public float patrolSpeed = 3.5f;
    public float chaseSpeed = 5f;
    public float searchSpeed = 2.0f;
    public float waypointReachedDistance = 1.0f;
    public float waitTime = 2.0f;

    [Header("Search Settings")]
    public float searchDuration = 4.0f; // 놓치고 나서 두리번거리는 시간
    private float searchTimer = 0f;
    private bool isSearching = false;

    [Header("Alert Settings")]
    public float alertSpeed = 2f;

    [Header("Detection")]
    public float hearingRange = 10f;
    [Range(0.5f, 2.0f)]
    public float hearingSensitivity = 1.0f;
    
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

    [Header("Wetness Stages")]
    [Range(0f, 1f)] public float stage1Threshold = 0.33f; // Damp: 살짝 젖음
    [Range(0f, 1f)] public float stage2Threshold = 0.60f; // Soaked: 흠뻑
    [Range(0f, 1f)] public float stage3Threshold = 0.85f; // Overloaded: 과부하 → 스턴
    public float stage1SpeedMult = 0.8f;   // 이동 소폭 감소
    public float stage2SpeedMult = 0.5f;   // 이동 크게 감소
    public float normalAcceleration = 8f;  // 기본 NavMesh 가속도
    public float slideAcceleration = 1.5f; // 미끄럼 표현용 가속도 (낮을수록 둔하게 방향 전환)

    // 내부 변수
    private NavMeshAgent agent;
    private AISensor sensor;
    private Animator animator;
    private GuardIcon alertIcon;
    private GuardIcon susIcon;    
    private Transform playerTarget = null;
    
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private float alertTimer = 0f;
    
    private float lastAttackTime = -999f;
    private float currentWetness = 0f;
    private bool isInPuddle = false;
    private bool isFalling = false;
    private WetnessStage currentWetnessStage = WetnessStage.Dry;
    private bool stunTriggeredThisWave = false; // 같은 젖음 파동에서 스턴 중복 방지
    
    private List<Material> wetnessMaterials = new List<Material>();
    private float basePatrolSpeed, baseAlertSpeed, baseChaseSpeed;
    private Vector3 lastKnownPosition;

    // 애니메이션 해시 (최적화)
    private int _animIDFall;
    private int _animIDHit;
    private int _animIDAlert;
    private int _animIDAttack;
    private int _animIDSearch;

    // =========================================================
    // 2. 초기화 (Start)
    // =========================================================
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        sensor = GetComponent<AISensor>();
        animator = GetComponent<Animator>();
        alertIcon = GetComponent<GuardIcon>();
        susIcon = GetComponent<GuardIcon>();

        // 속도 백업
        basePatrolSpeed = patrolSpeed;
        baseAlertSpeed = alertSpeed;
        baseChaseSpeed = chaseSpeed; // [중요] 기존 코드 오타 수정됨

        // 애니메이션 ID 캐싱
        _animIDFall = Animator.StringToHash("Fall");
        _animIDHit = Animator.StringToHash("Hit");
        _animIDAlert = Animator.StringToHash("isAlert");
        _animIDAttack = Animator.StringToHash("Attack");
        _animIDSearch = Animator.StringToHash("isSearching");

        agent.speed = patrolSpeed;
        alertIcon.SetAlert(false);
        susIcon.SetAlert(false);


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
    // 3. Update 
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
    // 4. 기능별 분리 함수들 
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
        if (isFalling) return; // 스턴 중에는 회복 일시 정지

        if (currentWetness > 0)
        {
            currentWetness -= wetnessDecayRate * Time.deltaTime;
            currentWetness = Mathf.Max(0, currentWetness);
        }
    }

    WetnessStage GetWetnessStage()
    {
        float ratio = currentWetness / maxWetness;
        if (ratio >= stage3Threshold) return WetnessStage.Overloaded;
        if (ratio >= stage2Threshold) return WetnessStage.Soaked;
        if (ratio >= stage1Threshold) return WetnessStage.Damp;
        return WetnessStage.Dry;
    }

    void OnWetnessStageChanged(WetnessStage from, WetnessStage to)
    {
        // 과부하 진입 시 스턴 발동 (중복 방지)
        if (to == WetnessStage.Overloaded && !isFalling && !stunTriggeredThisWave)
        {
            stunTriggeredThisWave = true;
            StartCoroutine(FallInPuddleCoroutine());
        }

        // 과부하에서 내려오면 다음 과부하에서 다시 스턴 가능
        if (from == WetnessStage.Overloaded && to != WetnessStage.Overloaded)
        {
            stunTriggeredThisWave = false;
        }

        Debug.Log($"[{gameObject.name}] 젖음 단계: {from} → {to}");
    }

    void DetectPlayer()
    {
        if (sensor == null) return;
        if (sensor.IsInSight(playerTarget.gameObject))
        {
            // 발견! -> 이미 추격 중이 아니면 Alert 발동
            if (currentState != GuardState.Chase && currentState != GuardState.Alert)
            {
                ChangeState(GuardState.Alert);
            }
        }
    }

    // =========================================================
    // 5. 상태별 업데이트 (UpdateXXX)
    // =========================================================

    void UpdatePatrol()
    {
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
        // 발견 후 잠깐 멈칫했다가 추격
        alertTimer -= Time.deltaTime;
        if (alertTimer <= 0.5f) ChangeState(GuardState.Chase);
        
        // Alert 상태에서도 계속 플레이어를 바라보게 하면 좋음
        Vector3 dir = (playerTarget.position - transform.position).normalized;
        dir.y = 0;
        if(dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
    }

    void UpdateChase()
    {
        // 1. 플레이어를 놓쳤는지 확인 (센서 시야에서 사라짐)
        if (sensor != null && !sensor.IsInSight(playerTarget.gameObject))
        {
            // 바로 돌아가는 게 아니라, '마지막 위치'를 수색하러 감
            ChangeState(GuardState.Suspicious);
            return;
        }

        // 2. 추격 로직
        lastKnownPosition = playerTarget.position; // 계속 위치 갱신
        agent.SetDestination(lastKnownPosition);

        // 사거리 안이면 멈춰서 공격 준비 (회전)
        float dist = Vector3.Distance(transform.position, playerTarget.position);
        if (dist <= attackRange)
        {
            agent.isStopped = true;
            Vector3 dir = (playerTarget.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
        }
        else
        {
            agent.isStopped = false;
        }
    }

    void UpdateSuspicious()
    {        
        // 1. 목적지(소리 난 곳 or 마지막 목격지)로 이동 중
        if (!isSearching)
        {
            agent.SetDestination(lastKnownPosition);

            // 목적지 도착 체크
            if (!agent.pathPending && agent.remainingDistance <= waypointReachedDistance)
            {
                // 도착했으면 수색 시작 (두리번)
                isSearching = true;
                searchTimer = searchDuration;
                agent.isStopped = true; // 멈춤
                if(animator) animator.SetBool(_animIDSearch, true); // 두리번 애니메이션
            }
        }
        // 2. 도착 후 제자리 수색 중
        else
        {
            searchTimer -= Time.deltaTime;
            
            // 수색 시간 끝 -> 아무도 없음 -> 복귀
            if (searchTimer <= 0)
            {
                if(animator) animator.SetBool(_animIDSearch, false);
                ChangeState(GuardState.Return);
            }
        }
    }

    void UpdateReturn()
    {
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
        
        // 이전 상태 정리
        if(animator) animator.SetBool(_animIDSearch, false);
        if(animator) animator.SetBool(_animIDAlert, false);

        currentState = newState;
        agent.isStopped = false;

        switch (newState)
        {
            case GuardState.Patrol:
                agent.speed = patrolSpeed;
                SetDestination();
                break;

            case GuardState.Suspicious:
                alertIcon.SetAlert(false);
                susIcon.SetSus(true);
                agent.speed = searchSpeed; // 천천히 다가감 (긴장감)
                isSearching = false;       // 이동부터 시작
                agent.SetDestination(lastKnownPosition);
                break;

            case GuardState.Alert: // 발견 순간
                alertIcon.SetAlert(true);
                susIcon.SetSus(false);
                agent.speed = 0; // 잠깐 멈칫
                alertTimer = alertTimeout;
                if(animator) animator.SetBool(_animIDAlert, true);
                break;

            case GuardState.Chase:
                alertIcon.SetAlert(true);
                susIcon.SetSus(false);
                agent.speed = chaseSpeed; // 전력 질주
                if(animator) animator.SetBool(_animIDAlert, true); // 추격 모션
                break;

            case GuardState.Return:
                alertIcon.SetAlert(false);
                susIcon.SetSus(false);
                agent.speed = patrolSpeed;
                MoveToClosestWaypoint();
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
        WetnessStage stage = GetWetnessStage();

        // 단계 전환 감지
        if (stage != currentWetnessStage)
        {
            OnWetnessStageChanged(currentWetnessStage, stage);
            currentWetnessStage = stage;
        }

        // 단계별 속도 배율 및 가속도 설정
        float speedMult;
        switch (stage)
        {
            case WetnessStage.Damp:
                speedMult = stage1SpeedMult; 
                agent.acceleration = normalAcceleration;
                Debug.Log("Damp mode");
                break;
            case WetnessStage.Soaked: 
                speedMult = stage2SpeedMult; // 속도 0.8x
                agent.acceleration = slideAcceleration; // 방향 전환 둔화 → 미끄럼 표현
                Debug.Log("Soaked mode");
                break;
            case WetnessStage.Overloaded:
                speedMult = stage2SpeedMult; 
                agent.acceleration = slideAcceleration;
                Debug.Log("Overload mode");
                break;
            default: // Dry
                speedMult = 1f;
                agent.acceleration = normalAcceleration;
                break;
        }

        float baseSpeed;
        switch (currentState)
        {
            case GuardState.Alert: baseSpeed = baseAlertSpeed; break;
            case GuardState.Chase: baseSpeed = baseChaseSpeed; break;
            default: baseSpeed = basePatrolSpeed; break;
        }

        agent.speed = baseSpeed * speedMult;
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
    public void OnSoundHeard(Vector3 soundPosition, float soundRadius)
    {
        // 이미 추격 중이거나 경계 중이면 소리 무시 (시각 정보가 더 중요함)
        if (currentState == GuardState.Chase || currentState == GuardState.Alert) return;

        float distance = Vector3.Distance(transform.position, soundPosition);
        
        // 내가 들을 수 있는 유효 거리 = 소리 크기(Radius) * 내 귀 밝기(Sensitivity)
        float effectiveHearingDistance = soundRadius * hearingSensitivity;

        // 소리가 들리면 -> 그 위치를 '의심'하고 조사하러 감
        if (distance <= hearingRange)
        {
            lastKnownPosition = soundPosition; // 가야 할 곳 설정
            
            // 패트롤 중에 들었다면 -> Suspicious로 전환
            if (currentState == GuardState.Patrol || currentState == GuardState.Return)
            {
                ChangeState(GuardState.Suspicious);
            }
            // 이미 Suspicious 상태라면? -> 새로운 소리 위치로 목표 갱신
            else if (currentState == GuardState.Suspicious)
            {
                isSearching = false; // 다시 이동 모드로
                agent.isStopped = false;
                agent.SetDestination(lastKnownPosition);
            }
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

    }
}
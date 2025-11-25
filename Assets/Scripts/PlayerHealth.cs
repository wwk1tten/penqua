using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using StarterAssets;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    private int currentHealth;

    // ... (Invincibility & Physics 필드는 유지)

    [Header("Respawn & Stun Settings")]
    public float stunDuration = 2.0f; 
    private bool isStunned = false; 

    // 컴포넌트 참조
    private MonoBehaviour playerController; 
    private Animator animator;
    private Rigidbody rb;
    private CheckpointManager checkpointManager; // 🚩 매니저 참조 추가

    [Header("Events")]
    public UnityEvent<int> OnHealthChanged; 
    public UnityEvent OnDeath;

    [System.Obsolete]
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // 🚨 본인의 플레이어 컨트롤러 타입으로 변경
        playerController = GetComponent<ThirdPersonController>(); 
        animator = GetComponentInChildren<Animator>(); 
        
        // 🚩 매니저 찾기
        checkpointManager = FindObjectOfType<CheckpointManager>();
        if (checkpointManager == null)
        {
            Debug.LogError("씬에 CheckpointManager가 없습니다! 리스폰 시스템이 정상 작동하지 않습니다.");
        }

        currentHealth = maxHealth;
    }

    void Start()
    {
        OnHealthChanged?.Invoke(currentHealth);
    }

    // ... (TakeDamage 함수 유지)

    void Die()
    {
        Debug.Log("플레이어 기절!");
        OnDeath?.Invoke(); 
        
        // 1. 상태 및 컨트롤러 비활성화
        isStunned = true;
        if (playerController) playerController.enabled = false;
        
        // 2. 물리 정지 및 애니메이션 트리거
        rb.linearVelocity = Vector3.zero; 
        rb.angularVelocity = Vector3.zero; 
        // 🐧 기절 애니메이션 트리거
        if (animator) animator.SetTrigger("Stun"); 

        // 3. 리스폰 코루틴 시작
        StartCoroutine(StunAndRespawnRoutine());
    }

    IEnumerator StunAndRespawnRoutine()
    {
        // 1. 기절 애니메이션 재생 시간만큼 대기
        yield return new WaitForSeconds(stunDuration);

        // 2. 리스폰 로직
        if (checkpointManager != null)
        {
            // 🚩 매니저에게 리스폰 위치를 요청하여 이동
            transform.position = checkpointManager.GetRespawnPosition();
            Debug.Log("리스폰 성공!");
        }
        
        // 3. 체력 및 상태 초기화
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
        
        rb.linearVelocity = Vector3.zero; // 물리 재설정

        isStunned = false;
        if (playerController) playerController.enabled = true; // 컨트롤 재활성화

        // 4. 무적 시간 부여
        // StartCoroutine(InvincibilityRoutine());
        
        // 🐧 재시작 후 Idle 애니메이션으로 돌아가도록 설정 (필요시)
        if (animator) animator.SetTrigger("Respawned"); 
    }

    public void TakeDamage(int amount, Vector3 hitDirection)
    {
        throw new System.NotImplementedException();
    }

    // ... (InvincibilityRoutine 함수 유지)

    // 인터페이스 구현부 (TakeDamage 등)는 위 단계를 참고하여 기존 코드를 유지하면 됩니다.
}
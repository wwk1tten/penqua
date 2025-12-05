using UnityEngine;
using UnityEngine.Events;
using System.Collections;
// StarterAssets 네임스페이스가 필요할 수 있습니다. 
// using StarterAssets; 

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    [Header("Invincibility")]
    public float invincibilityDuration = 2.0f;
    private bool isInvincible = false;
    public GameObject modelObject;

    [Header("Stun & Respawn")]
    public float stunDuration = 2.0f;
    private bool isStunned = false;

    [Header("Physics (CharacterController)")]
    public MonoBehaviour movementScript;
    private CharacterController charController; // Rigidbody 대신 이거 사용
    
    private Animator animator;
    private CheckPointManager checkpointManager;

    [Header("Events")]
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent OnDeath;

    void Awake()
    {
        // Rigidbody 삭제, CharacterController 가져오기
        charController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        checkpointManager = GetComponent<CheckPointManager>();

        // 만약 못 찾으면 콘솔에 경고 띄우기
        if(movementScript == null) 
            Debug.LogWarning("움직임 스크립트를 찾지 못했습니다! Inspector에서 이름을 확인하세요.");

        currentHealth = maxHealth;
    }

    void Start()
    {
        OnHealthChanged?.Invoke(currentHealth);
    }

    public Transform GetTransform()
    {
        return transform;
    }

    // 1. 기본형 구현
    public void TakeDamage(int damage)
    {
        // 기본 넉백 설정 (뒤로 살짝)
        TakeDamage(damage, transform.position, -transform.forward, 5f);
    }

    // 2. 확장형 구현 (경비원 공격용)
    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 knockbackDir, float knockbackForce)
    {
        if (isInvincible || currentHealth <= 0 || isStunned) return;

        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth);
        Debug.Log($"아야! 남은 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // 데미지 입었을 때 넉백 코루틴 실행
            StartCoroutine(KnockbackRoutine(knockbackDir, knockbackForce));
            StartCoroutine(InvincibilityRoutine());
        }
    }

    // 💥 핵심: CharacterController용 수동 넉백
    IEnumerator KnockbackRoutine(Vector3 dir, float force)
    {
        if (animator) animator.SetTrigger("Stun");
        // 1. 제어권 뺏기 (빙글 돌기 방지)
        if (movementScript != null) movementScript.enabled = false;
        isStunned = true;

        float timer = 0f;
        float duration = 0.3f; // 넉백 지속 시간 (짧게 설정)
        Vector3 pushDir = dir.normalized;
        pushDir.y = 0; // 수평으로만 밀리게 (필요하면 조절)

        // 2. 짧은 시간 동안 뒤로 강제 이동
        while (timer < duration)
        {
            timer += Time.deltaTime;
            
            // 힘이 서서히 줄어들게 (Lerp)
            float currentForce = Mathf.Lerp(force, 0, timer / duration);
            
            // CharacterController로 이동 실행
            if(charController != null)
                charController.Move(pushDir * currentForce * Time.deltaTime);

            yield return null;
        }

        // 3. 넉백이 끝나면 바로 움직임 권한을 줄지, 아니면 '경직' 시간을 줄지 결정
        // 여기서는 바로 다시 움직일 수 있게 풀어줍니다.
        isStunned = false;
        if (movementScript != null) movementScript.enabled = true;
    }

    private void Die()
    {
        Debug.Log("플레이어 기절!");
        
        // 1. 사망 애니메이션 재생 (Die 트리거)
        if (animator) animator.SetTrigger("Die");
        OnDeath?.Invoke();

        isStunned = true;
        
        // 2. 움직임 끄기 
        if (movementScript != null) movementScript.enabled = false;

        // 3. 물리 충돌 문제 방지
        if (charController != null) charController.enabled = false;

        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        // 4. 누워있는 시간 동안 대기
        // (애니메이터 설정을 고쳤다면, 이 시간 동안 캐릭터는 바닥에 누운 채로 멈춰 있습니다)
        yield return new WaitForSeconds(stunDuration);

        // 5. 체크포인트로 순간이동 (CharacterController가 꺼진 상태여야 안전하게 이동됨)
        if (CheckPointManager.Instance != null)
        {
            transform.position = CheckPointManager.Instance.GetRespawnPosition();
        }

        // 한 프레임 대기 (위치 반영 확실하게 하기 위해)
        yield return null; 

        // 6. 컴포넌트 다시 켜기
        if (charController != null) charController.enabled = true;
        if (movementScript != null) movementScript.enabled = true;

        // 7. 상태 초기화
        currentHealth = maxHealth;
        isStunned = false;
        OnHealthChanged?.Invoke(currentHealth);

        // 🌟 핵심: 이제 일어나라고 알림! (1단계에서 만든 Trigger)
        if (animator) animator.SetTrigger("Respawn"); 

        // 8. 부활 후 무적 시간
        StartCoroutine(InvincibilityRoutine());
    }

    // ... (무적 코루틴은 기존과 동일)
    IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        // 🛡️ 안전장치: modelObject가 할당 안 되어 있으면 에러 방지
        if (modelObject == null)
        {
            Debug.LogWarning("모델 오브젝트가 할당되지 않았습니다! 깜빡임 효과 없음.");
            yield return new WaitForSeconds(invincibilityDuration);
            isInvincible = false;
            yield break;
        }

        // 🛡️ 안전장치 2: 만약 modelObject가 나 자신(본체)이라면 경고하고 중단
        if (modelObject == gameObject)
        {
            Debug.LogError("치명적 실수: Model Object에 플레이어 본체를 넣지 마세요! 자식 모델을 넣으세요.");
            isInvincible = false;
            yield break;
        }

        float timer = 0f;
        while (timer < invincibilityDuration)
        {
            // 모델을 껐다 켰다 (자식 오브젝트만 꺼지므로 스크립트는 계속 돔)
            modelObject.SetActive(!modelObject.activeSelf);
            
            yield return new WaitForSeconds(0.2f);
            timer += 0.2f;
        }

        // 루프 끝나면 반드시 켜두기
        modelObject.SetActive(true);
        isInvincible = false;
    }
}
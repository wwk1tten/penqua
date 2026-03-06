using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using StarterAssets; 

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    [Header("Invincibility")]
    public float invincibilityDuration = 2.0f;
    private bool isInvincible = false;
    public GameObject modelObject; // ⚠️ 주의: 애니메이터가 없는 순수 자식 메쉬 객체를 넣으세요.

    [Header("Stun & Respawn")]
    public float stunDuration = 2.0f;
    private bool isStunned = false;

    [Header("Components")]
    private CharacterController charController;
    private ThirdPersonController playerController; // 모호한 MonoBehaviour 대신 명확한 타입 사용
    private Animator animator;

    [Header("Events")]
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent OnDeath;

    void Awake()
    {
        charController = GetComponent<CharacterController>();
        playerController = GetComponent<ThirdPersonController>();
        animator = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;
    }

    void Start()
    {
        OnHealthChanged?.Invoke(currentHealth);
    }

    public Transform GetTransform() => transform;

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position, -transform.forward, 5f);
    }

    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 knockbackDir, float knockbackForce)
    {
        if (isInvincible || currentHealth <= 0 || isStunned) return;

        // Mathf.Max를 사용하여 음수 방지 로직을 한 줄로 압축
        currentHealth = Mathf.Max(0, currentHealth - damage);
        Debug.Log($"아야! 남은 체력: {currentHealth}");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateVignette(currentHealth, maxHealth);
            GameManager.Instance.TriggerDamageFlash();
        }

        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(KnockbackRoutine(knockbackDir, knockbackForce));
            StartCoroutine(InvincibilityRoutine());
        }
    }

    IEnumerator KnockbackRoutine(Vector3 dir, float force)
    {
        isStunned = true;
        animator?.SetTrigger("Stun");
        
        // 스크립트를 끄는 대신, 내부 상태만 '통제 불가'로 변경
        playerController?.SetControlState(false);

        float timer = 0f;
        float duration = 0.3f; 
        Vector3 pushDir = new Vector3(dir.x, 0, dir.z).normalized; // 수평으로만 확실히 밀리게 보정

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float currentForce = Mathf.Lerp(force, 0, timer / duration);
            
            // 넉백 도중 CharacterController가 꺼져있지 않은지 안전 검사 추가
            if(charController != null && charController.enabled)
            {
                charController.Move(pushDir * currentForce * Time.deltaTime);
            }

            yield return null;
        }

        isStunned = false;
        // ★ 제어권 반환
        playerController?.SetControlState(true);
    }

    private void Die()
    {
        Debug.Log("플레이어 사망!");
        isStunned = true;
        animator?.SetTrigger("Die");
        OnDeath?.Invoke();

        playerController?.SetControlState(false);
        
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(stunDuration);

        // ★ 환풍구 텔레포트 때 배운 법칙 적용: 위치 강제 이동 전에는 반드시 CC를 끈다.
        if (charController != null) charController.enabled = false;

        if (CheckPointManager.Instance != null)
        {
            transform.position = CheckPointManager.Instance.GetRespawnPosition();
        }

        yield return null; // 위치 적용을 위해 1프레임 확실히 대기

        if (charController != null) charController.enabled = true;

        currentHealth = maxHealth;
        isStunned = false;
        OnHealthChanged?.Invoke(currentHealth);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateVignette(currentHealth, maxHealth);
        }

        animator?.SetTrigger("Respawn");
        
        // 부활했으니 다시 움직일 수 있도록 통제권 반환
        playerController?.SetControlState(true);

        StartCoroutine(InvincibilityRoutine());
    }

    IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        // 경고 로직들을 통합하여 불필요한 yield break 제거
        if (modelObject != null && modelObject != gameObject)
        {
            float timer = 0f;
            while (timer < invincibilityDuration)
            {
                modelObject.SetActive(!modelObject.activeSelf);
                yield return new WaitForSeconds(0.2f);
                timer += 0.2f;
            }
            modelObject.SetActive(true); // 루프 종료 후 반드시 켜기
        }
        
        isInvincible = false;
    }
}
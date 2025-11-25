using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("하트 설정")]
    public int maxHearts = 3;
    public int currentHearts;

    [Header("넉백 설정")]
    public float knockbackDuration = 0.2f;
    public float knockbackResistance = 1f; // 넉백 세기 조절용

    CharacterController _controller;
    GameManager gameManager;
    bool _isKnockback = false;
    Vector3 _knockbackVelocity;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
         gameManager = GameManager.Instance;
    }

    void Start()
    {
        currentHearts = maxHearts;
        gameManager.UpdateHearts(currentHearts);
    }

    void Update()
    {
        if (_isKnockback)
        {
            _controller.Move(_knockbackVelocity * Time.deltaTime);
        }
    }

    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitDirection, float knockbackForce)
    {
        if (currentHearts <= 0) return;

        currentHearts -= damage;
        if (currentHearts < 0) currentHearts = 0;

        gameManager.UpdateHearts(currentHearts);

        if (knockbackForce > 0f)
        {
            ApplyKnockback(hitDirection, knockbackForce);
        }

        if (currentHearts <= 0)
        {
            Die();
        }
    }

    void ApplyKnockback(Vector3 hitDirection, float force)
    {
        // 위로 살짝 + 뒤로 밀리게
        Vector3 dir = hitDirection.normalized;
        dir.y = 0f;
        dir.Normalize();

        _knockbackVelocity = (-dir * force / knockbackResistance) + Vector3.up * 2f;
        _isKnockback = true;
        CancelInvoke(nameof(StopKnockback));
        Invoke(nameof(StopKnockback), knockbackDuration);
    }

    void StopKnockback()
    {
        _isKnockback = false;
        _knockbackVelocity = Vector3.zero;
    }

    void Die()
    {
        Debug.Log("플레이어 사망");
        // TODO: 게임오버 연출, 리스폰 등
    }
}

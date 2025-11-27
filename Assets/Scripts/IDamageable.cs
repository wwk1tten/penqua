using UnityEngine;

public interface IDamageable
{
    // 기본형 (함정 등 단순 데미지용)
    void TakeDamage(int damage);

    // 확장형 (경비원 공격용: 데미지 + 맞은 위치 + 밀려날 방향 + 밀는 힘)
    void TakeDamage(int damage, Vector3 hitPoint, Vector3 knockbackDir, float knockbackForce);
    
    Transform GetTransform();
}
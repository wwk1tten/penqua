using UnityEngine;

// 모든 '피해를 입을 수 있는 존재'는 이 인터페이스를 상속받습니다.
public interface IDamageable
{
    void TakeDamage(int amount, Vector3 hitDirection);
}
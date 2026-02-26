using StarterAssets;
using UnityEngine;

public class WaterGrowZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 물에 들어온 것이 캡슐인지 확인
        CapsuleController capsule = other.GetComponent<CapsuleController>();
        if (capsule != null)
        {
            Debug.Log($"캡슐 {capsule.capsuleID}가 물에 닿았습니다!");

            // 1. GameManager에 알림
            GameManager.Instance.OnCapsuleCollected(capsule.capsuleID);
            
            // 2. 동물 친구 생성
            if (capsule.animalPrefab != null)
            {
                // 캡슐 위치에 동물 생성
                Instantiate(capsule.animalPrefab, capsule.transform.position, Quaternion.identity);
            }
            
            // 3. 캡슐 오브젝트 파괴
            Destroy(capsule.gameObject);
        }

        if (other.TryGetComponent(out ThirdPersonController pc))
        {
            //pc._isSwimming = true);
        }

    }
}

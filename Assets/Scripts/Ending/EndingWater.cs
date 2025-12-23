using UnityEngine;
using StarterAssets; // 플레이어 스크립트용

public class EndingWater : MonoBehaviour
{
    public float sinkSpeed = 2.0f; // 가라앉는 속도

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. 플레이어 조작 끄기 (더 이상 못 움직임)
            var controller = other.GetComponent<ThirdPersonController>();
            if (controller != null) controller.enabled = false;

            var animator = other.GetComponent<Animator>();
            if (animator != null) animator.SetBool("FreeFall", true); // 떨어지는 모션(선택)

            // 2. 물리력 끄기 (중력 간섭 제거)
            var rb = other.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true; 
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // 3. 계속 아래로 끌어내리기
        if (other.CompareTag("Player"))
        {
            other.transform.position -= Vector3.up * sinkSpeed * Time.deltaTime;
        }
    }
}
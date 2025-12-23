using UnityEngine;
using System.Collections;
using Cinemachine;

public class PhysicalCapsule : MonoBehaviour
{
    [Header("설정")]
    public GameObject animalPrefab; // 변신할 동물 프리팹
    public CinemachineVirtualCamera animalCam;
    public GameObject poofEffect;   // 펑! 하는 연기 이펙트 (선택)
    public float hatchDelay = 1.0f; // 바닥에 닿고 몇 초 뒤에 변할지

    private bool hasLanded = false;

    // 물(Trigger) 대신 물리적 충돌(Collision)을 감지합니다.
    private void OnCollisionEnter(Collision collision)
    {
        // 이미 닿았으면 무시
        if (hasLanded) return;

        // "Floor" 태그가 달린 바닥에 닿았을 때
        // (혹은 태그 확인 없이 그냥 아무데나 닿으면 변하게 해도 됨)
        if (collision.gameObject.CompareTag("WaterSource") || collision.gameObject.CompareTag("Floor"))
        {
            hasLanded = true;
            StartCoroutine(HatchRoutine());
        }
    }

    IEnumerator HatchRoutine()
    {
        // 바닥에 닿고 잠시 대기 (구르는 연출)
        yield return new WaitForSeconds(hatchDelay);

        SpawnAnimal();
    }

    void SpawnAnimal()
    {
        if (animalPrefab != null)
        {
            // 1. 동물 소환 (살짝 위쪽에 소환해서 끼임 방지)
            GameObject spawnedAnimal = Instantiate(animalPrefab, transform.position + Vector3.up * 0.5f, transform.rotation);

            if (poofEffect != null)
            {
                Instantiate(poofEffect, transform.position, Quaternion.identity);
            }

            if (CinemachineManager.Instance != null && animalCam != null)
            {
                // 인자 3개짜리 함수 호출 (타겟, 시간, 카메라)
                CinemachineManager.Instance.FocusOnTarget(spawnedAnimal.transform, 2.5f, animalCam);
            }
        }
        // 3. 캡슐 껍데기 삭제
        Destroy(gameObject);
    }
}
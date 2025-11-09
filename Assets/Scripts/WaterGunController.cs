using UnityEngine;

public class WaterGunController : MonoBehaviour
{
    [Header("카메라 & 총구")]
    public Camera mainCamera;
    public Transform gunMuzzle;

    [Header("발사 설정")]
    public float shootRange = 50f;
    public float fireRate = 10f; // 초당 발사 횟수
    private float nextFireTime = 0f;

    [Header("물탱크 시스템")]
    public float maxWater = 100f;
    public float waterConsumptionRate = 15f; // 초당 물 소모량
    public float waterRegenRate = 10f; // 초당 물 회복량
    private float currentWater;

    [Header("VFX - 시각 효과")]
    public ParticleSystem waterStreamEffect; // 물줄기 파티클
    public GameObject waterSplashEffect; // 물보라(피격) 프리팹

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        currentWater = maxWater;

        // 물줄기 파티클이 있으면 일단 정지
        if (waterStreamEffect != null)
        {
            waterStreamEffect.Stop();
        }
    }

    void Update()
    {
        // 마우스 좌클릭을 누르고 있으면 연속 발사
        if (Input.GetMouseButton(0) && currentWater > 0)
        {
            // 1. 물 소모
            currentWater -= waterConsumptionRate * Time.deltaTime;
            
            // 2. 물줄기 파티클 재생
            if (waterStreamEffect != null && !waterStreamEffect.isPlaying)
            {
                waterStreamEffect.Play();
            }

            // 3. 발사 속도에 맞춰 Shoot() 함수 호출
            if (Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + 1f / fireRate;
                Shoot();
            }
        }
        else
        {
            // 4. 안 쏘고 있으면 물 회복
            if (currentWater < maxWater)
            {
                currentWater += waterRegenRate * Time.deltaTime;
            }

            // 5. 물줄기 파티클 정지
            if (waterStreamEffect != null && waterStreamEffect.isPlaying)
            {
                waterStreamEffect.Stop();
            }
        }

        // 물탱크 용량 제한
        currentWater = Mathf.Clamp(currentWater, 0, maxWater);
        
        // UI 업데이트 등...
        // Debug.Log($"현재 물: {currentWater:F1}");
    }

    void Shoot()
    {
        // 화면 중앙에서 레이 발사 (TPS 조준)
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        
        if (Physics.Raycast(ray, out RaycastHit hit, shootRange))
        {
            // 피격 지점에 물보라 효과 생성
            if (waterSplashEffect != null)
            {
                Instantiate(waterSplashEffect, hit.point, Quaternion.LookRotation(hit.normal));
            }

            // 바닥에 맞으면 물웅덩이 생성
            if (hit.collider.CompareTag("Floor"))
            {
                // Instantiate(waterPuddlePrefab, hit.point, ...);
            }
            
            // 경비원에게 젖음 피해
            if (hit.collider.CompareTag("Guard"))
            {
                GuardPatrol guard = hit.collider.GetComponent<GuardPatrol>();
                if (guard != null)
                {
                    // 연속 발사이므로 피해량을 시간에 맞춰 조절
                    guard.TakeWaterDamage(1f, hit.point); 
                }
            }
        }
    }

    // UI 표시용 함수
    public float GetWaterRatio()
    {
        return currentWater / maxWater;
    }
}

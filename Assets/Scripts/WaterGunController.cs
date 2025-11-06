using UnityEngine;

public class WaterGunController : MonoBehaviour
{
    [Header("총기 설정")]
    public Transform gunMuzzle; // 총구 위치 (null이면 카메라 중앙 사용)
    public Camera mainCamera;
    public GameObject waterPuddlePrefab;

    
    [Header("공격 설정")]
    public float shootRange = 50f;
    public float shootCooldown = 0.1f;
    public float wetnessDamage = 10f; // 한 번 맞을 때 증가할 젖음 수치
    
    private float lastShootTime = 0f;
    
    
    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }
    
    void Update()
    {
        // 마우스 좌클릭으로 발사
        if (Input.GetMouseButtonDown(0) && Time.time >= lastShootTime + shootCooldown)
        {
            Shoot();
            lastShootTime = Time.time;
        }
    }
    
    void Shoot()
    {
        // 1. 카메라 중심에서 Ray 생성 (조준점과 완벽 일치)
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Ray ray = mainCamera.ScreenPointToRay(screenCenter);
        Vector3 targetPoint;

        // 2. 카메라 Raycast로 우선 맞는 목표 지점 (벽, 적 등)
        float maxDistance = 1000f;
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            targetPoint = hit.point;
        else
            targetPoint = ray.GetPoint(maxDistance);

        // 3. 총구에서 targetPoint로 방향
        Vector3 muzzlePos = gunMuzzle.position;
        Vector3 direction = (targetPoint - muzzlePos).normalized;

        // 4. 총구~targetPoint 사이에 장애물이 또 있나 검사
        if (Physics.Raycast(muzzlePos, direction, out RaycastHit muzzleHit, maxDistance)) {
            targetPoint = muzzleHit.point;
            direction = (targetPoint - muzzlePos).normalized;
        }

        // 5. 총구에서 보정된 direction으로 히트스캔(즉시 타격, 이펙트 등)
        // 예: 총알 발사, 이펙트, 등등
        Debug.DrawRay(muzzlePos, direction * maxDistance, Color.red, 1f);
        // (맞은 대상을 처리)
        if (Input.GetMouseButtonDown(0)) {
            if (Physics.Raycast(muzzlePos, direction, out RaycastHit hitInfo, maxDistance))
                if (hitInfo.collider.CompareTag("Guard")){
                    GuardPatrol guard = hit.collider.GetComponent<GuardPatrol>();
                    if (guard != null)
                        guard.TakeWaterDamage(wetnessDamage, hit.point);
                        Debug.Log("적 맞춤!");
                }
                // 바닥에 명중하면 웅덩이 생성
                if (hitInfo.collider.CompareTag("Floor")) {
                    Instantiate(waterPuddlePrefab, hit.point + Vector3.up*0.01f, Quaternion.identity);
                }
            // 총알 등 이펙트도 direction으로 쏨
        }
    }

    
    
    // 선택: 물 튀김 이펙트
    void SpawnHitEffect(Vector3 position)
    {
        // 파티클이나 다른 이펙트 생성
        // Instantiate(waterSplashEffect, position, Quaternion.identity);
    }
}

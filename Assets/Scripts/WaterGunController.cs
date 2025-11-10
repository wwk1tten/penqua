using UnityEngine;
using System.Collections;


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
    public float waterConsumptionRate = 20f; // 초당 물 소모량
    public float reloadTime = 2.0f; // 재장전에 걸리는 시간
    private float currentWater;

    private bool isNearWaterSource = false; // 물 근처에 있는지 체크
    private bool isReloading = false;       // 현재 재장전 중인지 체크

    [Header("VFX - 시각 효과")]
    public ParticleSystem waterStreamEffect; // 물줄기 파티클
    public GameObject waterSplashEffect; // 물보라(피격) 프리팹

    private Animator playerAnimator; 
    private int animIDisReloading;

    void Start()
    {
        mainCamera = mainCamera ?? Camera.main;
        currentWater = maxWater;

        // 물줄기 파티클이 있으면 일단 정지
        if (waterStreamEffect != null) waterStreamEffect.Stop();

        playerAnimator = GetComponentInParent<Animator>();
        if (playerAnimator == null)
        {
            Debug.LogError("부모에게서 Player Animator를 찾을 수 없습니다!");
        }

        // "Reload" 파라미터의 해시 ID를 미리 받아옴
        animIDisReloading = Animator.StringToHash("isReloading");
    }

    void Update()
    {
        // 재장전 중일 때는 모든 행동을 막습니다.
        if (isReloading) return;

         // 'R'키를 누르고, 물 근처에 있으며, 물이 가득 차지 않았을 때 재장전 시작
        if (Input.GetKeyDown(KeyCode.R) && isNearWaterSource && currentWater < maxWater)
        {
            StartCoroutine(Reload());
            return; // 재장전을 시작하면 다른 행동은 하지 않음
        }

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
            // 4. 물줄기 파티클 정지
            if (waterStreamEffect != null && waterStreamEffect.isPlaying)
            {
                waterStreamEffect.Stop();
            }
        }

        // 물탱크 용량 제한
        currentWater = Mathf.Clamp(currentWater, 0, maxWater);
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

    IEnumerator Reload()
    {
        if (isReloading) yield break;

        isReloading = true;
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("isReloading", true);
        }

        yield return new WaitForSeconds(reloadTime); // reloadTime 만큼 대기
        

        currentWater = maxWater;

        // isReloading 불(bool) 파라미터를 false로 설정하여 루프 애니메이션 정지
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(animIDisReloading, false);
        }

        isReloading = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 들어간 콜라이더의 태그가 "WaterSource"이면
        if (other.CompareTag("WaterSource"))
        {
            isNearWaterSource = true;
            // 여기에 "Press R to Reload" 같은 UI를 띄워주면 더 좋습니다.
        }
    }

    // 트리거 콜라이더에서 나올 때 호출됨
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("WaterSource"))
        {
            isNearWaterSource = false;
            // "Press R to Reload" UI를 숨깁니다.
        }
    }

    public float GetWaterRatio()
    {
        return currentWater / maxWater;
    }
}

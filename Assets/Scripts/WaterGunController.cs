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

    [Header("물웅덩이")]
    public float puddleSpawnInterval = 0.1f;
    public float puddleMinDistance = 0.5f; // 이전 물웅덩이와 최소 거리
    public GameObject waterPuddlePrefab;
    private float nextPuddleTime = 0f;
    private Vector3 lastPuddlePosition = Vector3.zero;

    [Header("VFX - 시각 효과")]
    public ParticleSystem waterStreamEffect; // 물줄기 파티클
    public GameObject waterSplashEffect; // 물보라(피격) 프리팹
    [Header("NavMeshAgent")]
    public GameObject navObstaclePrefab; // NavPuddle_Obstacle 프리팹 연결 (1단계에서 만든 것)
    public float puddleDuration = 10f; // 물웅덩이 유지 시간
    [Header("Audio Settings")]
    public AudioSource audioSource; // 인스펙터에서 Audio Source 컴포넌트 연결
    public AudioClip shootSound;     // 물총 발사 시 재생할 소리
    public AudioClip reloadSound;      // 재장전 시 재생할 소리
    public AudioClip guardHitSound;         // 4. 경비원 피격 소리 (Guard에게 닿을 때)
    public AudioClip puddleSplashSound;



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
            // + 물웅덩이는 더 자주 체크하여 생성
            if (Time.time >= nextPuddleTime)
            {
                nextPuddleTime = Time.time + puddleSpawnInterval;
                CheckAndCreatePuddle();
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

            // 발사 사운드 재생
                    if (audioSource != null && shootSound != null)
                    {
                        audioSource.PlayOneShot(shootSound); 
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

                // 발사 사운드 재생
                if (audioSource != null && guardHitSound != null)
                {
                    audioSource.PlayOneShot(guardHitSound); 
                }
            }
        }
    }

    // [추가] 물웅덩이 생성만 담당하는 별도 함수
    void CheckAndCreatePuddle()
    {
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
    
        if (Physics.Raycast(ray, out RaycastHit hit, shootRange))
        {
            if (hit.collider.CompareTag("Floor"))
            {
                // 이전 물웅덩이와의 거리가 충분히 멀 때만 생성
                if (Vector3.Distance(hit.point, lastPuddlePosition) >= puddleMinDistance)
                {
                    // 웅덩이 사운드 재생
                    if (audioSource != null && puddleSplashSound != null)
                    {
                        audioSource.PlayOneShot(puddleSplashSound); 
                    }

                    CreateWaterPuddle(hit.point, hit.normal);
                    lastPuddlePosition = hit.point;

                    
                }
            }
        }
    }

    void CreateWaterPuddle(Vector3 position, Vector3 normal)
    {
        if (waterPuddlePrefab == null || navObstaclePrefab == null) 
        {
            Debug.LogError("Puddle Prefab 또는 Nav Obstacle Prefab이 연결되지 않았습니다!");
            return;
        }
        
        // **A. 시각 효과 (Visual Puddle) 생성**
        Quaternion alignToSurface = Quaternion.FromToRotation(Vector3.up, normal);
        Quaternion randomSpin = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        Quaternion finalRot = alignToSurface * randomSpin;
        Vector3 puddlePos = position + normal * 0.01f; // 바닥 위로 살짝 띄우기

        GameObject visualPuddle = Instantiate(waterPuddlePrefab, puddlePos, finalRot);
        GameObject navObstacle = Instantiate(navObstaclePrefab, puddlePos, finalRot);

        // 파티클 시스템이 스스로 사라지는 시간과 Obstacle 제거 시간을 맞춥니다.
        Destroy(visualPuddle, puddleDuration);
        Destroy(navObstacle, puddleDuration); 
    }

    IEnumerator Reload()
    {
        if (isReloading) yield break;

        isReloading = true;
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("isReloading", true);
        }

        // 발사 사운드 재생
        if (audioSource != null && reloadSound != null)
        {
            audioSource.PlayOneShot(reloadSound); 
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

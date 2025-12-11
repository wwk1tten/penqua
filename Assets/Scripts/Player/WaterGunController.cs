using UnityEngine;
using System.Collections;


public class WaterGunController : MonoBehaviour
{
    [Header("카메라 & 총구")]
    public Camera mainCamera;
    public Transform gunMuzzle;
    [Header("무기 장착")]
    public bool hasWeapon = false;      // 무기 획득 여부
    public GameObject handGunModel;     // 손에 있는 물총 모델 (RightHand 자식)
    public GameObject backGunModel;     // 등에 있는 물총 모델 (Spine 자식)

    [Header("발사 설정")]
    public float shootRange = 50f;
    public float fireRate = 10f; // 초당 발사 횟수
    private float nextFireTime = 0f;

    [Header("물탱크 시스템")]
    public float maxWater = 100f;
    public float waterConsumptionRate = 20f; // 초당 물 소모량
    public float puddleConsumptionRate = 50f; // 웅덩이 생성시 물 소모량
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
        if (!hasWeapon)
        {
            if(handGunModel) handGunModel.SetActive(false);
            if(backGunModel) backGunModel.SetActive(false);
        }
        else 
        {
            // 이미 있다면 등에 멘 상태로 시작
            if(handGunModel) handGunModel.SetActive(false);
            if(backGunModel) backGunModel.SetActive(true);
        }

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
        // [신규] 무기가 없으면 아무 로직도 실행하지 않음
        if (!hasWeapon) return;

        // [신규] 무기 모델 교체 로직 (조준 여부에 따라)
        HandleWeaponModel();

        // 재장전 중일 때는 모든 행동 막음
        if (isReloading) return;

        // 재장전 시도
        if (Input.GetKeyDown(KeyCode.R) && isNearWaterSource && currentWater < maxWater)
        {
            StartCoroutine(Reload());
            return; 
        }

        // [변경] 발사 조건: 우클릭(조준) 중일 때 + 좌클릭 시에만 발사되도록 변경
        // (만약 '지향사격'을 허용하려면 Input.GetMouseButton(1) 조건을 빼세요)
        if (Input.GetMouseButton(1) && Input.GetMouseButton(0) && currentWater > 0)
        {
            currentWater -= waterConsumptionRate * Time.deltaTime;

            if (Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + 1f / fireRate;
                Shoot();
            }

            if (waterStreamEffect != null)
            {
                if (!waterStreamEffect.isPlaying) waterStreamEffect.Play();
                AdjustParticleDirection();
            }

            if (Time.time >= nextPuddleTime)
            {
                nextPuddleTime = Time.time + puddleSpawnInterval;
                CheckAndCreatePuddle();
            }
        }
        else
        {
            if (waterStreamEffect != null && waterStreamEffect.isPlaying)
            {
                waterStreamEffect.Stop();
            }
        }

        currentWater = Mathf.Clamp(currentWater, 0, maxWater);
    }

    

    void HandleWeaponModel()
    {
        // 우클릭 중인지 확인 (StarterAssets라면 _input.aim 사용)
        bool isAiming = Input.GetMouseButton(1); 

        if (isAiming)
        {
            // 조준 중 -> 손에 든 모델 ON, 등에 멘 모델 OFF
            if(handGunModel && !handGunModel.activeSelf) handGunModel.SetActive(true);
            if(backGunModel && backGunModel.activeSelf) backGunModel.SetActive(false);
        }
        else
        {
            // 평상시 -> 손에 든 모델 OFF, 등에 멘 모델 ON
            if(handGunModel && handGunModel.activeSelf) handGunModel.SetActive(false);
            if(backGunModel && !backGunModel.activeSelf) backGunModel.SetActive(true);
            
            // 조준을 풀면 파티클도 즉시 꺼야 함
            if (waterStreamEffect != null && waterStreamEffect.isPlaying)
            {
                waterStreamEffect.Stop();
            }
        }
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
                SoundEmitter.MakeSound(transform.position, 5f);
            }
        }
    }

    void AdjustParticleDirection() // 파티클 방향을 조절하는 함수
    {
        // 화면 중앙에서 레이 발사
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        Vector3 targetPoint;

        // 레이가 어딘가에 맞았다면 그 지점을 목표로 함
        if (Physics.Raycast(ray, out RaycastHit hit, shootRange))
        {
            targetPoint = hit.point;
        }
        else // 아무것도 맞지 않았다면, 최대 사거리의 지점을 목표로 함
        {
            targetPoint = ray.GetPoint(shootRange);
        }

        // 총구 위치에서 목표 지점을 향하는 방향 계산
        Vector3 direction = targetPoint - waterStreamEffect.transform.position;
        
        // 파티클 시스템의 Transform을 해당 방향으로 회전시킴
        waterStreamEffect.transform.rotation = Quaternion.LookRotation(direction);
    }
    
    void CheckAndCreatePuddle() // 물웅덩이 생성만 담당하는 별도 함수
    {
        if (currentWater < puddleConsumptionRate) return;
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
    
        if (Physics.Raycast(ray, out RaycastHit hit, shootRange))
        {
            if (hit.collider.CompareTag("Floor"))
            {
                // 표면의 기울기를 확인. normal.y가 1에 가까울수록 평평한 바닥.
                // 0.7f 이상이면 충분히 평평하다고 간주.
                if (hit.normal.y >= 0.7f) {
                    // 이전 물웅덩이와의 거리가 충분히 멀 때만 생성
                    if (Vector3.Distance(hit.point, lastPuddlePosition) >= puddleMinDistance){
                        bool created = CreateWaterPuddle(hit.point, hit.normal);
                        if (created)
                        {
                            currentWater -= puddleConsumptionRate;
                            lastPuddlePosition = hit.point;
                        }
                        // 웅덩이 사운드 재생
                        if (audioSource != null && puddleSplashSound != null)
                        {
                            audioSource.PlayOneShot(puddleSplashSound); 
                        }
                        SoundEmitter.MakeSound(transform.position, 5f);

                        CreateWaterPuddle(hit.point, hit.normal);
                        lastPuddlePosition = hit.point;  
                    }
                }
            }
        }
    }

    bool CreateWaterPuddle(Vector3 position, Vector3 normal)
    {
        if (waterPuddlePrefab == null || navObstaclePrefab == null) return false;
        
        if (currentWater < puddleConsumptionRate) return false;

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

        return true;
    }

    public void PickupWaterGun()  // 외부에서 호출할 무기 획득 함수
    {
        hasWeapon = true;
        Debug.Log("물총을 획득했습니다!");
        
        // 획득 즉시 등에 멘 모습 보여주기
        if(backGunModel) backGunModel.SetActive(true);
        if(handGunModel) handGunModel.SetActive(false);
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
        SoundEmitter.MakeSound(transform.position, 5f);

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

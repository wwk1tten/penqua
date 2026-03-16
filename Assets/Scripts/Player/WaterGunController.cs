using UnityEngine;
using System.Collections;

public class WaterGunController : MonoBehaviour
{
    [Header("카메라 & 총구")]
    public Camera mainCamera;
    public Transform gunMuzzle;

    [Header("무기 장착")]
    public bool hasWeapon = false;
    public GameObject handGunModel;

    [Header("캐릭터 회전")]
    public Transform characterBody; // 회전시킬 캐릭터 바디 Transform 직접 지정
    public float aimRotationSpeed = 720f;

    [Header("발사 및 물탱크 설정")]
    public float shootRange = 50f;
    public float fireRate = 10f;
    public float maxWater = 100f;
    public float waterConsumptionRate = 20f;
    public float reloadTime = 2.0f;
    private float currentWater;
    private float nextFireTime = 0f;
    private bool isReloading = false;
    private bool isNearWaterSource = false;

    [Header("물웅덩이 생성")]
    public GameObject waterPuddlePrefab;
    public GameObject navObstaclePrefab;
    public float puddleConsumptionRate = 50f;
    public float puddleSpawnInterval = 0.1f;
    public float puddleMinDistance = 0.5f;
    public float puddleDuration = 10f;
    private float nextPuddleTime = 0f;
    private Vector3 lastPuddlePosition = Vector3.zero;

    [Header("VFX & SFX")]
    public ParticleSystem waterStreamEffect;
    public Vector3 streamRotationOffset = new(-90f, 0f, 0f); // 파티클 이미션 축 보정
    public GameObject waterSplashEffect;
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip guardHitSound;
    public AudioClip puddleSplashSound;

    private Animator playerAnimator;
    private int animIDisReloading;

    void Start(){
        InitializeWeapon();
        currentWater = maxWater;
        
        playerAnimator = GetComponentInParent<Animator>();
        animIDisReloading = Animator.StringToHash("isReloading");
    }

    void Update(){
        if (!hasWeapon) return;

        if (isReloading) return;

        // 1. 장전 처리
        if (Input.GetKeyDown(KeyCode.R) && isNearWaterSource && currentWater < maxWater){
            StartCoroutine(ReloadRoutine());
            return;
        }

        // 2. 발사 처리 (좌클릭)
        if (Input.GetMouseButton(0) && currentWater > 0){
            HandleShooting();
        }
        else{
            StopShootingVFX();
        }
    }

    // ==========================================
    // 핵심 로직 분리 영역
    // ==========================================

    private void HandleShooting()
    {
        // 캐릭터를 카메라 수평 방향으로 회전
        RotateCharacterTowardCamera();

        // 물 지속 소모
        currentWater = Mathf.Clamp(currentWater - (waterConsumptionRate * Time.deltaTime), 0, maxWater);

        // ★ 최적화: 매 프레임 한 번만 레이캐스트를 쏴서 발사, 파티클, 웅덩이가 공유합니다.
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        bool hasHit = Physics.Raycast(ray, out RaycastHit hitInfo, shootRange);

        // 시각 효과 재생 및 방향 조절
        PlayShootingVFX(ray, hasHit, hitInfo);

        // 실제 타격(데미지) 판정 주기 확인
        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + (1f / fireRate);
            ProcessHit(hasHit, hitInfo);
        }

        // 물웅덩이 생성 주기 확인
        if (Time.time >= nextPuddleTime)
        {
            nextPuddleTime = Time.time + puddleSpawnInterval;
            TryCreatePuddle(hasHit, hitInfo);
        }
    }

    private void ProcessHit(bool hasHit, RaycastHit hit)
    {
        if (!hasHit) return;

        // 타격 지점 물보라
        if (waterSplashEffect != null)
        {
            Vector3 incomingDir = gunMuzzle != null
                ? (hit.point - gunMuzzle.position).normalized
                : -hit.normal;
            Quaternion splashRot = Quaternion.LookRotation(Vector3.Reflect(incomingDir, hit.normal));
            Instantiate(waterSplashEffect, hit.point, splashRot);
        }

        PlaySound(shootSound);

        // NPC 타격 판정 (태그 대신 컴포넌트로 탐색 - 콜라이더가 자식에 있어도 동작)
        GuardPatrol guard = hit.collider.GetComponentInParent<GuardPatrol>();
        if (guard != null)
        {
            guard.TakeWaterDamage(1f, hit.point);
            PlaySound(guardHitSound);
            SoundEmitter.MakeSound(transform.position, 5f);
        }
    }

    private void TryCreatePuddle(bool hasHit, RaycastHit hit)
    {
        // 레이캐스트가 바닥에 안 맞았거나, 물이 부족하거나, 기울기가 가파르면 취소
        if (!hasHit || !hit.collider.CompareTag("Floor")) return;
        if (currentWater < puddleConsumptionRate || hit.normal.y < 0.7f) return;

        // 이전 웅덩이와 너무 가까우면 취소
        if (Vector3.Distance(hit.point, lastPuddlePosition) < puddleMinDistance) return;

        // 웅덩이 생성 로직
        Quaternion alignToSurface = Quaternion.FromToRotation(Vector3.up, hit.normal);
        Quaternion randomSpin = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        Vector3 puddlePos = hit.point + hit.normal * 0.01f;

        GameObject visualPuddle = Instantiate(waterPuddlePrefab, puddlePos, alignToSurface * randomSpin);
        GameObject navObstacle = Instantiate(navObstaclePrefab, puddlePos, alignToSurface * randomSpin);

        Destroy(visualPuddle, puddleDuration);
        Destroy(navObstacle, puddleDuration);

        // 물 소모 및 상태 업데이트
        currentWater -= puddleConsumptionRate;
        lastPuddlePosition = hit.point;
        
        PlaySound(puddleSplashSound);
        SoundEmitter.MakeSound(transform.position, 5f);
    }

    private void RotateCharacterTowardCamera()
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.01f) return;

        if (characterBody == null) return;

        Quaternion targetRot = Quaternion.LookRotation(flatForward);
        characterBody.rotation = Quaternion.RotateTowards(characterBody.rotation, targetRot, aimRotationSpeed * Time.deltaTime);
    }

    // ==========================================
    // 시각 및 사운드 효과 유틸리티
    // ==========================================

    private void PlayShootingVFX(Ray ray, bool hasHit, RaycastHit hit)
    {
        if (waterStreamEffect == null) return;

        Vector3 startPos = gunMuzzle != null ? gunMuzzle.position : transform.position;
        Vector3 targetPoint = hasHit ? hit.point : ray.GetPoint(shootRange);

        Quaternion shootRot = Quaternion.LookRotation(targetPoint - startPos) * Quaternion.Euler(streamRotationOffset);
        waterStreamEffect.transform.SetPositionAndRotation(startPos, shootRot);

        if (!waterStreamEffect.isPlaying) waterStreamEffect.Play();
    }

    private void StopShootingVFX()
    {
        if (waterStreamEffect != null && waterStreamEffect.isPlaying)
            waterStreamEffect.Stop();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // ==========================================
    // 기타 로직 (장전, 트리거, 외부 호출)
    // ==========================================

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        playerAnimator?.SetBool(animIDisReloading, true);
        
        PlaySound(reloadSound);
        SoundEmitter.MakeSound(transform.position, 5f);

        yield return new WaitForSeconds(reloadTime);

        currentWater = maxWater;
        playerAnimator?.SetBool(animIDisReloading, false);
        isReloading = false;
    }

    private void InitializeWeapon()
    {
        mainCamera = mainCamera ?? Camera.main;
        if (waterStreamEffect != null) waterStreamEffect.Stop();
        if (handGunModel != null) handGunModel.SetActive(hasWeapon);
    }

    public void PickupWaterGun()
    {
        hasWeapon = true;
        if (handGunModel != null) handGunModel.SetActive(true);
        Debug.Log("물총 획득 완료!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WaterSource")) isNearWaterSource = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("WaterSource")) isNearWaterSource = false;
    }

    public float GetWaterRatio() => currentWater / maxWater;
}
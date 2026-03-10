using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("게임 목표 설정 (캡슐)")]
    public int totalCapsulesToCollect = 3;
    private HashSet<CapsuleType> collectedCapsuleTypes = new HashSet<CapsuleType>();
    private int releasedCount = 0;

    [Header("연결된 시스템 (선택 사항)")] 
    [Tooltip("비상구 문 오브젝트 연결")]
    public ExitDoorController exitDoorController;

    [Header("UI 및 이펙트 설정")]
    public GameObject clearPanel;
    public TMP_Text capsuleCountText;
    public ParticleSystem capsuleParticle;
    
    [Header("데미지 연출")]
    public CanvasGroup damageOverlayGroup; 
    public UnityEngine.UI.Image damageFlashImage;
    public float flashDuration = 0.2f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // GameManager는 보통 씬마다 따로 두거나 파괴하지 않는데, 
            // 현재 구조에서는 DontDestroyOnLoad 유지.
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ==========================================
    // 1. 게임 진행 로직 (수집 및 방생)
    // ==========================================
    
    // 플레이어가 캡슐을 "먹었을 때" 호출됨 (PlayerInventory 혹은 CapsuleController에서 호출)
    public void OnCapsuleCollected(CapsuleType capsuleID)
    {
        // HashSet을 사용하면 중복 검사(.Contains)가 List보다 훨씬 빠르고 깔끔합니다.
        if (collectedCapsuleTypes.Add(capsuleID)) 
        {
            Debug.Log($"캡슐 {capsuleID} 획득! (총 {collectedCapsuleTypes.Count}개)");
            
            // 비상구 전구 갱신
            if (exitDoorController != null)
            {
                exitDoorController.UpdateLights(collectedCapsuleTypes.Count);
            }

            // 파티클 재생
            if (capsuleParticle != null)
            {
                capsuleParticle.Stop(); 
                capsuleParticle.Play(); 
            }
        }
    }

    // 플레이어가 캡슐을 물웅덩이 등에 "풀어줬을 때" 호출됨
    public void OnAnimalReleased()
    {
        releasedCount++;
        Debug.Log($"동물 방생 성공! ({releasedCount}/{totalCapsulesToCollect}마리)");

        // 목표치를 채웠는지 확인
        if (CheckWinCondition())
        {
            Debug.Log("탈출 조건 만족! 비상구가 완전히 개방됩니다.");
            // 탈출구를 물리적으로 여는 명령을 내립니다.
            if (exitDoorController != null)
            {
                // exitDoorController.OpenDoorPhysics(); // 해당 스크립트에 이 함수를 만들어야 함
            }
        }
    }

    // 승리 조건 검사
    public bool CheckWinCondition()
    {
        // 기획에 따라 '수집' 기준인지 '방생' 기준인지 선택하세요.
        // 현재는 '방생(releasedCount)' 기준으로 작성되었습니다.
        return releasedCount >= totalCapsulesToCollect; 
    }

    // 방생한 캡슐(동물) 갯수
    public int GetRemainingCapsuleCount()
    {
        return totalCapsulesToCollect - releasedCount;
    }

    // ==========================================
    // 2. 씬 전환 및 게임 제어 (UI)
    // ==========================================

    public void GameClear()
    {
        if (clearPanel != null) clearPanel.SetActive(true);
        Time.timeScale = 0f;

        // 마우스 잠금 해제 (UI 클릭을 위해)
        Cursor.lockState = CursorLockMode.None; 
        Cursor.visible = true;
    }

    public void Replay()
    {
        Time.timeScale = 1f;
        // 기존 상태 초기화 로직이 필요할 수 있습니다.
        releasedCount = 0;
        collectedCapsuleTypes.Clear();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene"); // 실제 타이틀 씬 이름으로 변경하세요.
    }

    // ==========================================
    // 3. 시각적 연출 (Damage, Flash)
    // ==========================================

    public void UpdateVignette(int currentHealth, int maxHealth)
    {
        if (damageOverlayGroup == null) return;

        float healthPercent = (float)currentHealth / maxHealth;
        // 체력이 100%일 때 alpha 0, 0%일 때 alpha 1
        damageOverlayGroup.alpha = 1f - healthPercent;
    }

    public void TriggerDamageFlash()
    {
        if (damageFlashImage != null)
        {
            StartCoroutine(FlashRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        Color color = damageFlashImage.color;
        color.a = 0.5f; 
        damageFlashImage.color = color;

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0.5f, 0f, elapsed / flashDuration);
            damageFlashImage.color = color;
            yield return null;
        }

        color.a = 0f;
        damageFlashImage.color = color;
    }
}
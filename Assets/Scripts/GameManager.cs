using UnityEngine;
using System.Collections.Generic; 
using UnityEngine.UI; 
using TMPro;
using Image = UnityEngine.UI.Image;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class GameManager : MonoBehaviour
{
    [Header("게임 목표 설정")]
    public int totalCapsulesToCollect = 3; // 목표 개수
    public List<string> collectedCapsuleIDs = new List<string>(); // 획득한 캡슐 ID 목록
    public int releasedCount = 0;
    [Header("연결된 시스템")] 
    public ExitDoorController exitDoorController;
    public static GameManager Instance { get; private set; }
    public TMP_Text capsuleCountText; // 캡슐 카운트
    public GameObject ClearPanel;
    public CanvasGroup damageOverlayGroup; 
    public Image damageFlashImage; // (선택) 맞을 때 번쩍일 빨간 화면
    [Header("타격 연출 설정")]
    public Color flashColor = new Color(1f, 1f, 1f, 0.5f); // 흰색 반투명 추천
    public float flashDuration = 0.2f;
    [Header("캡슐")]
    public ParticleSystem CapsuleParticle;
   
    void Awake()
    {
        // 싱글톤 인스턴스 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // 캡슐이 수집되었을 때 호출될 함수
    
    public void OnCapsuleCollected(string capsuleID)
    {
        if (!collectedCapsuleIDs.Contains(capsuleID))
        {
            collectedCapsuleIDs.Add(capsuleID);
            Debug.Log($"capsule {capsuleID} collected! (total {collectedCapsuleIDs.Count})");
            
            //  비상구 전구 갱신하기 
            if (exitDoorController != null)
            {
                exitDoorController.UpdateLights(collectedCapsuleIDs.Count);
            }

            if (CapsuleParticle != null)
            {
        
                CapsuleParticle.Stop(); // 혹시 재생 중이면 멈추고
                CapsuleParticle.Play(); // 펑!
            }
        }
    }

    // 2. 캡슐을 물에 "풀어줬을 때" (미션 진행)
    public void OnAnimalReleased()
    {
        releasedCount++; // 방생 카운트 증가
        Debug.Log($"동물 방생! ({releasedCount}마리째)");

        // ★ 불 켜는 로직은 여기서 뺍니다. (이미 켜져 있으니까)
        
        // 대신 승리 조건만 체크
        if (releasedCount >= totalCapsulesToCollect)
        {
            Debug.Log("탈출 조건 만족! 문이 완전히 개방됩니다.");
            // 여기서 문이 물리적으로 열리는 애니메이션(OpenDoor)을 실행하면 됩니다.
             if (exitDoorController != null)
            {
                // 불 켜는 거 말고, "문 여는 함수"를 따로 만들어서 호출
                // exitDoorController.OpenDoorPhysics(); 
            }
        }
    }
    public void Replay(){
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Time.timeScale = 1f;
    }

    public void BackToTitle(){
        SceneManager.LoadScene("TitleScene");
    }
    
    // 탈출 조건을 확인하는 함수
    public bool CheckWinCondition()
    {
        return collectedCapsuleIDs.Count >= 3;
    }

    public void GameClear(){
        if (ClearPanel != null) ClearPanel.SetActive(true);
        Time.timeScale = 0f;

        UnityEngine.Cursor.lockState = CursorLockMode.None; // 커서를 자유롭게 풀어줌
        UnityEngine.Cursor.visible = true;
    }

    public void UpdateVignette(int currentHealth, int maxHealth)
    {
        // 1. 체력 비율 계산 (0.0 ~ 1.0)
        float healthPercent = (float)currentHealth / maxHealth;

        // 2. 비네팅 투명도 계산 (체력이 적을수록 불투명해짐)
        // 체력 100% -> alpha 0 (안 보임)
        // 체력 0% -> alpha 1 (완전 진하게 보임)
        float targetAlpha = 1f - healthPercent;

        // 3. 부드럽게 적용하지 않고 즉시 적용 (반응성을 위해)
        if (damageOverlayGroup != null)
        {
            damageOverlayGroup.alpha = targetAlpha;
        }
    }

    public void TriggerDamageFlash()
    {
        if (damageFlashImage != null)
        {
            StartCoroutine(FlashRoutine());
        }
    }

    IEnumerator FlashRoutine()
    {
        // 빨간색 확 켜기
        Color color = damageFlashImage.color;
        color.a = 0.5f; // 반투명하게
        damageFlashImage.color = color;

        // 서서히 사라지기
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0.5f, 0f, elapsed / flashDuration);
            damageFlashImage.color = color;
            yield return null;
        }

        // 확실하게 끄기
        color.a = 0f;
        damageFlashImage.color = color;
    }
}

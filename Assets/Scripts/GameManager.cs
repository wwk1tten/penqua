using UnityEngine;
using System.Collections.Generic; 
using UnityEngine.UI; 
using TMPro;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("게임 목표 설정")]
    public int totalCapsulesToCollect = 3; // 목표 개수
    public List<string> collectedCapsuleIDs = new List<string>(); // 획득한 캡슐 ID 목록
    [Header("UI 연결")]
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
        }
    }

    public void Replay(){
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToTitle(){
        SceneManager.LoadScene("TitleScene");
    }
    public void ShowInventoryStatus()
    {
        if(capsuleCountText != null){
            capsuleCountText.text = $"total {collectedCapsuleIDs.Count}collected";
        }
    }
    
    // 탈출 조건을 확인하는 함수
    public bool CheckWinCondition()
    {
        return collectedCapsuleIDs.Count >= 3;
    }

    public void GameClear(){
        if (ClearPanel != null) ClearPanel.SetActive(true);
        Time.timeScale = 0f;
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

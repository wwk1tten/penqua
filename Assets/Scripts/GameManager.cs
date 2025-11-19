using UnityEngine;
using System.Collections.Generic; 
using UnityEngine.UI; 
using TMPro;

public class GameManager : MonoBehaviour
{
    // 싱글톤 패턴: 어디서든 GameManager.Instance로 접근 가능
    public static GameManager Instance { get; private set; }

    // 수집된 캡슐의 ID를 저장할 Set (중복 방지)
    public HashSet<string> collectedCapsuleIDs = new HashSet<string>();
    
    // UI (선택 사항)
    public TMP_Text capsuleCountText;

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
            Debug.Log($"캡슐 {capsuleID} 수집! (총 {collectedCapsuleIDs.Count}개)");
            
            // UI 업데이트
            UpdateUI();
        }
    }
    
    // 탈출 조건을 확인하는 함수
    public bool CheckWinCondition()
    {
        return collectedCapsuleIDs.Count >= 3;
    }
    
    void UpdateUI()
    {
        if (capsuleCountText != null)
        {
            capsuleCountText.text = $"{collectedCapsuleIDs.Count} / 3 remaining";
        }
    }
}

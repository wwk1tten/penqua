using UnityEngine;
using System.Collections.Generic; 
using UnityEngine.UI; 
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public HashSet<string> collectedCapsuleIDs = new HashSet<string>(); // 수집된 캡슐의 ID를 저장할 Set (중복 방지)
    
    public int totalCapsulesToCollect = 3; 
    public TMP_Text capsuleCountText; // 캡슐 카운트
    public Image[] hearts; // 인스펙터에서 하트 이미지 3개 연결
    public Sprite fullHeart;
    public Sprite emptyHeart; // 비어있는 하트 이미지 (선택 사항)

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

    public void GameClear(){
        
    }

    public void UpdateHearts(int currentHealth)
    {
        // 방어 코드: 체력이 음수면 0으로 처리
        if (currentHealth < 0) currentHealth = 0;
        // 방어 코드: 체력이 최대 하트 개수보다 많으면 그에 맞춤
        if (currentHealth > hearts.Length) currentHealth = hearts.Length;

        for (int i = 0; i < hearts.Length; i++)
        {
            // 1. 하트 이미지가 보이도록 켭니다. (방법1을 쓰다가 넘어왔을 때 꺼져있을 수 있음)
            hearts[i].enabled = true;

            if (i < currentHealth)
            {
                // 현재 체력보다 작으면 -> 꽉 찬 하트
                hearts[i].sprite = fullHeart;
            }
            else
            {
                // 현재 체력보다 크거나 같으면 -> 빈 하트
                hearts[i].sprite = emptyHeart;
            }
        }
    }
}

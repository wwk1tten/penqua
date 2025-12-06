using UnityEngine;

public class EscapeZone : MonoBehaviour
{
    [Header("설정")]
    public GameObject lockedEffect; // 빨간불 (평소)
    public GameObject unlockedEffect; // 초록불 (조건 달성 시)
    public string notReadyMessage = "친구들을 두고 갈 순 없어!"; // UI 메시지 내용

    private bool isUnlocked = false;

    void Update()
    {
        // 최적화를 위해 매 프레임 체크하기보다, GameManager에서 이벤트로 받는 게 좋지만
        // 테스트 단계에선 Update에서 체크해도 무방합니다.
        
        // 조건 달성 시 시각적 변화 (빨간불 -> 초록불)
        if (!isUnlocked && GameManager.Instance.CheckWinCondition())
        {
            isUnlocked = true;
            if(lockedEffect) lockedEffect.SetActive(false);
            if(unlockedEffect) unlockedEffect.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance.CheckWinCondition())
            {
                Debug.Log("탈출 성공! 미션 클리어!");
                // 게임 클리어 UI 호출
                GameManager.Instance.GameClear(); 
            }
            else
            {
                // 남은 개수 계산 (하드코딩 제거)
                int totalGoals = GameManager.Instance.totalCapsulesToCollect; 
                int current = GameManager.Instance.collectedCapsuleIDs.Count;
                int remaining = totalGoals - current;

                Debug.Log($"아직 {remaining}명의 친구가 갇혀있어!");
                
                GameManager.Instance.ShowInventoryStatus();
            }
        }
    }
}
using UnityEngine;

public class EscapeZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 들어온 것이 플레이어인지 확인
        if (other.CompareTag("Player"))
        {
            // GameManager를 통해 승리 조건 확인
            if (GameManager.Instance.CheckWinCondition())
            {
                Debug.Log("탈출 성공! 축하합니다!");
                // 여기에 다음 레벨을 로드하거나, 게임 클리어 UI를 띄우는 코드 추가
            }
            else
            {
                int remaining = 3 - GameManager.Instance.collectedCapsuleIDs.Count;
                Debug.Log($"아직 {remaining}명의 친구들을 더 구해야 해!");
            }
        }
    }
}

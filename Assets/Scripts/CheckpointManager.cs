using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    // 씬 시작 시 플레이어가 리스폰할 초기 위치를 저장합니다.
    private Vector3 initialRespawnPosition;
    
    // 플레이어가 마지막으로 활성화한 체크포인트 위치를 저장합니다.
    private Vector3 lastCheckpointPosition;

    void Awake()
    {
        // 🚨 중요: 씬 시작 시 플레이어가 있어야 할 초기 위치를 설정하세요.
        // 예를 들어, 씬에 'InitialSpawnPoint'라는 GameObject를 두고 그 위치를 사용하거나, 
        // 플레이어의 시작 위치를 그대로 사용합니다.
        
        // 현재는 이 스크립트가 부착된 오브젝트의 위치를 초기 위치로 설정하겠습니다.
        initialRespawnPosition = transform.position; 
        lastCheckpointPosition = initialRespawnPosition;
    }

    /// <summary>
    /// 플레이어가 새로운 체크포인트를 통과할 때 이 함수를 호출합니다.
    /// </summary>
    /// <param name="newPosition">새로운 체크포인트의 월드 위치</param>
    public void UpdateCheckpoint(Vector3 newPosition)
    {
        lastCheckpointPosition = newPosition;
        Debug.Log($"새로운 체크포인트 설정됨: {lastCheckpointPosition}");
    }

    /// <summary>
    /// 플레이어가 사망했을 때 현재 리스폰 위치를 반환합니다.
    /// </summary>
    /// <returns>마지막으로 기록된 체크포인트 위치</returns>
    public Vector3 GetRespawnPosition()
    {
        return lastCheckpointPosition;
    }
}
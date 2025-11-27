using System;
using UnityEngine;

public class CheckPointManager : MonoBehaviour
{
    public Transform initialSpawnPoint; 
    private Vector3 lastCheckpointPosition;
    public static CheckPointManager Instance { get; private set; }

    void Awake()
    {
        // 싱글톤 초기화 로직
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 이미 매니저가 있으면 나 자신을 파괴 (중복 방지)
            return;
        }
        Instance = this;
        // 씬이 넘어가도 파괴되지 않게 하려면 아래 줄 주석 해제
        // DontDestroyOnLoad(gameObject);

        // ... 기존 초기화 로직 ...
        if (initialSpawnPoint != null)
        {
            lastCheckpointPosition = initialSpawnPoint.position;
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if(player) lastCheckpointPosition = player.transform.position;
        }
    }

    public void UpdateCheckpoint(Vector3 newPosition)
    {
        lastCheckpointPosition = newPosition;
        Debug.Log($"체크포인트 저장 완료: {lastCheckpointPosition}");
    }

    public Vector3 GetRespawnPosition()
    {
        return lastCheckpointPosition;
    }

    public static implicit operator CheckPointManager(CheckPointObject v)
    {
        throw new NotImplementedException();
    }
}
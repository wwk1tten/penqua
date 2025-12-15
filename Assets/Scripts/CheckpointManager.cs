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
            Destroy(gameObject); 
            return;
        }
        Instance = this;

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
using UnityEngine;
using System.Collections.Generic;

public class FollowerManager : MonoBehaviour
{
    public static FollowerManager Instance { get; private set; }

    public List<Transform> followPoints; // [설정 필요] 1단계에서 만든 'Point'들을 담을 리스트
    private Queue<Transform> availablePoints; // 비어있는 '지정석'을 관리할 큐

    void Awake()
    {
        Instance = this;
        
        // 시작할 때 모든 포인트를 '사용 가능' 상태로 큐에 넣음
        availablePoints = new Queue<Transform>(followPoints);
    }
    
    // 동물이 땅에 도착했을 때 이 함수를 호출하여 자리를 요청
    public Transform RequestFollowPoint()
    {
        if (availablePoints.Count > 0)
        {
            Transform point = availablePoints.Dequeue(); // 큐에서 자리 하나를 꺼내줌
            Debug.Log($"자리를 배정했습니다: {point.name}");
            return point;
        }
        
        Debug.LogWarning("남아있는 자리가 없습니다!");
        return null; // 모든 자리가 찼으면 null 반환
    }
    
    // (선택) 동물이 사라질 때 자리를 반납하는 기능
    public void ReturnFollowPoint(Transform point)
    {
        if (point != null)
        {
            availablePoints.Enqueue(point);
        }
    }
}

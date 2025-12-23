using System.Collections.Generic;
using UnityEngine;

public class CapsuleFriendController : MonoBehaviour
{
    public static List<CapsuleFriendController> ActiveSwimmers = new List<CapsuleFriendController>();
    [Header("수영 설정")]
    public float moveSpeed = 2.0f;
    public float rotSpeed = 1.0f;
    public float swimRange = 5.0f; // 수조 크기에 맞춰 조절

    private Vector3 targetPos;
    private Vector3 startPos;

    void OnEnable()
    {
        ActiveSwimmers.Add(this);
    }

    // ★ 꺼지거나 파괴될 때 명단에서 제거
    void OnDisable()
    {
        ActiveSwimmers.Remove(this);
    }
    void Start()
    {
        // 태어난 위치(수조 안)를 기준으로 배회함
        startPos = transform.position;
        GetNewTarget();
        
        // 중력 끄기 (물속이니까)
        if(TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.useGravity = false;
            rb.linearDamping = 1f; // 물 저항
        }
        
        // 네비게이션 끄기 (물속에서 자유이동)
        if(TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
        {
            agent.enabled = false;
        }
    }

    void Update()
    {
        // 1. 목표 지점으로 부드럽게 이동
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        // 2. 회전 (물고기처럼)
        Vector3 dir = targetPos - transform.position;
        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotSpeed * Time.deltaTime);
        }

        // 3. 목표 도착하면 새 목표 설정
        if (Vector3.Distance(transform.position, targetPos) < 0.5f)
        {
            GetNewTarget();
        }
    }

    void GetNewTarget()
    {
        // 시작 위치 주변 랜덤한 곳 찍기
        targetPos = startPos + Random.insideUnitSphere * swimRange;
    }
}
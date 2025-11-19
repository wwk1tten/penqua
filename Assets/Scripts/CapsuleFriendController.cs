using UnityEngine;
using UnityEngine.AI; // NavMeshAgent를 사용하기 위해 추가
using System.Collections;
using Bitgem.VFX.StylisedWater;

[RequireComponent(typeof(NavMeshAgent), typeof(Rigidbody))]
public class CapsuleFriendController : MonoBehaviour
{
    // 상태 정의
    private enum AnimalState { GrowingInWater, SwimmingToShore, WalkingOnLand }
    private AnimalState currentState;

    [Header("성장 및 이동")]
    public Vector3 targetScale = new Vector3(3, 3, 3);
    public float growDuration = 5f;
    public float walkSpeed = 3.5f;
    public float rotationSpeed = 5f;
    public float swimSpeed = 3f; // 땅으로 헤엄쳐가는 속도

    private Transform followTarget;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private bool isFullyGrown = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        
        // [핵심 1] 시작할 때는 NavMeshAgent를 반드시 비활성화
        agent.enabled = false;
        
        // 초기 상태 설정 및 코루틴 시작
        currentState = AnimalState.GrowingInWater;
        StartCoroutine(GrowAndSwimToShore());
    }

    void Update()
    {
        // 땅 위를 걷고 있는 상태일 때
        if (currentState == AnimalState.WalkingOnLand && agent != null && agent.enabled)
        {
            // 1. 이동: NavMeshAgent가 목표 지점(followTarget)으로 이동을 담당
            if (followTarget != null)
            {
                agent.SetDestination(followTarget.position);
            }

            // 2. 회전: 스크립트가 직접 플레이어를 바라보도록 회전을 담당
            if (followTarget != null)
            {
                // 플레이어를 향하는 방향 벡터를 계산 (Y축은 무시하여 기울어지지 않게)
                Vector3 directionToPlayer = followTarget.position - transform.position;
                directionToPlayer.y = 0;

                // 방향이 0이 아닐 경우에만 회전 실행 (오류 방지)
                if (directionToPlayer.sqrMagnitude > 0)
                {
                    // 해당 방향을 바라보는 목표 회전값을 계산
                    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                    
                    // 현재 회전값에서 목표 회전값으로 부드럽게 회전
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
        }
    }
    
    IEnumerator GrowAndSwimToShore()
    {
        // --- 상태 1: 물 속에서 성장 ---
        Vector3 initialScale = transform.localScale;
        float timer = 0;
        
        while(timer < growDuration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(initialScale, targetScale, timer / growDuration);
            yield return null;
        }
        isFullyGrown = true;
        Debug.Log("다 자랐다! 이제 땅으로 가야지!");
        
        // --- 상태 2: 가장 가까운 땅으로 헤엄치기 ---
        currentState = AnimalState.SwimmingToShore;
        
        // 가장 가까운 NavMesh(땅) 위치 찾기
        Vector3 shorePosition;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 10f, NavMesh.AllAreas))
        {
            shorePosition = navHit.position;
        }
        else
        {
            Debug.LogError("주변에 땅이 없어! 여기서 멈춘다.");
            yield break; // 코루틴 중단
        }
        
        // 땅에 도착할 때까지 부드럽게 이동
        while(Vector3.Distance(transform.position, shorePosition) > 0.5f)
        {
            transform.position = Vector3.MoveTowards(transform.position, shorePosition, swimSpeed * Time.deltaTime);
            transform.LookAt(new Vector3(shorePosition.x, transform.position.y, shorePosition.z)); // 땅을 바라보며 이동
            yield return null;
        }

        // --- 상태 3: 땅 도착 후 전환 ---
        Debug.Log("땅에 도착했다! 이제부터 따라다녀야지!");
        currentState = AnimalState.WalkingOnLand;
        
        rb.isKinematic = true; // 물리 효과 정지
        GetComponent<WateverVolumeFloater>().enabled = false; // 물에 뜨는 기능 정지
        agent.enabled = true; // NavMeshAgent 활성화!

        agent.stoppingDistance = 0; // 지정석에 정확히 도착해야 하므로 0으로 설정
        agent.baseOffset = 0;
        agent.updateRotation = false;
        
        followTarget = FollowerManager.Instance.RequestFollowPoint();
    }

    void OnDestroy()
    {
        FollowerManager.Instance.ReturnFollowPoint(followTarget);
    }
}

using UnityEngine;

public class EnemyFOV : MonoBehaviour
{
    [Header("Vision Settings")]
    [Tooltip("시야 범위 (거리)")]
    public float detectionRange = 20f;
    
    [Tooltip("시야각 (도)")]
    public float fieldOfView = 90f;
    
    [Tooltip("플레이어 레이어")]
    public LayerMask playerLayer;
    
    [Tooltip("장애물 레이어")]
    public LayerMask obstacleLayer;
    
    [Header("Debug")]
    [Tooltip("감지 시 색상 (감지함/감지안함)")]
    public Color detectionColor = Color.green;
    public Color noDetectionColor = Color.red;
    
    private bool canSeePlayer = false;
    private Transform playerTransform;

    void Start()
    {
        // 플레이어 찾기
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (playerTransform == null)
        {
            Debug.LogWarning("Player를 찾을 수 없습니다!");
        }
    }

    void Update()
    {
        if (playerTransform == null) return;
        
        DetectPlayer();
    }

    /// <summary>
    /// 플레이어 감지 (범위 + 시야각 + 장애물 확인)
    /// </summary>
    private void DetectPlayer()
    {
        canSeePlayer = false;
        
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;
        
        // 1단계: 범위 확인
        if (distanceToPlayer > detectionRange)
        {
            return;
        }
        
        // 2단계: 시야각 확인 (FOV)
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer.normalized);
        
        if (angleToPlayer > fieldOfView / 2f)
        {
            return;
        }
        
        // 3단계: 장애물 확인 (Raycast)
        if (IsPlayerBlocked(directionToPlayer, distanceToPlayer))
        {
            return;
        }
        
        // 모든 조건 통과: 플레이어 감지!
        canSeePlayer = true;
        Debug.Log($"[{gameObject.name}] 플레이어 감지됨! 거리: {distanceToPlayer:F2}m, 각도: {angleToPlayer:F2}°");
    }

    /// <summary>
    /// 플레이어가 장애물에 의해 차단되었는지 확인
    /// </summary>
    private bool IsPlayerBlocked(Vector3 directionToPlayer, float distanceToPlayer)
    {
        // Raycast로 장애물 확인
        if (Physics.Raycast(transform.position, directionToPlayer.normalized, 
            out RaycastHit hit, distanceToPlayer, obstacleLayer))
        {
            // 플레이어보다 가까운 장애물이 있으면 차단됨
            if (hit.distance < distanceToPlayer - 0.1f)
            {
                Debug.Log($"플레이어가 {hit.collider.gameObject.name}에 의해 차단됨");
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// 공개 메서드: 플레이어를 볼 수 있는지 확인
    /// </summary>
    public bool CanSeePlayer()
    {
        return canSeePlayer;
    }

    #region Gizmos (시각화)

    /// <summary>
    /// 항상 보이는 기즈모
    /// </summary>
    void OnDrawGizmos()
    {
        // 감지 범위 원 (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    /// <summary>
    /// 선택했을 때만 보이는 기즈모 (FOV 시각화)
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;
        
        // 플레이어 감지 여부에 따라 색상 변경
        Gizmos.color = canSeePlayer ? detectionColor : noDetectionColor;
        
        // 1) 플레이어 방향 (거리)
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;
        
        // 플레이어로의 광선
        Gizmos.DrawLine(transform.position, playerTransform.position);
        Gizmos.DrawWireSphere(playerTransform.position, 0.3f);
        
        // 2) 시야각 시각화 (FOV 범위)
        float halfFOV = fieldOfView / 2f;
        
        // 왼쪽 가장자리
        Vector3 leftEdge = Quaternion.Euler(0, -halfFOV, 0) * transform.forward * detectionRange;
        Gizmos.color = new Color(0, 1, 0, 0.3f); // 투명 초록색
        Gizmos.DrawLine(transform.position, transform.position + leftEdge);
        
        // 오른쪽 가장자리
        Vector3 rightEdge = Quaternion.Euler(0, halfFOV, 0) * transform.forward * detectionRange;
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + rightEdge);
        
        // FOV 범위 호 그리기
        DrawFOVArc(halfFOV, detectionRange);
        
        // 3) 플레이어 상태 텍스트
        Gizmos.color = canSeePlayer ? Color.green : Color.red;
        
        // 감지 조건 텍스트 (Scene 뷰에서 볼 수 있음)
        float angleToPlayer = playerTransform != null ? 
            Vector3.Angle(transform.forward, directionToPlayer.normalized) : 0;
    }

    /// <summary>
    /// FOV 시야각 호 그리기
    /// </summary>
    void DrawFOVArc(float halfFOV, float range)
    {
        int segments = 20;
        float angleStep = halfFOV * 2f / segments;
        
        Gizmos.color = new Color(0, 1, 0, 0.2f); // 투명 초록색
        
        for (int i = 0; i < segments; i++)
        {
            float currentAngle = -halfFOV + (angleStep * i);
            float nextAngle = currentAngle + angleStep;
            
            Vector3 currentDir = Quaternion.Euler(0, currentAngle, 0) * transform.forward * range;
            Vector3 nextDir = Quaternion.Euler(0, nextAngle, 0) * transform.forward * range;
            
            Gizmos.DrawLine(transform.position + currentDir, transform.position + nextDir);
        }
    }

    #endregion
}

using UnityEngine;

public class SoundEmitter : MonoBehaviour
{
    // 정적 함수로 어디서든 호출 가능
    public static void MakeSound(Vector3 position, float range)
    {
        // 1. 범위 내의 모든 충돌체 검출 (Enemy 레이어만 검출하면 성능 더 좋음)
        Collider[] colliders = Physics.OverlapSphere(position, range);
        
        foreach (var col in colliders)
        {
            // [수정됨] GuardHearing -> GuardPatrol로 변경
            // 우리가 만든 GuardPatrol 스크립트의 OnSoundHeard를 호출하기 위함
            GuardPatrol guard = col.GetComponent<GuardPatrol>();
            
            if (guard != null)
            {
                guard.OnSoundHeard(position);
            }
        }
        
        // 디버그용 (에디터에서만 보임)
        DrawSoundRange(position, range);
    }
    
    // (기존 코드 유지)
    public static void DrawSoundRange(Vector3 position, float range)
    {
        Debug.DrawRay(position, Vector3.up * range, Color.yellow, 2f);
        Debug.DrawRay(position, Vector3.down * range, Color.yellow, 2f);
        Debug.DrawRay(position, Vector3.left * range, Color.yellow, 2f);
        Debug.DrawRay(position, Vector3.right * range, Color.yellow, 2f);
    }
}
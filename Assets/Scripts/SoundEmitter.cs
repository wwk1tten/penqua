using UnityEngine;

public class SoundEmitter : MonoBehaviour
{
    // 정적 함수: 어디서든 SoundEmitter.MakeSound(...)로 호출 가능
    public static void MakeSound(Vector3 position, float range)
    {
        // ==========================================
        // 1. 시각 효과 (Visual) - 파동 그리기
        // ==========================================
        if (SoundVisualManager.Instance != null)
        {
            // 범위가 4m 이상이면 '뛰는 소리'로 간주하고 노란색 표시
            bool isRunning = range > 4.0f; 
            SoundVisualManager.Instance.SpawnRipple(position, range, isRunning);
        }

        // ==========================================
        // 2. 논리 판정 (Logic) - 적에게 알리기
        // ==========================================
        // 최적화를 위해 'Enemy' 레이어만 검출 (레이어 이름 확인 필수!)
        int layerMask = 1 << LayerMask.NameToLayer("Guard"); // 혹은 "Guard"
        
        Collider[] colliders = Physics.OverlapSphere(position, range, layerMask);
        
        foreach (var col in colliders)
        {
            // 적 스크립트에 소리 들었다고 알림
            GuardPatrol guard = col.GetComponent<GuardPatrol>();
            if (guard != null) guard.OnSoundHeard(position);
            
            // 범용성을 위해 SendMessage 사용 (느리지만 편함, 나중에 최적화 가능)
            //col.SendMessage("OnSoundHeard", position, SendMessageOptions.DontRequireReceiver);
        }

        // 디버그용 (에디터 씬뷰에서만 보임)
        #if UNITY_EDITOR
        Debug.DrawRay(position, Vector3.up * 2, Color.red, 1f);
        #endif
    }
}
using UnityEngine;

public class SoundEmitter : MonoBehaviour
{
    public static void MakeSound(Vector3 position, float range, bool isSwimming = false)
    {
        // 1. 시각 효과 (기존 코드 유지)
        if (SoundVisualManager.Instance != null)
        {
            bool isRunning = range > 4.0f; 
            SoundVisualManager.Instance.SpawnRipple(position, range, isRunning, isSwimming);
        }

        // 2. 논리 판정
        int layerMask = 1 << LayerMask.NameToLayer("Guard");
        Collider[] colliders = Physics.OverlapSphere(position, range, layerMask);
        
        foreach (var col in colliders)
        {
            // [수정 1] 먼저 '청각 센서(GuardHearing)'가 있는지 확인
            GuardHearing hearing = col.GetComponent<GuardHearing>();
            
            if (hearing != null)
            {
                // Hearing 컴포넌트가 있으면 거기로 전달 (범위 range 포함)
                hearing.OnSoundHeard(position, range);
            }
            else
            {
                // [수정 2] Hearing 컴포넌트가 없다면, 직접 GuardPatrol을 찾음 (안전장치)
                GuardPatrol guard = col.GetComponent<GuardPatrol>();
                
                if (guard != null) 
                {
                    guard.OnSoundHeard(position, range);
                }
            }
        }

        #if UNITY_EDITOR
        Debug.DrawRay(position, Vector3.up * 2, Color.red, 1f);
        #endif
    }
}
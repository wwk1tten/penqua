using UnityEngine;

public class SoundEmitter : MonoBehaviour
{
    public static void MakeSound(Vector3 position, float range)
    {
        Collider[] colliders = Physics.OverlapSphere(position, range);
        
        Debug.Log($"OverlapSphere 감지된 콜라이더 수: {colliders.Length}");
        
        foreach (var col in colliders)
        {
            Debug.Log($"감지된 오브젝트: {col.gameObject.name}");
            
            GuardHearing hearing = col.GetComponent<GuardHearing>();
            if (hearing != null)
            {
                Debug.Log($"GuardHearing 발견! {col.gameObject.name}에게 소리 전달");
                hearing.OnSoundHeard(position);
            }
            else
            {
                Debug.Log($"{col.gameObject.name}에 GuardHearing 컴포넌트 없음");
            }
        }
    }
    
    // Scene 뷰에서 범위 시각화
    public static void DrawSoundRange(Vector3 position, float range)
    {
        Debug.DrawRay(position, Vector3.up * range, Color.yellow, 2f);
        Debug.DrawRay(position, Vector3.down * range, Color.yellow, 2f);
        Debug.DrawRay(position, Vector3.left * range, Color.yellow, 2f);
        Debug.DrawRay(position, Vector3.right * range, Color.yellow, 2f);
    }
}

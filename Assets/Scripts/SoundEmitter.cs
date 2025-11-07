using UnityEngine;

public class SoundEmitter : MonoBehaviour
{
    public static void MakeSound(Vector3 position, float range)
    {
        Collider[] colliders = Physics.OverlapSphere(position, range);
        foreach (var col in colliders)
        {
            GuardHearing hearing = col.GetComponent<GuardHearing>();
            if (hearing != null)
            {
                hearing.OnSoundHeard(position);
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

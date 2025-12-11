using UnityEngine;

public class SoundVisualManager : MonoBehaviour
{
    public static SoundVisualManager Instance;

    [Header("파티클 연결")]
    public ParticleSystem rippleParticle; // 아까 만든 VFX_SoundRipples

    [Header("색상 설정")]
    public Color walkColor = new Color(0f, 1f, 1f, 0.5f); // 걷기: 청록색
    public Color runColor = new Color(1f, 0.9f, 0f, 0.8f);  // 뛰기: 노란색

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /// <param name="range">소리 범위 (반지름)</param>
    /// <param name="isRunning">뛰는 중인가?</param>
    public void SpawnRipple(Vector3 position, float range, bool isRunning)
    {
        if (rippleParticle == null) return;

        // 파티클 1개를 발사하기 위한 설정(Params) 만들기
        var emitParams = new ParticleSystem.EmitParams();

        // 1. 위치: 바닥(y)보다 아주 살짝 위에 (겹침 방지)
        emitParams.position = position + Vector3.up * 0.05f;

        // 2. 크기: 지름 = 반지름(range) * 2
        emitParams.startSize = range * 2f;

        // 3. 색상: 뛰면 노랑, 걸으면 파랑
        emitParams.startColor = isRunning ? runColor : walkColor;

        // 4. 발사! (1개)
        rippleParticle.Emit(emitParams, 1);
    }
}
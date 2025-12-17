using UnityEngine;

public class SoundVisualManager : MonoBehaviour
{
    public static SoundVisualManager Instance;

    [Header("파티클 연결")]
    public ParticleSystem rippleParticle;

    [Header("색상 설정")]
    public Color walkColor = new Color(0f, 1f, 1f, 0.5f); // 걷기: 청록색
    public Color runColor = new Color(1f, 0.9f, 0f, 0.8f);  // 뛰기: 노란색

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /// <param name="range">소리 범위 (반지름)</param>
    /// <param name="isRunning">뛰는 중인가?</param>
    public void SpawnRipple(Vector3 position, float range, bool isRunning, bool isSwimming)
    {     
        if (rippleParticle == null) {
            return;
        }

        if (!rippleParticle.isPlaying) {
            rippleParticle.Play();
        }

        // 파티클 1개를 발사하기 위한 설정(Params) 만들기
        var emitParams = new ParticleSystem.EmitParams();
    
        // 지상: 바닥에 깔려야 함 -> 바닥 좌표(y=0 근처)로 보정
        emitParams.position = Vector3.up * 0.05f;
        emitParams.startSize = range * 2f;

        emitParams.startColor = isRunning ? runColor : walkColor;

        // 발사
        Debug.Log($"[Emit] 발사! 위치: {emitParams.position}, 크기: {emitParams.startSize}");
        rippleParticle.Emit(emitParams, 1);
    }
}
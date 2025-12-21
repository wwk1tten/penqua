using UnityEngine;

public class GlowPulse : MonoBehaviour
{
    [Header("설정")]
    public Color glowColor = Color.yellow; // 빛나는 색깔 (노랑 or 파랑 추천)
    public float speed = 2.0f;             // 깜빡이는 속도
    public float minIntensity = 0.5f;      // 최소 밝기
    public float maxIntensity = 2.0f;      // 최대 밝기

    private Renderer rend;
    private Material mat;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            mat = rend.material;
            // 중요: Emission 기능을 켜줍니다.
            mat.EnableKeyword("_EMISSION");
        }
    }

    void Update()
    {
        if (mat == null) return;

        // 시간에 따라 밝기가 웅~ 웅~ 변함 (PingPong)
        float emission = Mathf.PingPong(Time.time * speed, maxIntensity - minIntensity) + minIntensity;
        
        // 최종 색상 계산 (기본색 * 밝기)
        Color finalColor = glowColor * Mathf.LinearToGammaSpace(emission);

        // 재질에 적용
        mat.SetColor("_EmissionColor", finalColor);
    }
}
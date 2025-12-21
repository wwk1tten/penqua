using UnityEngine;
using System.Collections;

public class BubblePopUI : MonoBehaviour
{
    private Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    // 오브젝트가 활성화(SetActive true) 될 때마다 실행
    void OnEnable()
    {
        StartCoroutine(PopUpEffect());
    }

    IEnumerator PopUpEffect()
    {
        float timer = 0f;
        transform.localScale = Vector3.zero; // 0에서 시작

        // 1. 띠용~ 하고 커지기 (Overshoot 효과)
        while (timer < 0.4f)
        {
            timer += Time.deltaTime;
            float t = timer / 0.4f;
            
            // 사인 그래프를 이용한 탄성 효과 (커졌다가 살짝 작아짐)
            float scaleProgress = Mathf.Sin(t * Mathf.PI) * 0.3f + t;
            transform.localScale = originalScale * scaleProgress;            
            yield return null;
        }
        
        // 2. 둥둥 떠있기 (Idle)
        transform.localScale = originalScale;
    }

    void Update()
    {
        // 둥실둥실 (위아래로 살짝)
        float bobbing = Mathf.Sin(Time.time * 3f) * 0.1f;
        transform.localPosition = new Vector3(0, 0.5f + bobbing, 0); 
        // 0.5f는 환기구 바닥에서의 높이
    }
}
using UnityEngine;

public class CheckPointObject : MonoBehaviour
{
    [Header("Settings")]
    // 활성화되었을 때 보여줄 모델이나 파티클 (예: 깃발이 올라감, 불이 켜짐)
    public GameObject activeVisual; 
    public GameObject inactiveVisual; // 꺼져있을 때 모습 (선택사항)
    
    [Header("Audio")]
    public AudioClip activationSound; // 띠링~ 하는 효과음
    private AudioSource audioSource;

    private bool isActivated = false;
    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // 초기 상태 설정
        if(activeVisual) activeVisual.SetActive(false);
        if(inactiveVisual) inactiveVisual.SetActive(true);
    }

    void OnTriggerEnter(Collider other)
    {
        // 이미 켜진 체크포인트면 무시
        if (isActivated) return;

        if (other.CompareTag("Player"))
        {
            CheckPointManager.Instance.UpdateCheckpoint(transform.position); 
        }
    }

    private void ActivateCheckpoint(Vector3 playerPos)
    {
        isActivated = true;

        // 1. 매니저에게 위치 저장 요청 (싱글톤 사용)
        if (CheckPointManager.Instance != null)
        {
            CheckPointManager.Instance.UpdateCheckpoint(transform.position);
        }
        else
        {
            Debug.LogWarning("체크포인트 매니저가 씬에 없습니다!");
        }

        // 2. 시각적 변화 (귀여운 연출!)
        if(inactiveVisual) inactiveVisual.SetActive(false); // 꺼진 모습 숨기기
        if(activeVisual) activeVisual.SetActive(true);   // 켜진 모습 보이기 (깃발 펄럭!)

        // 3. 청각적 피드백
        if (audioSource && activationSound)
        {
            audioSource.PlayOneShot(activationSound);
        }
    }
}
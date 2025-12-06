using UnityEngine;

public class ExitDoorController : MonoBehaviour
{
    [Header("전구 설정 (0, 1, 2 순서대로)")]
    public GameObject[] redLights;   // 빨간불 오브젝트 3개
    public GameObject[] greenLights; // 초록불 오브젝트 3개

    [Header("문 설정")]
    public GameObject doorObject;    // 열릴 문 오브젝트
    public AudioSource audioSource;
    public AudioClip lightOnSound;   // 띠링! 소리
    public AudioClip doorOpenSound;  // 끼이익 소리

    private bool isOpen = false;

    void Start()
    {
        // 시작할 때 초기화: 모두 빨간불 켜고, 초록불 끄기
        UpdateLights(0);
    }

    // GameManager가 호출할 함수
    public void UpdateLights(int currentCount)
    {
        // 1. 전구 상태 갱신
        for (int i = 0; i < 3; i++)
        {
            // 현재 모은 개수보다 작으면 초록불(켜짐), 아니면 빨간불(대기)
            if (i < currentCount)
            {
                // 이미 켜진 상태가 아닐 때만 소리 재생 (중복 방지)
                if (!greenLights[i].activeSelf && audioSource != null) 
                    audioSource.PlayOneShot(lightOnSound);

                redLights[i].SetActive(false);
                greenLights[i].SetActive(true);
            }
            else
            {
                redLights[i].SetActive(true);
                greenLights[i].SetActive(false);
            }
        }

        // 2. 3개를 다 모았으면 문 열기
        if (currentCount >= 3 && !isOpen)
        {
            OpenDoor();
        }
    }

    void OpenDoor()
    {
        isOpen = true;
        Debug.Log("문이 열립니다!");
        
        // 문 열리는 소리
        if (audioSource != null) audioSource.PlayOneShot(doorOpenSound);

        // 문 오브젝트를 비활성화하거나 애니메이션 재생
        // 간단하게 사라지게 하거나, 회전시키는 코드 추가 가능
        // doorObject.SetActive(false); // 문 삭제
        
        // 또는 애니메이터 사용 시:
        // doorObject.GetComponent<Animator>().SetTrigger("Open");
    }
}
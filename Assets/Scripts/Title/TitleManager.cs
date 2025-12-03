using UnityEngine;
using UnityEngine.SceneManagement; // 씬 이동을 위해 필수
using System.Collections;

public class TitleManager : MonoBehaviour
{
    [Header("설정")]
    public string gameSceneName = "Stage1"; // 이동할 게임 씬 이름
    public GameObject startUI; // 'Press Any Key' 텍스트
    public AudioClip splashSound; // 첨벙 소리
    public AudioSource audioSource;
    public GameObject transitionEffect; // (선택) 물방울 파티클

    private bool isStarting = false;

    void Update()
    {
        // 이미 시작 중이면 입력 무시
        if (isStarting) return;

        // 아무 키나 누르거나 마우스 클릭 시
        if (Input.anyKeyDown)
        {
            StartCoroutine(StartGameRoutine());
        }
    }

    IEnumerator StartGameRoutine()
    {
        isStarting = true;

        // 1. UI 숨기기 (깔끔하게)
        if(startUI != null) startUI.SetActive(false);

        // 2. 효과음 재생 (첨벙!)
        if(audioSource != null && splashSound != null)
        {
            audioSource.PlayOneShot(splashSound);
        }

        // 3. (선택) 파티클 효과나 카메라 무빙이 있다면 여기서 실행
        if(transitionEffect != null) transitionEffect.SetActive(true);

        // 4. 소리가 들릴 틈을 줌 (1~2초 대기)
        yield return new WaitForSeconds(1.5f);

        // 5. 씬 이동
        SceneManager.LoadScene(gameSceneName);
    }
}
using UnityEngine;
using Cinemachine; 
using System.Collections;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("시네머신 카메라")]
    public CinemachineVirtualCamera underwaterCam; // Priority 11
    public CinemachineVirtualCamera surfaceCam;    // Priority 10
    
    [Header("타격감 (Impulse)")]
    public CinemachineImpulseSource shakeSource;   // 쉐이크 발생기

    [Header("UI & Audio")]
    public GameObject logoPanel;
    public GameObject mainMenuUI;
    public GameObject settingPanel;
    public AudioSource bgmSource;
    public AudioClip splashSound;
    public AudioLowPassFilter lowPassFilter;
    public ParticleSystem bubbleBurst;

    private bool isStarted = false;

    void Update()
    {
        if (!isStarted && Input.anyKeyDown)
        {
            StartGameSequence();
        }
    }

    void StartGameSequence()
    {
        isStarted = true;

        // 1. 임팩트 (소리 + 쉐이크 + 파티클)
        bgmSource.PlayOneShot(splashSound);
        if(bubbleBurst != null) bubbleBurst.Play();
        if(logoPanel != null) logoPanel.SetActive(false);

        // ★ 시네머신 쉐이크 발생 (한 줄로 끝!)
        // GenerateImpulse()를 호출하면 설정된 진동이 카메라에 전달됨
        if(shakeSource != null) shakeSource.GenerateImpulse();

        // 2. 카메라 전환 (핵심!)
        // 물속 카메라의 우선순위를 낮추면 -> 자동으로 물 밖 카메라(10)가 활성화됨
        // 시네머신 Brain이 설정된 시간(2초) 동안 알아서 부드럽게 이동시킴
        underwaterCam.Priority = 9; 

        // 3. 오디오 필터 및 UI 처리는 코루틴으로 타이밍 맞춤
        StartCoroutine(AudioAndUIProcess());
    }

    IEnumerator AudioAndUIProcess()
    {
        // 카메라가 올라가는 시간(2초) 동안 소리도 같이 맑아지게
        float duration = 2.0f; // Brain의 Blend Time과 맞추세요
        float elapsed = 0f;
        float startCutoff = lowPassFilter.cutoffFrequency;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            lowPassFilter.cutoffFrequency = Mathf.Lerp(startCutoff, 22000f, elapsed / duration);
            yield return null;
        }

        lowPassFilter.enabled = false;
        if(mainMenuUI != null){
            logoPanel.SetActive(false);
            mainMenuUI.SetActive(true);
        }

    }

    public void OnPlayButtonClicked(){
        SceneManager.LoadScene("SampleScene");
    }

    public void OnSettingButtonClicked(){
        if(settingPanel != null){
            settingPanel.SetActive(true);
        }
    }

}
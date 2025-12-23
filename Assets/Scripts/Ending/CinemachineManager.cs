using UnityEngine;
using Cinemachine;
using System.Collections;

public class CinemachineManager : MonoBehaviour
{
    public static CinemachineManager Instance;

    [Header("설정")]
    public int activePriority = 20;  // 켜질 때 우선순위
    public int inactivePriority = 0; // 꺼질 때 우선순위

    // 현재 활성화된 카메라와 코루틴을 기억하는 변수
    private CinemachineVirtualCamera currentActiveCam;
    private Coroutine currentRoutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ★ 모든 카메라 요청은 이 함수 하나로 통일됩니다.
    public void SwitchToCamera(CinemachineVirtualCamera newCam, float duration)
    {
        if (newCam == null) return;

        // 1. 이미 돌고 있는 연출 코루틴이 있다면 강제 종료
        if (currentRoutine != null) StopCoroutine(currentRoutine);

        // 2. 기존에 켜져 있던 카메라가 있다면 즉시 끄기 (여기가 핵심!)
        if (currentActiveCam != null && currentActiveCam != newCam)
        {
            currentActiveCam.Priority = inactivePriority;
            currentActiveCam.LookAt = null; 
            currentActiveCam.Follow = null;
        }

        // 3. 새 연출 시작
        currentRoutine = StartCoroutine(FocusRoutine(newCam, duration));
    }

    // (동물처럼 움직이는 타겟을 비출 때 쓰는 함수)
    public void FocusOnTarget(Transform target, float duration, CinemachineVirtualCamera dynamicCam)
    {
        if (dynamicCam == null) return;

        // 타겟 설정 후 위 함수 호출
        dynamicCam.LookAt = target;
        dynamicCam.Follow = target;
        SwitchToCamera(dynamicCam, duration);
    }

    IEnumerator FocusRoutine(CinemachineVirtualCamera cam, float duration)
    {
        // 켜기
        currentActiveCam = cam;
        cam.Priority = activePriority;

        // 대기
        yield return new WaitForSeconds(duration);

        // 끄기
        cam.Priority = inactivePriority;
        cam.LookAt = null;
        cam.Follow = null;
        currentActiveCam = null;
        currentRoutine = null;
    }
}
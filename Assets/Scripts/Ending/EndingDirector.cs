using UnityEngine;
using Cinemachine;
using System.Collections;

public class EndingDirector : MonoBehaviour
{
    [Header("시네머신 카메라")]
    public CinemachineVirtualCamera divingCamera; // 위에서 아래를 찍는 카메라
    public float originalPriority = 10; // 평소 카메라 우선순위
    public float highPriority = 20;     // 다이빙 카메라 우선순위

    [Header("슬로우 모션")]
    public float slowMotionFactor = 0.3f; // 0.3배속 (아주 느리게)
    public float duration = 3.0f;         // 슬로우 모션 지속 시간 (낙하 예상 시간)

    private bool isActivated = false;

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어가 다이빙대에 서면 발동
        if (other.CompareTag("Player") && !isActivated)
        {
            StartCoroutine(PlayDivingSequence());
        }
    }

    IEnumerator PlayDivingSequence()
    {
        isActivated = true;
        Debug.Log("🎬 다이빙 연출 시작!");

        // 1. 카메라 전환 (탑뷰 앵글)
        if (divingCamera != null)
        {
            divingCamera.Priority = (int)highPriority;
        }

        // 2. 시간 느리게 (슬로우 모션)
        Time.timeScale = slowMotionFactor;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // 물리 연산 동기화 (필수)

        // 3. 지정된 시간(낙하 시간)만큼 대기
        // (슬로우 모션 상태이므로 WaitForSecondsRealtime을 써야 실제 시간으로 셉니다)
        yield return new WaitForSecondsRealtime(duration);

        // 4. 연출 종료 (물에 빠진 후)
        // 카메라는 그대로 유지할지, 되돌릴지 선택 (여기선 유지)
        // Time.timeScale = 1.0f; // 엔딩 크레딧이나 다음 씬으로 넘길 거면 굳이 안 돌려도 됨
    }
}
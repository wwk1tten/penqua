using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        // 메인 카메라 찾기
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        // 1. UI가 항상 카메라 정면을 바라보게 회전
        transform.forward = mainCamera.transform.forward;

        // (선택 사항) 만약 UI가 문 모델 안에 파묻히는 게 싫다면?
        // 아래 주석을 풀면 항상 카메라 쪽으로 살짝 튀어나와서 그려집니다.
        transform.position = transform.parent.position + Vector3.up * 2.0f; // 예시
    }
}
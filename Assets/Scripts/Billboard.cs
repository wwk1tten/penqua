using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform mainCam;

    void Start()
    {
        // 메인 카메라 찾기
        if (Camera.main != null)
        {
            mainCam = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (mainCam != null)
        {
            // 내 정면(forward)을 카메라의 정면과 일치시킴
            // (LookAt보다 이 방식이 덜 울렁거리고 깔끔함)
            transform.forward = mainCam.forward;
        }
    }
}
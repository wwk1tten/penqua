using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        // 항상 카메라 방향을 바라봄
        transform.forward = mainCam.transform.forward;
    }
}
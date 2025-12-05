using UnityEngine;
using UnityEngine.UI;

public class GuardIcon : MonoBehaviour
{
    [Header("설정")]
    public GameObject iconPrefab; // 캔버스에 띄울 아이콘 프리팹 (! 그림)
    public Canvas mainCanvas; // 메인 HUD 캔버스
    public Transform headPos; // 적의 머리 위치 (아이콘 띄울 위치)
    public float yOffset = 1.0f; // 머리보다 얼마나 위에 띄울지

    private GameObject myIcon; // 생성된 내 아이콘
    private RectTransform iconRect;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        
        // 1. 게임 시작 시 메인 캔버스에 내 아이콘을 하나 생성함
        if(mainCanvas != null && iconPrefab != null)
        {
            myIcon = Instantiate(iconPrefab, mainCanvas.transform);
            iconRect = myIcon.GetComponent<RectTransform>();
            myIcon.SetActive(false); // 평소엔 꺼둠
        }
    }

    void Update()
    {
        if (myIcon == null) return;

        // 2. 적이 화면에 보일 때만 아이콘 위치 갱신 (최적화)
        // (여기서는 간단히 항상 갱신하는 코드로 짭니다)
        
        // 3D 좌표(적 머리) -> 2D 화면 좌표로 변환
        Vector3 screenPos = mainCam.WorldToScreenPoint(headPos.position + Vector3.up * yOffset);

        // 적이 카메라 뒤에 있으면 아이콘 숨김
        if (screenPos.z < 0) 
        {
            myIcon.SetActive(false);
        }
        else
        {
            // 아이콘 위치 이동
            myIcon.transform.position = screenPos;
        }
    }
    
    // 적이 죽거나 사라질 때 아이콘도 같이 삭제
    void OnDestroy()
    {
        if (myIcon != null) Destroy(myIcon);
    }

    // 외부에서 아이콘 켜고 끄는 함수
    public void SetAlert(bool isActive)
    {
        if(myIcon != null) myIcon.SetActive(isActive);
    }
}
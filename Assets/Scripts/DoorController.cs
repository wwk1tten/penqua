using UnityEngine;
using StarterAssets; // Player 스크립트 참조용

public class DoorController : MonoBehaviour
{
    // 문 종류 선택 (인스펙터에서 설정)
    public enum DoorType 
    { 
        NeedKeycard,      // 키카드가 있어야 열림 (직접 상호작용)
        NeedMasterKey,    // 창고 열쇠(마스터키)가 있어야 열림 (직접 상호작용)
        RemoteOnly        // 플레이어가 못 엽니다. LSS 콘솔 신호로만 열림
    }

    [Header("문 설정")]
    public DoorType doorType = DoorType.NeedKeycard; // 기본값
    public bool isOpened = false;

    [Header("UI 연결")]
    public GameObject bubbleLocked;    // 잠김(열쇠 필요) 아이콘
    public GameObject bubblePressE;    // E 버튼 아이콘

    [Header("모델 연결")]
    public GameObject closedDoorModel; // 닫힌 문
    public GameObject openDoorModel;   // 열린 문

    private bool isPlayerInZone = false;
    private ThirdPersonController playerScript; // 플레이어 스크립트

    void Start()
    {
        UpdateDoorVisuals(); // 시작할 때 모델 상태 동기화
        
        if (bubbleLocked != null) bubbleLocked.SetActive(false);
        if (bubblePressE != null) bubblePressE.SetActive(false);
    }

    // ★ 1. 외부(LSS 콘솔)에서 호출하는 함수 (원격 문 열기)
    public void RemoteOpen()
    {
        if (isOpened) return; // 이미 열렸으면 패스

        isOpened = true;
        UpdateDoorVisuals();
        
        Debug.Log("원격 신호 수신: 문이 열렸습니다!");
        // 효과음 재생 (철커덩!)
    }

    // ★ 2. 플레이어 직접 상호작용 (E키)
    private void Update()
    {
        // 이미 열렸거나, 원격 전용 문이면 E키 반응 안 함
        if (isOpened || doorType == DoorType.RemoteOnly) return;

        if (isPlayerInZone && Input.GetKeyDown(KeyCode.E))
        {
            TryOpenDoor();
        }
    }

    void TryOpenDoor()
    {
        if (playerScript == null) return;

        bool canOpen = false;

        // 문 타입에 따라 필요한 열쇠 확인
        switch (doorType)
        {
            case DoorType.NeedKeycard:
                // 플레이어 스크립트에 hasKeycard 변수가 있다고 가정
                // (만약 없다면 hasKeycard 부분 수정 필요)
                // if (playerScript.hasKeycard) canOpen = true; 
                canOpen = true; // 테스트용: 일단 무조건 열리게 (변수 확인 후 주석 해제하세요)
                break;

            case DoorType.NeedMasterKey:
                if (playerScript.hasWarehouseKey) canOpen = true;
                break;
        }

        if (canOpen)
        {
            isOpened = true;
            UpdateDoorVisuals();
            UpdateBubbleState(); // 문 열렸으니 UI 끄기
            Debug.Log("열쇠를 사용해 문을 열었습니다.");
        }
        else
        {
            Debug.Log("맞는 열쇠가 없습니다!");
            // 띠띠~ 실패음 재생
        }
    }

    // 문 상태에 따라 모델 껐다 켜기
    void UpdateDoorVisuals()
    {
        if (closedDoorModel != null) closedDoorModel.SetActive(!isOpened);
        if (openDoorModel != null) openDoorModel.SetActive(isOpened);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 문이 닫혀있고, 원격 전용이 아닐 때만 UI 표시
        if (other.CompareTag("Player") && !isOpened && doorType != DoorType.RemoteOnly)
        {
            isPlayerInZone = true;
            playerScript = other.GetComponent<ThirdPersonController>();
            UpdateBubbleState();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            playerScript = null;

            if (bubbleLocked != null) bubbleLocked.SetActive(false);
            if (bubblePressE != null) bubblePressE.SetActive(false);
        }
    }

    void UpdateBubbleState()
    {
        if (playerScript == null || isOpened) 
        {
            if (bubbleLocked != null) bubbleLocked.SetActive(false);
            if (bubblePressE != null) bubblePressE.SetActive(false);
            return;
        }

        bool hasTheKey = false;

        // 내가 가진 열쇠 확인
        switch (doorType)
        {
            case DoorType.NeedKeycard:
                // if (playerScript.hasKeycard) hasTheKey = true;
                hasTheKey = true; // 테스트용
                break;
            case DoorType.NeedMasterKey:
                if (playerScript.hasWarehouseKey) hasTheKey = true;
                break;
        }

        if (hasTheKey)
        {
            if (bubblePressE != null) bubblePressE.SetActive(true);
            if (bubbleLocked != null) bubbleLocked.SetActive(false);
        }
        else
        {
            if (bubblePressE != null) bubblePressE.SetActive(false);
            if (bubbleLocked != null) bubbleLocked.SetActive(true);
        }
    }
}
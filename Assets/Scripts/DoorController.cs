using UnityEngine;

public class DoorController : MonoBehaviour, IInteractable
{
    [Header("문 설정")]
    public bool isRemoteOnly = false; // true면 E키로 절대 안 열림 (LSS 콘솔 전용)
    
    // 2. DoorType enum을 과감히 버리고, 아까 만든 ItemType을 직접 사용합니다.
    [Tooltip("이 문을 여는 데 필요한 열쇠 (None이면 열쇠 없이 열림)")]
    public ItemType requiredKey = ItemType.None; 
    public bool isOpened = false;

    [Header("UI 연결")]
    public GameObject bubbleLocked;    // 잠김(열쇠 필요) 아이콘
    public GameObject bubblePressE;    // E 버튼 아이콘

    [Header("모델 연결")]
    public GameObject closedDoorModel; // 닫힌 문
    public GameObject openDoorModel;   // 열린 문

    private void Start()
    {
        UpdateDoorVisuals();
        HideBubbles();
    }

    // 외부(LSS 콘솔 등)에서 호출하는 원격 개방 함수
    public void RemoteOpen()
    {
        if (isOpened) return;

        isOpened = true;
        UpdateDoorVisuals();
        HideBubbles(); // 문이 열렸으니 버블도 치웁니다.
        
        Debug.Log("원격 신호 수신: 문이 열렸습니다!");
    }

    // 플레이어가 E키를 눌렀을 때 실행되는 인터페이스 함수
    public void Interact(GameObject player)
    {
        // 이미 열렸거나 원격 전용 문이면 무시
        if (isOpened || isRemoteOnly) return;

        // 플레이어의 인벤토리를 확인합니다.
        if (player.TryGetComponent(out PlayerInventory inv))
        {
            // 요구하는 열쇠가 아예 없거나(None), 인벤토리에 해당 열쇠가 있다면 개방
            if (requiredKey == ItemType.None || inv.HasItem(requiredKey))
            {
                isOpened = true;
                UpdateDoorVisuals();
                HideBubbles();
                Debug.Log($"{requiredKey}를 사용해 문을 열었습니다.");
            }
            else
            {
                Debug.Log($"문이 잠겨있습니다. {requiredKey}가 필요합니다!");
                // TODO: 실패음 재생
            }
        }
    }

    // 모델 상태 동기화
    private void UpdateDoorVisuals()
    {
        if (closedDoorModel != null) closedDoorModel.SetActive(!isOpened);
        if (openDoorModel != null) openDoorModel.SetActive(isOpened);
    }

    // ==========================================
    // UI 버블 처리 구역 (트리거는 UI 띄우는 용도로만 사용)
    // ==========================================
    private void OnTriggerEnter(Collider other)
    {
        if (isOpened || isRemoteOnly) return;

        if (other.CompareTag("Player") && other.TryGetComponent(out PlayerInventory inv))
        {
            UpdateBubbleState(inv);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            HideBubbles();
        }
    }

    private void UpdateBubbleState(PlayerInventory inv)
    {
        if (isOpened) return;

        // 내가 필요한 열쇠를 가지고 있는지 확인하는 논리식
        bool hasTheKey = (requiredKey == ItemType.None || inv.HasItem(requiredKey));

        if (bubblePressE != null) bubblePressE.SetActive(hasTheKey);
        if (bubbleLocked != null) bubbleLocked.SetActive(!hasTheKey);
    }

    private void HideBubbles()
    {
        if (bubbleLocked != null) bubbleLocked.SetActive(false);
        if (bubblePressE != null) bubblePressE.SetActive(false);
    }
}
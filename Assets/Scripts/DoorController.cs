using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject bubbleKeyNeeded; // 열쇠 그림 버블 (열쇠 없을 때)
    public GameObject bubblePressE;    // E 버튼 버블 (열쇠 있을 때)

    [Header("문 모델 연결")]
    public GameObject closedDoorModel; // 닫혀있는 문 (Collider 포함)
    public GameObject openDoorModel;   // 활짝 열려있는 문 프리팹

    [Header("상태")]
    public bool isOpened = false;

    private bool isPlayerInZone = false;
    private PlayerInventory playerInv; // 플레이어 인벤토리 참조

    void Start()
    {
        // 초기화: 문 닫힘, 열린 문 모델은 숨김, 버블 다 끔
        if (closedDoorModel != null) closedDoorModel.SetActive(true);
        if (openDoorModel != null) openDoorModel.SetActive(false);
        
        if (bubbleKeyNeeded != null) bubbleKeyNeeded.SetActive(false);
        if (bubblePressE != null) bubblePressE.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isOpened)
        {
            isPlayerInZone = true;
            playerInv = other.GetComponent<PlayerInventory>();

            UpdateBubbleState(); // 상황에 맞는 버블 켜기
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            playerInv = null;

            // 나갈 땐 모든 버블 끄기
            if (bubbleKeyNeeded != null) bubbleKeyNeeded.SetActive(false);
            if (bubblePressE != null) bubblePressE.SetActive(false);
        }
    }

    private void Update()
    {
        // 문이 이미 열렸으면 아무것도 안 함
        if (isOpened) return;

        // 플레이어가 근처에 있고 + 열쇠가 있고 + E를 눌렀을 때
        if (isPlayerInZone && Input.GetKeyDown(KeyCode.E))
        {
            if (playerInv != null && playerInv.hasKeycard)
            {
                OpenDoor();
            }
            else
            {
                // 열쇠 없는데 E 누르면? "잠김" 효과음 재생 (선택)
                Debug.Log("열쇠가 필요해!"); 
            }
        }
    }

    // 버블 상태 갱신 함수
    void UpdateBubbleState()
    {
        if (playerInv == null) return;

        // 기존 버블 일단 다 끄고
        if (bubbleKeyNeeded != null) bubbleKeyNeeded.SetActive(false);
        if (bubblePressE != null) bubblePressE.SetActive(false);

        // 조건 검사
        if (playerInv.hasKeycard)
        {
            // 열쇠 있음 -> E 버튼 보여주기
            if (bubblePressE != null) bubblePressE.SetActive(true);
        }
        else
        {
            // 열쇠 없음 -> 열쇠 그림 보여주기
            if (bubbleKeyNeeded != null) bubbleKeyNeeded.SetActive(true);
        }
    }

    void OpenDoor()
    {
        isOpened = true;
        Debug.Log("문이 열렸습니다!");

        // 1. 버블 숨기기
        if (bubblePressE != null) bubblePressE.SetActive(false);

        // 2. 모델 교체 (닫힌 문 끄고, 열린 문 켜기)
        if (closedDoorModel != null) closedDoorModel.SetActive(false);
        if (openDoorModel != null) openDoorModel.SetActive(true);

        // 3. 소리 재생 (선택)
        // SoundEmitter.MakeSound(...) 
    }
}
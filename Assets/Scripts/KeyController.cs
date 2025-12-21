using UnityEngine;

public class KeyController : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject bubbleUI; // 아까 만든 E버튼 버블 (자식 오브젝트)

    private bool isPlayerInZone = false;
    private PlayerInventory playerInv; // 플레이어 인벤토리 참조

    void Start()
    {
        // 시작할 때 버블 꺼두기
        if (bubbleUI != null) bubbleUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            playerInv = other.GetComponent<PlayerInventory>();

            // 가까이 오면 버블 띠용~
            if (bubbleUI != null) bubbleUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            playerInv = null;

            // 멀어지면 버블 끄기
            if (bubbleUI != null) bubbleUI.SetActive(false);
        }
    }

    private void Update()
    {
        // 범위 안이고 + E키를 눌렀다면
        if (isPlayerInZone && Input.GetKeyDown(KeyCode.E))
        {
            PickUpKey();
        }
    }

    void PickUpKey()
    {
        Debug.Log("열쇠를 주웠습니다!");

        // 1. 플레이어 주머니에 열쇠 정보 입력
        if (playerInv != null)
        {
            playerInv.GetKey(); // 아까 만든 함수 호출
        }

        // 2. 획득 효과음 (있으면)
        // AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        // 3. 열쇠 오브젝트 삭제 (세상에서 사라짐)
        Destroy(gameObject); 
    }
}
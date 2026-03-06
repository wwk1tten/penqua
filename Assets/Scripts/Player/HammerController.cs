using UnityEngine;

public class HammerController : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject bubbleUI; // E버튼 버블

    private bool isPlayerInZone = false;
    private PlayerInventory playerInv;

    void Start() { if (bubbleUI != null) bubbleUI.SetActive(false); }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            playerInv = other.GetComponent<PlayerInventory>();
            if (bubbleUI != null) bubbleUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            playerInv = null;
            if (bubbleUI != null) bubbleUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (isPlayerInZone && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("망치를 주웠습니다!");
            
            // 1. 망치 획득 처리
            if (playerInv != null) playerInv.HasItem(ItemType.Hammer);

            // 2. 삭제
            Destroy(gameObject);
        }
    }
}
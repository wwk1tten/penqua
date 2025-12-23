using UnityEngine;
using DiabolicalGames; // ★ 에셋 네임스페이스 필수

public class HammerInteraction : MonoBehaviour
{
    [Header("에셋 연결")]
    // 에셋에 포함된 파괴 스크립트를 연결합니다.
    public DestructibleObject targetObject; 

    [Header("UI 연결")]
    public GameObject bubbleHammerNeeded; // 망치 필요 아이콘
    public GameObject bubblePressE;       // E 버튼 아이콘

    private bool isBroken = false;
    private bool isPlayerInZone = false;
    private PlayerInventory playerInv;

    void Start()
    {
        // 시작할 때 UI 끄기
        if (bubbleHammerNeeded != null) bubbleHammerNeeded.SetActive(false);
        if (bubblePressE != null) bubblePressE.SetActive(false);

        // 만약 인스펙터에서 연결 안 했으면, 같은 오브젝트에서 찾기
        if (targetObject == null)
            targetObject = GetComponent<DestructibleObject>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 이미 부서졌으면 무시
        if (isBroken) return;

        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            playerInv = other.GetComponent<PlayerInventory>();
            UpdateBubbleState();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            playerInv = null;
            
            // 나갈 때 버블 끄기
            if (bubbleHammerNeeded != null) bubbleHammerNeeded.SetActive(false);
            if (bubblePressE != null) bubblePressE.SetActive(false);
        }
    }

    void Update()
    {
        if (isBroken) return;

        // 플레이어가 범위 안에 있고 E키를 눌렀을 때
        if (isPlayerInZone && Input.GetKeyDown(KeyCode.E))
        {
            if (playerInv != null && playerInv.hasHammer)
            {
                Smash();
            }
            else
            {
                Debug.Log("망치가 필요해!");
                // 띠띠~ 하는 실패 효과음 넣어도 좋음
            }
        }
        
    }

    void UpdateBubbleState()
    {
        if (playerInv == null) return;

        // 일단 다 끄고
        if (bubbleHammerNeeded != null) bubbleHammerNeeded.SetActive(false);
        if (bubblePressE != null) bubblePressE.SetActive(false);

        // 망치 유무에 따라 켜기
        if (playerInv.hasHammer)
        {
            if (bubblePressE != null) bubblePressE.SetActive(true);
        }
        else
        {
            if (bubbleHammerNeeded != null) bubbleHammerNeeded.SetActive(true);
        }
    }

    void Smash()
    {
        isBroken = true;

        // 1. UI 끄기
        if (bubblePressE != null) bubblePressE.SetActive(false);
        if (bubbleHammerNeeded != null) bubbleHammerNeeded.SetActive(false);

        // 2. ★ 핵심: 에셋 스크립트의 Break 함수 강제 호출!
        if (targetObject != null)
        {
            targetObject.Break(); 
        }
    }
}
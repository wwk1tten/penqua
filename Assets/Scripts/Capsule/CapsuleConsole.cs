using UnityEngine;
using UnityEngine.Events;
using StarterAssets;
using Cinemachine;

public class CapsuleConsole : MonoBehaviour
{
    [Header("진행 상황 (0:시작전, 1:캡슐1완료...)")]
    public int currentStage = 0;

    [Header("투하 설정")]
    public Transform dropPoint;           // 캡슐이 떨어질 천장 위치
    public GameObject capsuleShellPrefab; // 떨어질 캡슐 껍데기 프리팹 (Rigidbody 필수)
    public GameObject[] animalPrefabs;    // 단계별 소환될 동물들 (0:펭귄, 1:거북이...)
    [Header("연출용 전용 카메라")]
    public CinemachineVirtualCamera eventCam_Animal; // 동물 비추는 카메라 (Follow/LookAt 비워두기)
    public CinemachineVirtualCamera eventCam_Door;   // 경비실 문 전용 카메라 (위치 고정)
    public CinemachineVirtualCamera eventCam_Tank;   // 수조 포탈 전용 카메라 (위치 고정)

    [Header("UI 연결")]
    public GameObject bubbleCapsule1; 
    public GameObject bubbleCapsule2; 
    public GameObject bubbleCapsule3; 
    public GameObject bubblePressE;   
    public GameObject tankDoor;
    public UnityEvent onStage1Clear; 
    public UnityEvent onStage2Clear; 
    public UnityEvent onStage3Clear; 

    private bool isPlayerInZone = false;
    private PlayerInventory playerInventory;

    void Start() { UpdateBubbleState(); }

    public void OpenTank(){
        if(tankDoor != null) tankDoor.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            playerInventory = other.GetComponent<PlayerInventory>();
            UpdateBubbleState();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            playerInventory = null;
            AllBubblesOff();
        }
    }

    void Update()
    {
        if (isPlayerInZone && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Dropped!");
            TryInsertCapsule();
        }
    }

    void TryInsertCapsule()
    {
        if (playerInventory == null) return;

        // 단계별 로직
        if (currentStage == 0) // 1단계
        {
            if (playerInventory.HasCapsule(CapsuleType.Gecko))
            {
                playerInventory.UseCapsule(CapsuleType.Gecko);
                currentStage = 1;
                onStage1Clear.Invoke(); 

                // 1. 동물
                DropCapsule(0);

                // 2. 문
                if (CinemachineManager.Instance != null && eventCam_Door != null)
                {
                    StartCoroutine(DelayedFocus(eventCam_Door, 3.0f, 3.0f));
                }
            }
        }
        else if (currentStage == 1) // 2번 캡슐
        {
            if (playerInventory.HasCapsule(CapsuleType.Herring))
            {
                playerInventory.UseCapsule(CapsuleType.Herring);
                DropCapsule(1); // ★ 1번 동물(2단계) 투하
                currentStage = 2;
                onStage2Clear.Invoke(); 
            }
        }
        else if (currentStage == 2) // 3번 캡슐
        {
            if (playerInventory.HasCapsule(CapsuleType.Muskrat))
            {
                playerInventory.UseCapsule(CapsuleType.Muskrat);
                currentStage = 3;
                onStage3Clear.Invoke(); 

                DropCapsule(2); // 2번 동물(3단계) 투하

                // ★ 포탈 강제 생성
                if (EndingTrigger.Instance != null) 
                {
                    EndingTrigger.Instance.OpenPortalFromConsole();
                } 
                
                // ★ 4초 뒤에 수조 전용 카메라 비추기
                if (CinemachineManager.Instance != null && eventCam_Tank != null)
                {
                    StartCoroutine(DelayedFocus(eventCam_Tank, 3.0f, 3.0f));
                }
            }
        }
        
        UpdateBubbleState();
    }

    System.Collections.IEnumerator DelayedFocus(CinemachineVirtualCamera cam, float delay, float duration)
    {
        yield return new WaitForSeconds(delay); 
        CinemachineManager.Instance.SwitchToCamera(cam, duration);
    }

    // 캡슐 투하 함수

    void DropCapsule(int animalIndex)
    {
        GameObject shell = Instantiate(capsuleShellPrefab, dropPoint.position, dropPoint.rotation);
        PhysicalCapsule physCapsule = shell.GetComponent<PhysicalCapsule>();
        
        if (physCapsule != null && animalIndex < animalPrefabs.Length)
        {
            physCapsule.animalPrefab = animalPrefabs[animalIndex];
            physCapsule.animalCam = eventCam_Animal; // 카메라 정보 전달
        }
    }

    void UpdateBubbleState()
    {
        AllBubblesOff();
        if (playerInventory == null || currentStage >= 3) return;

        string targetID = (currentStage + 1).ToString(); 
        
        if (playerInventory.HasCapsule(CapsuleType.Gecko))
        {
            if (bubblePressE != null) bubblePressE.SetActive(true);
        }
        else 
        {
            if (currentStage == 0 && bubbleCapsule1 != null) bubbleCapsule1.SetActive(true);
            else if (currentStage == 1 && bubbleCapsule2 != null) bubbleCapsule2.SetActive(true);
            else if (currentStage == 2 && bubbleCapsule3 != null) bubbleCapsule3.SetActive(true);
        }
    }

    void AllBubblesOff()
    {
        if (bubbleCapsule1 != null) bubbleCapsule1.SetActive(false);
        if (bubbleCapsule2 != null) bubbleCapsule2.SetActive(false);
        if (bubbleCapsule3 != null) bubbleCapsule3.SetActive(false);
        if (bubblePressE != null) bubblePressE.SetActive(false);
    }
}
using UnityEngine;
using DiabolicalGames; // ★ 에셋의 네임스페이스 추가

public class BreakableWall : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject bubbleHammerNeeded;
    public GameObject bubblePressE;

    [Header("오브젝트 연결")]
    public GameObject normalWall;
    public GameObject brokenWallPrefab; // Despawn 스크립트가 붙은 프리팹이어야 함

    [Header("파괴 효과 설정 (에셋 연동)")]
    public AudioClip smashSound;      // 와장창 소리 파일
    [Range(0, 100)] public int despawnPercent = 100; // 몇 퍼센트나 사라지게 할지
    public float despawnTime = 5.0f;  // 몇 초 뒤에 사라질지
    public float soundVolume = 1.0f;  // 소리 크기

    private bool isBroken = false;
    private bool isPlayerInZone = false;
    private PlayerInventory playerInv;
    private GameObject playerObj; // 플레이어 오브젝트 기억용

    void Start()
    {
        if (normalWall != null) normalWall.SetActive(true);
        if (bubbleHammerNeeded != null) bubbleHammerNeeded.SetActive(false);
        if (bubblePressE != null) bubblePressE.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isBroken)
        {
            isPlayerInZone = true;
            playerObj = other.gameObject; // 플레이어 기억 (거리 계산용)
            playerInv = other.GetComponent<PlayerInventory>();
            UpdateBubbleState();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            playerObj = null;
            playerInv = null;
            if (bubbleHammerNeeded != null) bubbleHammerNeeded.SetActive(false);
            if (bubblePressE != null) bubblePressE.SetActive(false);
        }
    }

    void Update()
    {
        if (isBroken) return;

        if (isPlayerInZone && Input.GetKeyDown(KeyCode.E))
        {
            if (playerInv != null && playerInv.hasHammer)
            {
                SmashWall();
            }
            else
            {
                Debug.Log("망치가 필요해!");
            }
        }
    }

    void UpdateBubbleState()
    {
        if (playerInv == null) return;
        
        if (bubbleHammerNeeded != null) bubbleHammerNeeded.SetActive(false);
        if (bubblePressE != null) bubblePressE.SetActive(false);

        if (playerInv.hasHammer)
        {
            if (bubblePressE != null) bubblePressE.SetActive(true);
        }
        else
        {
            if (bubbleHammerNeeded != null) bubbleHammerNeeded.SetActive(true);
        }
    }

    // ★ 핵심: 에셋 스크립트에 명령 내리기
    void SmashWall()
    {
        isBroken = true;

        // 1. UI 및 멀쩡한 벽 끄기
        if (bubblePressE != null) bubblePressE.SetActive(false);
        if (normalWall != null) normalWall.SetActive(false);

        // 2. 부서진 조각 생성
        if (brokenWallPrefab != null)
        {
            GameObject debris = Instantiate(brokenWallPrefab, transform.position, transform.rotation);
            
            // 3. 생성된 조각에서 'Despawn' 스크립트 찾기
            Despawn despawnScript = debris.GetComponent<Despawn>();

            if (despawnScript != null)
            {
                // 에셋 스크립트에 설정값 주입 (소리, 시간, 플레이어 정보 등)
                despawnScript.SetVariables(
                    despawnPercent,    // 사라질 비율
                    despawnTime,       // 사라질 시간
                    100f,              // 거리 (Timed 모드라 크게 상관없음)
                    playerObj,         // 플레이어
                    smashSound,        // 소리
                    soundVolume,       // 볼륨
                    0.1f,              // 볼륨 변동폭
                    0.1f               // 피치 변동폭
                );

                // "시간 지나면 사라져라" 명령 시작
                despawnScript.BeginCoroutine("Timed");
            }
        }
    }
}
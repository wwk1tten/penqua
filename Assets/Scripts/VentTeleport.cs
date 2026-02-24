using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using StarterAssets;
using UnityEngine.InputSystem;

public class VentTeleport : MonoBehaviour
{
    [Header("텔레포트 설정")]
    public Transform exitPoint;   // 도착할 지점 (빈 오브젝트)
    public CanvasGroup fadePanel; // 검은 화면 패널
    public GameObject bubbleGuideObj;
    public float fadeDuration = 0.5f; // 깜빡이는 속도

    private bool isTeleporting = false; // 텔레포트 중인지 확인용
    private bool isPlayerInZone = false; // 플레이어가 범위 안에 있는지 확인용
    private GameObject playerRef; // what is this for?!

    // 플레이어카 E키 눌렀을 때 thirPersonController 스크립트에서 이 함수를 호출
    public void Interact(GameObject player) 
    {
        if (!isTeleporting)
        {
            StartCoroutine(TeleportRoutine(player));
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            playerRef = other.gameObject;

            if (bubbleGuideObj != null) bubbleGuideObj.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ResetZoneState();
        }
    }

    private void ResetZoneState()
    {
        Debug.Log("Zone State Reset");
        isPlayerInZone = false;
        playerRef = null;

        if (bubbleGuideObj != null) bubbleGuideObj.SetActive(false);
    }

    private void Update()
    {
        if (isPlayerInZone && !isTeleporting)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (playerRef != null)
                {
                    StartCoroutine(TeleportRoutine(playerRef));
                }
            }
        }
    }

    IEnumerator TeleportRoutine(GameObject player)
    {
        isTeleporting = true;
        ThirdPersonController pc = player.GetComponent<ThirdPersonController>(); 
        CharacterController cc = player.GetComponent<CharacterController>();

        // 1. 플레이어 조작 얼리기 (입력 막기)
        if (pc != null) pc.enabled = false; // 필요하면 주석 해제

        // 2. 화면 어둡게 (Fade Out)
        yield return Fade(0f, 1f);

        // 3. ★ 핵심: 텔레포트 (CharacterController 끄고 옮겨야 함!)
        if (cc != null) cc.enabled = false; // 잠깐 끄기 (물리 충돌 방지)
        
        player.transform.position = exitPoint.position;
        player.transform.rotation = exitPoint.rotation; // 출구 방향 보게 하기
        
        // TODO: 사운드 재생

        yield return new WaitForSeconds(0.2f); // 잠깐 대기 (로딩 느낌)

        if (cc != null) cc.enabled = true; // 다시 켜기

        ResetZoneState();

        yield return Fade(1f, 0f);

        // 5. 조작 풀기
        if (pc != null) pc.enabled = true;
        isTeleporting = false;
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / fadeDuration);
            yield return null;
        }
        fadePanel.alpha = endAlpha;
    }
}
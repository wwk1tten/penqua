using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using StarterAssets;
using UnityEngine.InputSystem;

public class VentTeleport : MonoBehaviour
{
    [Header("설정")]
    public Transform exitPoint;   // 도착할 지점 (빈 오브젝트)
    public CanvasGroup fadePanel; // 아까 만든 검은 화면 패널
    public GameObject bubbleGuideObj;
    public float fadeDuration = 0.5f; // 깜빡이는 속도

    private bool isTeleporting = false;
    private bool isPlayerInZone = false; // 플레이어가 범위 안에 있는지 확인용
    private GameObject playerRef;

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
        ThirdPersonController pc = player.GetComponent<ThirdPersonController>(); // 님 스크립트
        CharacterController cc = player.GetComponent<CharacterController>();

        // 1. 플레이어 조작 얼리기 (입력 막기)
        if (pc != null) pc.enabled = false; // 필요하면 주석 해제

        // 2. 화면 어둡게 (Fade Out)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadePanel.alpha = timer / fadeDuration;
            yield return null;
        }
        fadePanel.alpha = 1f;

        // 3. ★ 핵심: 텔레포트 (CharacterController 끄고 옮겨야 함!)
        if (cc != null) cc.enabled = false; // 잠깐 끄기 (물리 충돌 방지)
        
        player.transform.position = exitPoint.position;
        player.transform.rotation = exitPoint.rotation; // 출구 방향 보게 하기
        
        // 소리 재생 (기어가는 소리 등)은 여기서!
        // SoundEmitter.MakeSound(...) 

        yield return new WaitForSeconds(0.2f); // 잠깐 대기 (로딩 느낌)

        if (cc != null) cc.enabled = true; // 다시 켜기

        ResetZoneState();

        // 4. 화면 밝게 (Fade In)
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadePanel.alpha = 1f - (timer / fadeDuration);
            yield return null;
        }
        fadePanel.alpha = 0f;

        // 5. 조작 풀기
        if (pc != null) pc.enabled = true;
        isTeleporting = false;
    }
}
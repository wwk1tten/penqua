using UnityEngine;
using StarterAssets; // 플레이어 스크립트 네임스페이스

public class SimpleLadder : MonoBehaviour
{
    [Header("사다리 속도")]
    public float climbSpeed = 3.0f;

    private ThirdPersonController playerScript;
    private CharacterController charController;
    private bool isClimbing = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 플레이어 컴포넌트 찾아오기
            playerScript = other.GetComponent<ThirdPersonController>();
            charController = other.GetComponent<CharacterController>();

            if (playerScript != null)
            {
                isClimbing = true;
                playerScript.enabled = false; // 1. 기존 이동/중력 끄기 (핵심)
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StopClimbing();
        }
    }

    void Update()
    {
        if (!isClimbing || charController == null) return;

        // W, S 키 입력 받기 (위아래)
        float verticalInput = Input.GetAxis("Vertical"); // W(+1), S(-1)

        // 2. 직접 위아래로 이동시키기
        Vector3 moveDirection = Vector3.up * verticalInput * climbSpeed * Time.deltaTime;
        charController.Move(moveDirection);

        // 스페이스바 누르면 사다리에서 뛰어내리기
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StopClimbing();
        }
    }

    void StopClimbing()
    {
        isClimbing = false;
        if (playerScript != null) playerScript.enabled = true; // 이동/중력 다시 켜기
    }
}
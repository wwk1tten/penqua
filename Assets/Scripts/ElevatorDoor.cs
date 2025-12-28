using UnityEngine;
using System.Collections;

public class VerticalElevator : MonoBehaviour
{
    [Header("문 오브젝트 연결")]
    public Transform door1; // 첫 번째 문짝
    public Transform door2; // 두 번째 문짝

    [Header("설정")]
    public float moveDistance = 2.0f; // 얼마나 이동할지 (높이)
    public float speed = 2.0f;        // 열리는 속도
    public float startDelay = 2.0f;   // 시작 후 대기 시간
    public bool isOpened = false; 

    [Header("사운드")]
    public AudioSource audioSource;
    public AudioClip openSound;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player")&& isOpened == false){
            StartCoroutine(OpenVertical());
        }
    }

    IEnumerator OpenVertical()
    {
        isOpened = true;
        yield return new WaitForSeconds(startDelay);

        if (audioSource != null && openSound != null)
            audioSource.PlayOneShot(openSound);

        float t = 0f;
        
        // 시작 위치 저장
        Vector3 start1 = door1.localPosition;
        Vector3 start2 = door2.localPosition;

        // 목표 위치 계산 (Local Y축 기준)
        Vector3 end1 = start1 + Vector3.up * moveDistance;
        Vector3 end2 = Vector3.zero;

        end2 = start2 + Vector3.up * moveDistance;


        while (t < 1.0f)
        {
            t += Time.deltaTime * speed;
            if(door1 != null) door1.localPosition = Vector3.Lerp(start1, end1, t);
            if(door2 != null) door2.localPosition = Vector3.Lerp(start2, end2, t);
            yield return null;
        }
    }
}
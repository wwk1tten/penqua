using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndingTrigger : MonoBehaviour
{
    public static EndingTrigger Instance;
    [Header("포탈 연결")]
    public GameObject portalObject; // 씬에 미리 배치해둔 포탈 오브젝트
    public Transform escapePoint;   // 동물들이 모일 지점 (포탈 정중앙)

    [Header("연출 설정")]
    public float portalGrowSpeed = 2.0f; // 포탈이 커지는 속도
    public Vector3 finalScale = Vector3.one; // 포탈의 최종 크기 (보통 1,1,1)

    private bool isEnding = false;
    void Awake(){
        if (Instance == null) Instance = this;
    }
    void Start()
    {
        // 게임 시작할 때 포탈은 숨겨두기 (안 보이게)
        if (portalObject != null)
        {
            portalObject.transform.localScale = Vector3.zero; // 크기를 0으로 해서 숨김
            portalObject.SetActive(false); // 아예 꺼둠
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isEnding)
        {
            CallAnimalsToEscape();
        }
    }
    public void OpenPortalFromConsole()
    {
        if (!isEnding)
        {
            StartCoroutine(StartEscapeSequence());
        }
    }

    IEnumerator StartEscapeSequence()
    {
        isEnding = true;
        Debug.Log("🌊 엔딩 시퀀스 시작! 포탈 개방!");

        if (portalObject != null)
        {
            portalObject.SetActive(true);
            float t = 0;
            while (t < 1.0f)
            {
                t += Time.deltaTime * portalGrowSpeed;
                portalObject.transform.localScale = Vector3.Lerp(Vector3.zero, finalScale, t);
                yield return null;
            }
        }
        
        // 동물 호출은 여기서 빼거나, 상황에 따라 유지 (선택)
        CallAnimalsToEscape(); 
    }

    void CallAnimalsToEscape()
    {
        // 아까 만든 정적 리스트 사용
        List<CapsuleFriendController> currentSwimmers = new List<CapsuleFriendController>(CapsuleFriendController.ActiveSwimmers);

        foreach (var animal in currentSwimmers)
        {
            if (animal == null) continue;

            animal.enabled = false; 
            StartCoroutine(MoveAnimalToExit(animal.transform));
        }
    }

    IEnumerator MoveAnimalToExit(Transform animalInfo)
    {
        float speed = 3.0f;
        // 탈출구(포탈 중앙)로 빨려 들어감
        while(animalInfo != null && Vector3.Distance(animalInfo.position, escapePoint.position) > 0.5f)
        {
            animalInfo.position = Vector3.MoveTowards(animalInfo.position, escapePoint.position, speed * Time.deltaTime);
            animalInfo.LookAt(escapePoint); 
            yield return null;
        }
        
        // 포탈에 닿으면 사라짐
        if(animalInfo != null) Destroy(animalInfo.gameObject);
    }
}
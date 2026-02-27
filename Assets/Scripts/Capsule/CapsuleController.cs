// Capsule.cs
using StarterAssets;
using Unity.VisualScripting;
using UnityEngine;

public class CapsuleController : MonoBehaviour
{
    [Tooltip("얼마나 높이 떠오를지")]
    public float bobHeight = 0.2f;

    [Tooltip("떠오르는 속도")]
    public float bobSpeed = 2f;

    public string capsuleID = "A";
    public GameObject animalPrefab;
    public Sprite capsuleIcon; 
    
    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // Mathf.Sin 함수는 -1과 1 사이를 부드럽게 반복하는 값을 만듭니다.
        // Time.time을 곱해 시간에 따라 계속 움직이게 합니다.
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        
        // 계산된 Y값으로 위치를 업데이트합니다. X와 Z는 고정됩니다.
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }
    
    public void Interact(GameObject player)
    {
        // 1. 데이터 생성 (컨트롤러가 하던 일을 캡슐이 직접 함)
        CapsuleData newData = new CapsuleData
        {
            //capsuleID = this.capsuleID,
            animalPrefab = this.animalPrefab,
            capsuleIcon = this.capsuleIcon
        };

        // 2. 플레이어의 인벤토리에 데이터 전달
        if (player.TryGetComponent(out PlayerInventory inventory))
        {
            //inventory.AddCapsule(newData);
        }

        // 3. 게임 매니저 알림 및 파괴
        GameManager.Instance.OnCapsuleCollected(capsuleID);
        Destroy(gameObject);
    }
    
}

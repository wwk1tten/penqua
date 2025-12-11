using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public bool requireInteraction = true;
    private bool isPlayerInZone = false;
    private WaterGunController waterGun; // 타입 변경

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            waterGun = other.GetComponent<WaterGunController>(); // 컴포넌트 변경
            
            if (!requireInteraction) CollectItem();
            else Debug.Log("물총을 주우려면 'E' 키를 누르세요.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            waterGun = null;
        }
    }

    void Update()
    {
        if (isPlayerInZone && requireInteraction && Input.GetKeyDown(KeyCode.E))
        {
            CollectItem();
        }
    }

    void CollectItem()
    {
        if (waterGun != null)
        {
            waterGun.PickupWaterGun(); // 함수 호출
            Destroy(gameObject); 
        }
    }
}

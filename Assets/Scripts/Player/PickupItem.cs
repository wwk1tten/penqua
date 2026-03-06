using UnityEngine;

// 상호작용을 위해 IInteractable 인터페이스를 상속받습니다.
public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("획득 설정")]
    [Tooltip("체크 해제 시 플레이어와 닿기만 해도 자동으로 주워집니다.")]
    public bool requireInteraction = true;

    [Header("UI 연결 (선택 사항)")]
    public GameObject bubblePressE; // "E키를 누르세요" 안내 버블

    private void Start()
    {
        if (bubblePressE != null) bubblePressE.SetActive(false);
    }

    // ==========================================
    // 1. 트리거 진입: 자동 획득 또는 UI 띄우기
    // ==========================================
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!requireInteraction)
            {
                // 상호작용이 필요 없다면 닿자마자 즉시 획득
                CollectItem(other.gameObject);
            }
            else
            {
                // 상호작용이 필요하다면 UI만 띄워줌 (입력 대기)
                if (bubblePressE != null) bubblePressE.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && bubblePressE != null)
        {
            bubblePressE.SetActive(false);
        }
    }

    // ==========================================
    // 2. 인터페이스 구현부: E키를 눌렀을 때 플레이어가 호출
    // ==========================================
    public void Interact(GameObject player)
    {
        // 자동 획득 모드일 때 실수로 E키를 눌러도 중복 실행되지 않도록 방어
        if (requireInteraction)
        {
            CollectItem(player);
        }
    }

    // ==========================================
    // 3. 실제 획득 로직 통합
    // ==========================================
    private void CollectItem(GameObject player)
    {
        // TryGetComponent를 사용하여 안전하고 빠르게 컴포넌트 접근
        if (player.TryGetComponent(out WaterGunController waterGun))
        {
            waterGun.PickupWaterGun();
            
            // TODO: 무기 획득 효과음 (AudioSource.PlayClipAtPoint 등) 추가 가능
            
            Destroy(gameObject);
        }
    }
}
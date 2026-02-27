using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum CapsuleType
{
    None = 0,
    Gecko = 1,
    Herring = 2,
    Muskrat = 3
}
public enum ItemType
{
    None = 0,
    Keycard = 1,
    WarehouseKey = 2,
    Hammer = 3,
}

[System.Serializable]
public struct CapsuleData
{
    public CapsuleType capsuleID;
    public GameObject animalPrefab;
    public Sprite capsuleIcon;
}
public class PlayerInventory : MonoBehaviour{
    [Header("보유 중인 열쇠 목록")]
    //public List<KeyType> possessedKeys = new List<KeyType>();
    [Header("획득한 아이템 현황")]
    public bool hasKeycard = false; 
    public bool hasHammer = false;
    public bool hasWarehouseKey = false; // 아까 언급한 창고 열쇠

    [Header("수집된 펭귄 캡슐 리스트")]
    public List<CapsuleData> collectedCapsules = new List<CapsuleData>();

    // 일반 아이템 획득 (확장성 있게 이름으로 체크 가능)
    public void GetItem(string itemName)
    {
        switch (itemName)
        {
            case "Keycard": hasKeycard = true; break;
            case "Hammer": hasHammer = true; break;
            case "WarehouseKey": hasWarehouseKey = true; break;
            default: Debug.LogWarning($"{itemName}은(는) 정의되지 않은 아이템 이름입니다."); break;
        }
        Debug.Log($"{itemName} 획득 완료!");
    }

    // 캡슐 데이터
    public void AddCapsule(CapsuleData data)
    {
        collectedCapsules.Add(data);
        Debug.Log($"캡슐 {data.capsuleID}번이 인벤토리에 저장되었습니다. 현재 총 {collectedCapsules.Count}개.");
    }

    // 특정 아이템을 가지고 있는지 확인하는 함수
    public bool HasItem(string itemName)
    {
        return itemName switch
        {
            "Keycard" => hasKeycard,
            "Hammer" => hasHammer,
            "WarehouseKey" => hasWarehouseKey,
            _ => false
        };
    }

    // 1. 특정 캡슐 사용
    public void UseCapsule(CapsuleType targetID)
    {
        // List.FindIndex를 사용해 조건에 맞는 첫 번째 요소의 인덱스를 찾습니다.
        int index = collectedCapsules.FindIndex(c => c.capsuleID == targetID);
        
        if (index != -1)
        {
            collectedCapsules.RemoveAt(index);
            Debug.Log($"캡슐 '{targetID}'을(를) 사용했습니다.");
        }
    }

    // 2. 특정 캡슐 보유 여부 확인
    public bool HasCapsule(CapsuleType targetID)
    {
        // List.Exists를 사용하면 foreach 루프와 Trim() 연산을 한 줄로 대체할 수 있습니다.
        return collectedCapsules.Exists(c => c.capsuleID == targetID);
    }

    // 3. 캡슐 방생 (선입선출)
    public void ActivateCapsule(Vector3 activationPoint)
    {
        if (collectedCapsules.Count == 0)
        {
            Debug.Log("해방할 캡슐이 없습니다!");
            return;
        }

        CapsuleData capsuleToRelease = collectedCapsules[0];

        if (capsuleToRelease.animalPrefab != null)
        {
            Instantiate(capsuleToRelease.animalPrefab, activationPoint, Quaternion.identity);
        }

        GameManager.Instance.OnAnimalReleased();
        collectedCapsules.RemoveAt(0);
    }

}
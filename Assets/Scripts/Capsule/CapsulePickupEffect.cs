using UnityEngine;

public class CapsulePickupEffect : MonoBehaviour, IPickupEffect
{
    public void OnPickup(GameObject player)
    {
        if (!TryGetComponent(out CapsuleController capsule)) return;

        CapsuleData newData = new CapsuleData
        {
            capsuleID = capsule.capsuleID,
            animalPrefab = capsule.animalPrefab,
            capsuleIcon = capsule.capsuleIcon
        };

        if (player.TryGetComponent(out PlayerInventory inventory))
        {
            inventory.AddCapsule(newData);
        }

        GameManager.Instance.OnCapsuleCollected(capsule.capsuleID);
    }
}

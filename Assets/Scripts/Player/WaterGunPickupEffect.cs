using UnityEngine;

public class WaterGunPickupEffect : MonoBehaviour, IPickupEffect
{
    public void OnPickup(GameObject player)
    {
        if (player.TryGetComponent(out WaterGunController waterGun))
        {
            waterGun.PickupWaterGun();
        }
    }
}

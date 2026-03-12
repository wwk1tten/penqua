using UnityEngine;
using UnityEngine.UI; 

public class WaterUIController : MonoBehaviour
{
    [Header("연결할 대상")]
    public WaterGunController waterGun; // 물의 양 정보를 가져올 물총 컨트롤러
    public Image waterFillImage;      // 채워질 UI 이미지

    void Update()
    {
        // waterGun이나 waterFillImage가 할당되지 않았으면 아무것도 하지 않음
        if (waterGun == null || waterFillImage == null)
        {
            return;
        }

        // waterGun 스크립트의 GetWaterRatio() 함수를 호출하여 0~1 사이의 비율을 받아옴
        float waterRatio = waterGun.GetWaterRatio();

        // 받아온 비율을 UI 이미지의 Fill Amount 값에 그대로 적용
        waterFillImage.fillAmount = waterRatio;
    }
}

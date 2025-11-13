#region Using statements

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Bitgem.VFX.StylisedWater
{
    public class WateverVolumeFloater : MonoBehaviour
    {
        #region Public fields

        public WaterVolumeHelper WaterVolumeHelper = null;

        #endregion

        #region MonoBehaviour events

        void Update()
        {
            // WaterVolumeHelper의 인스턴스를 가져옴
            var instance = WaterVolumeHelper.Instance;
            
            // 인스턴스가 null이면 아무것도 하지 않음
            if (instance == null)
            {
                Debug.LogWarning("WaterVolumeHelper.Instance가 null입니다. 씬에 WaterVolumeHelper가 있는지 확인하세요.");
                return;
            }

            // GetHeight가 null을 반환할 수 있으므로 null 체크
            float? waterHeight = instance.GetHeight(transform.position);
            
            if (waterHeight.HasValue)
            {
                transform.position = new Vector3(transform.position.x, waterHeight.Value, transform.position.z);
            }
            else
            {
                // 물 높이를 찾을 수 없으면 현재 위치 유지
                // Debug.Log("해당 위치에서 물 높이를 찾을 수 없습니다.");
            }
        }


        #endregion
    }
}
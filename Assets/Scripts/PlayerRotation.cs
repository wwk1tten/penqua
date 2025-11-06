using UnityEngine;

public class PlayerRotation : MonoBehaviour {
    public Transform cameraTransform; // MainCamera 또는 카메라Pivot

    void LateUpdate() {
        // 카메라 Forward의 Y축만 반영
        Vector3 lookDir = cameraTransform.forward;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.01f) {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = targetRot;
        }
    }
}

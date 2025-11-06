using UnityEngine;

public class WaterPuddle : MonoBehaviour
{
    public PhysicsMaterial slipperyMat;
    public PhysicsMaterial originalMat; // 기존 바닥용

    void OnTriggerEnter(Collider other) {
        var col = other.GetComponent<Collider>();
        if (col != null && slipperyMat != null) {
            col.material = slipperyMat; // 마찰 낮은 Physics Material
        }
    }
    void OnTriggerExit(Collider other) {
        var col = other.GetComponent<Collider>();
        if (col != null && originalMat != null) {
            col.material = originalMat;
        }
    }
}

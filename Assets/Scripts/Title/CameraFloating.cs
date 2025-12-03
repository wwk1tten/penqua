using UnityEngine;

public class CameraFloating : MonoBehaviour
{
    public float speed = 0.5f;
    public float height = 0.2f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // 사인(Sin) 그래프를 이용해 위아래로 둥둥 떠다니게 함
        float newY = startPos.y + Mathf.Sin(Time.time * speed) * height;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class AISensor : MonoBehaviour
{
    public float distance = 1.5f;
    public float angle = 70f;
    public float height = 1f;
    public Color meshColor = Color.red;
    public int scanFrequency = 30;
    public LayerMask layers;
    public LayerMask occlusionLayers;
    
    public List<GameObject> Objects 
    {
        get 
        {
            objects.RemoveAll(obj => !obj);
            return objects;
        }
    }
    
    public List<GameObject> objects = new List<GameObject>();

    Collider[] colliders = new Collider[50];
    Mesh mesh;
    int count;
    float scanInterval;
    float scanTimer;

    void Start()
    {
        scanInterval = 1.0f / scanFrequency;
    }

    void Update()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer < 0) {
            scanTimer += scanInterval;
            Scan();
        }

        if (Application.isEditor && !Application.isPlaying) {
             // 에디터 모드일 때만 실시간 갱신
        }
    }

    private void Scan()
    {
        count = Physics.OverlapSphereNonAlloc(transform.position, distance, colliders, layers, QueryTriggerInteraction.Collide);

        objects.Clear();
        for(int i = 0; i < count; ++i)
        {
            GameObject obj = colliders[i].gameObject;
            if(IsInSight(obj)) 
            {
                objects.Add(obj);
            }
        }
    }

    public bool IsInSight(GameObject obj)
    {
        Vector3 origin = transform.position + Vector3.up * (height / 2f);
        Vector3 dest = obj.transform.position + Vector3.up * (height / 2f);
        Vector3 direction = dest - origin;
        
        // 1. 높이 체크
        float heightDiff = Mathf.Abs(direction.y);
        if (heightDiff > height / 2f) 
        {
            return false;
        }

        // 2. 각도 체크
        Vector3 flatDirection = new Vector3(direction.x, 0, direction.z).normalized;
        float deltaAngle = Vector3.Angle(flatDirection, transform.forward);
        
        if (deltaAngle > angle / 2f) return false;

        // 3. 장애물 체크
        if(Physics.Linecast(origin, dest, occlusionLayers)) return false;
        
        return true;
    }

    Mesh CreateWedgeMesh(){
        Mesh newMesh = new Mesh();

        int segments = 10;
        int numTriangles = (segments * 4) + (2 * segments); // 4각 면 + top/bottom
        int numVertices = numTriangles * 3;

        Vector3[] vertices = new Vector3[numVertices];
        int[] triangles = new int[numVertices];
        
        Vector3 bottomCenter = Vector3.zero;
        Vector3 topCenter = Vector3.up * height;

        int vert = 0;
        
        float currentAngle = -angle / 2f; 
        float deltaAngle = angle / segments;
        
        // 모든 segment의 포인트 저장
        Vector3[] bottomPoints = new Vector3[segments + 1];
        Vector3[] topPoints = new Vector3[segments + 1];
        
        for(int i = 0; i <= segments; ++i)
        {
            Vector3 direction = Quaternion.Euler(0, currentAngle + (deltaAngle * i), 0) * Vector3.forward;            
            
            bottomPoints[i] = direction * distance;
            topPoints[i] = bottomPoints[i] + Vector3.up * height;
        }

        // ===== LEFT SIDE =====
        vertices[vert++] = bottomCenter;
        vertices[vert++] = bottomPoints[0];
        vertices[vert++] = topPoints[0];

        vertices[vert++] = topPoints[0];
        vertices[vert++] = topCenter;
        vertices[vert++] = bottomCenter;

        // ===== RIGHT SIDE =====
        vertices[vert++] = bottomCenter;
        vertices[vert++] = topCenter;
        vertices[vert++] = topPoints[segments];

        vertices[vert++] = topPoints[segments];
        vertices[vert++] = bottomPoints[segments];
        vertices[vert++] = bottomCenter;

        // ===== FAR SIDE (Subdivisions) =====
        for(int i = 0; i < segments; ++i)
        {
            // 아래 삼각형
            vertices[vert++] = bottomPoints[i];
            vertices[vert++] = bottomPoints[i + 1];
            vertices[vert++] = topPoints[i + 1];

            // 위 삼각형
            vertices[vert++] = topPoints[i + 1];
            vertices[vert++] = topPoints[i];
            vertices[vert++] = bottomPoints[i];
        }

        // ===== TOP SIDE (Fan triangulation) =====
        for(int i = 0; i < segments; ++i)
        {
            vertices[vert++] = topCenter;
            vertices[vert++] = topPoints[i];
            vertices[vert++] = topPoints[i + 1];
        }

        // ===== BOTTOM SIDE (Fan triangulation) =====
        for(int i = 0; i < segments; ++i)
        {
            vertices[vert++] = bottomCenter;
            vertices[vert++] = bottomPoints[i + 1];
            vertices[vert++] = bottomPoints[i];
        }

        // Triangles 배열
        for(int i = 0; i < vert; ++i) 
        {
            triangles[i] = i;
        }

        newMesh.vertices = vertices;
        newMesh.triangles = triangles;
        newMesh.RecalculateNormals();

        return newMesh;
    }


    private void OnValidate() 
    {
        mesh = CreateWedgeMesh();
        scanInterval = 1.0f / scanFrequency;
        
        // 에디터에서 값 바꿀 때 바로 반영
        if (GetComponent<MeshFilter>() != null)
            GetComponent<MeshFilter>().sharedMesh = mesh;
            
        //UpdateMeshMaterial();
    }

    private void OnDrawGizmos() 
    {
        Gizmos.color = Color.green;
        foreach (var obj in Objects) 
        {
            if (obj != null) Gizmos.DrawSphere(obj.transform.position, 0.2f);
        } 
    }
    
    public int Filter(GameObject[] buffer, string layerName) 
    {
        int layer = LayerMask.NameToLayer(layerName);
        int count = 0;
        
        foreach (var obj in Objects) 
        {
            if (obj.layer == layer) 
            {
                buffer[count++] = obj;
            }
            
            if (buffer.Length == count) 
            {
                break;
            }
        }
        return count;
    }
}

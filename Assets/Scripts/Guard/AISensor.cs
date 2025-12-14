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
        // 경비원 눈 위치
        Vector3 origin = transform.position + Vector3.up * (height / 2f);
        
        // 체크할 타겟의 포인트들 (중심, 머리, 좌, 우)
        List<Vector3> targetPoints = new List<Vector3>();
        
        // 1. 중심 (기존)
        targetPoints.Add(obj.transform.position + Vector3.up * (height / 2f)); 
        
        // 2. 머리 위 (높이 판정 보완)
        // (CharacterController가 있다면 height나 radius를 가져오면 더 정확함)
        targetPoints.Add(obj.transform.position + Vector3.up * (height * 0.9f));

        // 3. 좌우 어깨 (폭 판정 보완 - 엉덩이 튀어나옴 감지용)
        Vector3 right = obj.transform.right * 0.3f; // 펭귄 너비의 절반 정도
        targetPoints.Add(obj.transform.position + Vector3.up * (height / 2f) + right);
        targetPoints.Add(obj.transform.position + Vector3.up * (height / 2f) - right);

        // 포인트 중 하나라도 보이면 "본 것"으로 처리
        foreach (var dest in targetPoints)
        {
            Vector3 direction = dest - origin;
            
            // 거리 체크 (OverlapSphere에서 했지만 확실히)
            if (direction.magnitude > distance) continue;

            // 각도 체크
            Vector3 flatDirection = new Vector3(direction.x, 0, direction.z).normalized;
            float deltaAngle = Vector3.Angle(flatDirection, transform.forward);
            if (deltaAngle > angle / 2f) continue;

            // 장애물(벽) 체크
            // Linecast가 true면 벽에 막힌 것 -> false면 뚫린 것(보임)
            if (!Physics.Linecast(origin, dest, occlusionLayers)) 
            {
                return true; // 하나라도 통과하면 발견!
            }
        }
        
        return false; // 모든 포인트가 안 보임
    }

    Mesh CreateWedgeMesh()
    {
        Mesh newMesh = new Mesh();

        int segments = 20; // ★ 벽에 닿는 모양을 부드럽게 하려면 숫자를 늘리세요 (10 -> 20 추천)
        int numTriangles = (segments * 4) + (2 * segments);
        int numVertices = numTriangles * 3;

        Vector3[] vertices = new Vector3[numVertices];
        int[] triangles = new int[numVertices];
        Vector2[] uvs = new Vector2[numVertices];

        Vector3 bottomCenter = Vector3.zero;
        Vector3 topCenter = Vector3.up * height;

        int vert = 0;

        float currentAngle = -angle / 2f;
        float deltaAngle = angle / segments;

        Vector3[] bottomPoints = new Vector3[segments + 1];
        Vector3[] topPoints = new Vector3[segments + 1];

        // ★ [핵심 변경] 각 세그먼트마다 레이캐스트를 쏴서 거리 계산
        for (int i = 0; i <= segments; ++i)
        {
            // 1. 로컬 방향 계산
            Vector3 localDir = Quaternion.Euler(0, currentAngle + (deltaAngle * i), 0) * Vector3.forward;
            
            // 2. 월드 방향으로 변환 (레이캐스트용)
            Vector3 worldDir = transform.TransformDirection(localDir);

            // 3. 레이캐스트 발사 (중심 높이에서 발사)
            float actualDistance = distance;
            
            // 높이의 중간 지점에서 레이를 쏩니다.
            Vector3 rayOrigin = transform.position + (Vector3.up * height * 0.5f);

            RaycastHit hit;
            // occlusionLayers에 포함된 물체(벽)에만 반응
            if (Physics.Raycast(rayOrigin, worldDir, out hit, distance, occlusionLayers))
            {
                // 벽에 맞았다면 거리를 충돌 지점까지로 단축 (약간의 오차 방지를 위해 살짝 줄임)
                actualDistance = hit.distance - 0.05f; 
            }

            // 4. 계산된 거리로 정점 위치 설정
            bottomPoints[i] = localDir * actualDistance;
            topPoints[i] = bottomPoints[i] + Vector3.up * height;
        }

        // --- 이하 정점/UV 생성 로직은 동일합니다 ---
        
        void AddVertex(Vector3 position, float u, float v)
        {
            vertices[vert] = position;
            uvs[vert] = new Vector2(u, v);
            vert++;
        }

        // LEFT SIDE
        AddVertex(bottomCenter, 0, 0);
        AddVertex(bottomPoints[0], 1, 0);
        AddVertex(topPoints[0], 1, 1);
        AddVertex(topPoints[0], 1, 1);
        AddVertex(topCenter, 0, 1);
        AddVertex(bottomCenter, 0, 0);

        // RIGHT SIDE
        AddVertex(bottomCenter, 0, 0);
        AddVertex(topCenter, 0, 1);
        AddVertex(topPoints[segments], 1, 1);
        AddVertex(topPoints[segments], 1, 1);
        AddVertex(bottomPoints[segments], 1, 0);
        AddVertex(bottomCenter, 0, 0);

        // FAR SIDE
        for (int i = 0; i < segments; ++i)
        {
            AddVertex(bottomPoints[i], 1, 0);
            AddVertex(bottomPoints[i + 1], 1, 0);
            AddVertex(topPoints[i + 1], 1, 1);
            AddVertex(topPoints[i + 1], 1, 1);
            AddVertex(topPoints[i], 1, 1);
            AddVertex(bottomPoints[i], 1, 0);
        }

        // TOP SIDE
        for (int i = 0; i < segments; ++i)
        {
            AddVertex(topCenter, 0, 1);
            AddVertex(topPoints[i], 1, 1);
            AddVertex(topPoints[i + 1], 1, 1);
        }

        // BOTTOM SIDE
        for (int i = 0; i < segments; ++i)
        {
            AddVertex(bottomCenter, 0, 0);
            AddVertex(bottomPoints[i + 1], 1, 0);
            AddVertex(bottomPoints[i], 1, 0);
        }

        for (int i = 0; i < vert; ++i)
        {
            triangles[i] = i;
        }

        newMesh.vertices = vertices;
        newMesh.triangles = triangles;
        newMesh.uv = uvs;
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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; // 네비게이션(적 이동) 제어용

public class SecurityAlertSystem : MonoBehaviour
{
    [Header("경보 설정")]
    public float speedMultiplier = 1.5f; // 속도 1.5배 증가
    public List<GameObject> enemies;
    [Header("연출")]
    public AudioSource alarmSound;       // 앵~ 앵~ 사이렌 소리
    public Light[] roomLights;           // 빨갛게 바꿀 조명들 (선택)
    public Color alertColor = Color.red; // 경보 색상

    public void TriggerEmergency()
    {
        Debug.Log("🚨 비상 경보 발령! 적들이 강화됩니다!");

        // 1. 소리 재생
        if (alarmSound != null) 
        {
            alarmSound.loop = true; // 계속 울리게
            alarmSound.Play();
        }

        // 2. 조명 색 바꾸기 (분위기 전환)
        foreach (Light light in roomLights)
        {
            if (light != null) light.color = alertColor;
        }

        // 3. ★ 핵심: 맵 전체의 적을 찾아서 강화시킴
        BuffAllEnemies();
    }

    void BuffAllEnemies()
    {
        foreach (GameObject enemyObj in enemies)
        {
            if (enemyObj == null) continue;

            // 1. NavMeshAgent 속도 증가
            NavMeshAgent agent = enemyObj.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed *= speedMultiplier;
                agent.acceleration *= speedMultiplier;
                agent.angularSpeed *= speedMultiplier;
            }

            // 2. 감지 범위도 늘리기
            
            AISensor ai = enemyObj.GetComponent<AISensor>();
            if (ai != null)
            {
                ai.distance *= speedMultiplier; 
                ai.angle *= speedMultiplier;
            }

            GuardPatrol gp = enemyObj.GetComponent<GuardPatrol>();
            if (gp != null)
            {
                gp.hearingRange *= speedMultiplier; 
                gp.hearingSensitivity *= speedMultiplier;
            }
            
        }
    }
}
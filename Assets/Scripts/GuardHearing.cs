using UnityEngine;

public class GuardHearing : MonoBehaviour
{
    private GuardPatrol guardPatrol;
    
    void Start()
    {
        guardPatrol = GetComponent<GuardPatrol>();
        
        if (guardPatrol == null)
        {
            Debug.LogError($"{gameObject.name}에 GuardPatrol 컴포넌트가 없습니다!");
        }
    }
    
    public void OnSoundHeard(Vector3 soundPosition)
    {
        if (guardPatrol != null)
        {
            guardPatrol.OnSoundHeard(soundPosition);
        }
    }
}

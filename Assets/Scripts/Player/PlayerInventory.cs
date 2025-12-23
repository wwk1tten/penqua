using UnityEngine;

public class PlayerInventory : MonoBehaviour{
    public bool hasKeycard = false; 
    public bool hasHammer = false;


    public void GetKey()
    {
        hasKeycard = true;
        Debug.Log("키카드 획득!");
    }

    public void GetHammer()
    {
        hasHammer = true;
        Debug.Log("망치 획득!");
    }

}
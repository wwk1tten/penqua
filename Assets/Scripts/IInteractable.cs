using UnityEngine;

/// <summary>
/// E키 등으로 상호작용할 수 있는 모든 오브젝트가 구현해야 하는 인터페이스
/// </summary>
public interface IInteractable
{
    void Interact(GameObject player);
}
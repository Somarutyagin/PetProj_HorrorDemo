using UnityEngine;
public abstract class Item : MonoBehaviour, IInteractable
{
    public abstract void Interact();
}
using UnityEngine;

public class Cup : Item
{
    [SerializeField] private GameObject lid; // The lid GameObject that can be removed
    private bool hasBeenFilled = false; // Track if cup has been filled with coffee
    
    public override void Interact()
    {
        // Logic for picking up cup
        Player.Instance.PickUpItem(this);
    }
    
    public GameObject GetLid()
    {
        return lid;
    }
    
    public bool HasBeenFilled => hasBeenFilled;
    
    public void MarkAsFilled()
    {
        hasBeenFilled = true;
    }
}
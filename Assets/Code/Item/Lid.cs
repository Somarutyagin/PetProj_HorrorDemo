using UnityEngine;

public class Lid : Item
{
    public override void Interact()
    {
        // Logic for picking up lid
        Player.Instance.PickUpItem(this);
    }
}

using UnityEngine;

public class InputHandler : MonoBehaviour, IInputProvider
{
    public Vector2 GetMovementInput()
    {
        return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
    }

    public Vector2 GetLookInput()
    {
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    }

    public bool IsRunPressed() => Input.GetKey(KeyCode.LeftShift);

    public bool IsJumpPressed() => Input.GetKeyDown(KeyCode.Space);

    public bool IsInteractionPressed() => Input.GetKeyDown(KeyCode.E);
}

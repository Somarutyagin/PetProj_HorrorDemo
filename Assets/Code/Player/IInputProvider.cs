using UnityEngine;

public interface IInputProvider
{
    Vector2 GetMovementInput();
    Vector2 GetLookInput();
    bool IsRunPressed();
    bool IsJumpPressed();
    bool IsInteractionPressed();
}

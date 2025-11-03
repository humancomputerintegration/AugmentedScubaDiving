using UnityEngine;

public enum HandSide { Left, Right }

public class StrokeZone : MonoBehaviour
{
    [Header("Setup")]
    public HandSide handSide;               // Set per zone (Left for left zone, Right for right zone)
    public Collider targetHandCollider;     // Drag the hand's collider here (e.g., a collider on the hand model)
    public SimpleMovement movement;         // Drag your SimpleMovement here

    private void OnTriggerEnter(Collider other)
    {
        if (!movement || !targetHandCollider) return;

        if (other == targetHandCollider)
        {
            movement.NotifyStroke(handSide);
        }
    }
}

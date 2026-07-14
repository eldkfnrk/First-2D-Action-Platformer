using UnityEngine;

public class PlayerRuntimeData : MonoBehaviour
{
    // 변하는 플레이어의 값들을 저장

    public float direction;

    public RaycastHit2D groundCheck;
    public float groundCheckDistance;
    public bool isJump;
}

using UnityEngine;

public class PlayerRuntimeData : MonoBehaviour
{
    // 변하는 플레이어의 값들을 저장

    public float sightDirection;  // 캐릭터가 바라보고 있는 방향 값(-1f는 왼쪽 1f는 오른쪽)

    public float moveDirection;
    public bool cantInput;
    
    public bool downKeyPressed;

    public RaycastHit2D groundCheck;
    public RaycastHit2D wallCheck;

    public Vector2 wallJumpVec;
    public bool isJump;
    public bool jumpPressed;  // 점프 키 입력 여부

    public bool isWall;
}

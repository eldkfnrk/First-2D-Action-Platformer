using UnityEngine;

public class PlayerRuntimeData : MonoBehaviour
{
    // 변하는 플레이어의 값들을 저장

    public float curHP;  

    public float sightDirection;  // 캐릭터가 바라보고 있는 방향 값(-1f는 왼쪽 1f는 오른쪽)

    public float moveDirection;
    public bool cantInput;
    
    public bool downKeyPressed;
    public bool jumpKeyDown;  // 점프 키 입력 여부
    public bool rollKeyDown;

    public RaycastHit2D groundCheck;
    public RaycastHit2D wallCheck;

    public Vector2 wallJumpVec;
    public bool isJump;

    public Vector2 attackBoxPos;
    public Vector2 attackDir;

    public bool isRoll;
    public bool isWall;
    public bool isAttack;
    public bool isHit;
    public bool isDead;

    public bool isRevival;  // 플레이어 사망 시 플레이 모드를 껐다가 다시 켜야지만 또 확인을 할 수 있는 번거로움을 해소하기 위해 부활시키는 임시 동작을 위한 변수

    public Vector2 knockbackDir;

    public bool atkRoutine;
    public int atkKeyDownCount;
    public int atkCount;
}

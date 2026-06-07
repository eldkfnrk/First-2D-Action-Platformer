using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAction : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Run,
        Jump,
        Fall,
        Attack,
        WallSlide,
        Roll,
        Block,
        Hurt,
        Death,
    }

    public PlayerState playerState;

    // 바닥, 벽 등의 충돌체 체크
    RaycastHit2D groundCheck;
    RaycastHit2D wallCheck;
    public LayerMask groundLayer;

    // 좌우 이동
    public float moveSpeed;
    public float moveDirection;

    // 점프
    public float jumpPower;
    public bool isJump;
    public bool isGround;
    int jumpCnt;

    // 공격
    public bool isAttack;
    int attackCnt;
    WaitForSeconds attackDelay;
    public float attackDelayData;

    // 구르기
    public bool isRoll;
    public bool canRoll;
    WaitForSeconds rollDuration;
    public float rollDurationData;
    WaitForSeconds rollCoolTime;
    public float rollCoolTimeData;

    // 방어
    public bool isBlock;

    // 컴포넌트
    Rigidbody2D rigid;
    SpriteRenderer spriteR;

    private void Awake()
    {
        playerState = PlayerState.Idle;
        attackDelay = new WaitForSeconds(attackDelayData);
        rollDuration = new WaitForSeconds(rollDurationData);
        rollCoolTime = new WaitForSeconds(rollCoolTimeData - rollDurationData);
        rigid = GetComponent<Rigidbody2D>();
        spriteR = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (moveDirection > 0f)
            spriteR.flipX = false;
        else if (moveDirection < 0f)
            spriteR.flipX = true;
    }

    private void FixedUpdate()
    {
        groundCheck = Physics2D.Raycast(transform.position, Vector2.down, 1f, groundLayer);

        rigid.linearVelocityX = moveDirection * moveSpeed;

        switch (playerState)
        {
            case PlayerState.Attack:
            case PlayerState.Roll:
            case PlayerState.Hurt:
            case PlayerState.Death:
            case PlayerState.Block:
                rigid.linearVelocityX = 0f;
                break;
            case PlayerState.Run:
                if (moveDirection == 0f)
                    playerState = PlayerState.Idle;
                break;
            case PlayerState.Jump:
                if (rigid.linearVelocityY < 0f)
                    playerState = PlayerState.Fall;
                break;
            case PlayerState.Fall:
                if (groundCheck.collider != null)
                {
                    isGround = true;
                    isJump = false;
                    jumpCnt = 0;

                    if (moveDirection != 0f)
                        playerState = PlayerState.Run;
                    else
                        playerState = PlayerState.Idle;
                }
                break;
        }

        // 6-7
        // 점프를 몇 번 하면 다시 점프가 안 되거나 특정 경우 애니메이션 전환이 원할히 되지 않는 문제가 발생
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        // 이동 방향 값 획득
        // 상태가 Idle일 때는 상태 전환
        // 상태가 Jump 혹은 Fall일 때는 전환x
        // 상태가 Attack, Roll, Hurt, Death, Block일 경우에는 이동 불가
        // 상태가 WallSlide일 때는 아래로 이동하는 값만 획득, 좌우 이동 불가
        if(context.started || playerState == PlayerState.Idle)
        {
            playerState = PlayerState.Run;
        }
        
        moveDirection = context.ReadValue<Vector2>().x;
        if (moveDirection > 0f)
            moveDirection = 1f;
        else if (moveDirection < 0f)
            moveDirection = -1f;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        // 입력과 동시에 점프
        // 어느 상태에서든 동작
        // 상태가 Attack, Roll, Hurt, Death일 경우에는 불가
        // 상태가 Block일 경우 Block을 하기 위해 변경되었던 값들을 원상태로 수정
        if (isJump || jumpCnt == 1)
            return;

        if (context.started)
        {
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            isGround = false;
            isJump = true;
            ++jumpCnt;
            playerState = PlayerState.Jump;
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        // 공격 - 최대 3회까지 연속 공격 가능
        // 상태가 Jump, Roll, WallSlide일 경우 불가
        // 상태가 Block일 경우 Block을 하기 위해 변경되었던 값들을 원상태로 수정
    }

    public void OnRoll(InputAction.CallbackContext context)
    {
        // 구르기
        // 상태가 Jump, WallSlide일 때 불가
        // 상태가 Block일 경우 Block을 하기 위해 변경되었던 값들을 원상태로 수정
        // 공격 중일 경우 해당 공격을 중지하고 변경되었던 값들을 원상태로 수정
    }

    public void OnBlock(InputAction.CallbackContext context)
    {
        // 방어
        // 상태가 Jump, WallSlide, Fall, Hurt, Death일 경우 불가
        // 움직이고 있었던 경우에는 즉시 그 자리에서 멈추고 방어 태세로 전환
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(transform.position, Vector2.down * 1f);
    }
}

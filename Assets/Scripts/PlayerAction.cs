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

    public enum AttackState
    {
        Attack1,
        Attack2,
        Attack3,
        None,
    }

    public AttackState attackState;

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
    public float jumpTimer;  // 점프 후 잠시 동안 바닥 검사를 하지 않는 시간
    public float fallSpeed;

    // 공격
    public bool isAttack;
    public int attackCnt;
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
        attackState = AttackState.None;
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
        groundCheck = Physics2D.Raycast(transform.position, Vector2.down, 0.9f, groundLayer);

        rigid.linearVelocityX = moveDirection * moveSpeed;

        switch (playerState)
        {
            case PlayerState.Idle:
                // 이동 키가 입력되어 있는데도 움직이는 상태로 변하지 않는 경우를 위한 조건문
                if (moveDirection != 0f)
                    playerState = PlayerState.Run;
                break;
            case PlayerState.Attack:
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
                // 0.25초 동안 바닥 체크 x -> 빠른 바닥 체크로 점프 하자마자 착지된 것으로 판정되는 문제를 해결
                jumpTimer += Time.fixedDeltaTime;
                if (jumpTimer < 0.25f)
                    break;
                
                if (rigid.linearVelocityY < 0f)
                {
                    jumpTimer = 0f;
                    playerState = PlayerState.Fall;
                }
                else if (groundCheck.collider != null)
                {
                    isGround = true;
                    isJump = false;
                    jumpTimer = 0f;
                    playerState = PlayerState.Idle;
                }
                break;
            case PlayerState.Fall:
                if (rigid.gravityScale == 1f)
                {
                    rigid.gravityScale = fallSpeed;
                }

                if (groundCheck.collider != null)
                {
                    isGround = true;
                    isJump = false;
                    rigid.gravityScale = 1f;
                    playerState = PlayerState.Idle;
                }
                break;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        // 이동 방향 값 획득
        // 상태가 Idle일 때는 상태 전환
        // 상태가 Jump 혹은 Fall일 때는 전환x
        // 상태가 Attack, Roll, Hurt, Death, Block일 경우에는 이동 불가
        // 상태가 WallSlide일 때는 아래로 이동하는 값만 획득, 좌우 이동 불가        
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
        if (isJump)
            return;

        if (context.started)
        {
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            isGround = false;
            isJump = true;
            playerState = PlayerState.Jump;
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        // 공격 - 최대 3회까지 연속 공격 가능
        // 상태가 Jump, Roll, WallSlide일 경우 불가
        // 상태가 Block일 경우 Block을 하기 위해 변경되었던 값들을 원상태로 수정
        if (context.started)
        {
            if (attackCnt == 0)
            {
                ++attackCnt;
                StartCoroutine(AttackRoutine());
            }
            else if (attackCnt <= 2)
            {
                ++attackCnt;
            }
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttack = true;
        playerState = PlayerState.Attack;
        attackState = AttackState.Attack1;
        // overlapbox 위치 지정
        // 적 공격(overlapbox 안에 적이 있었다면 그 적은 데미지를 입도록 함수 하나 생성 필요)

        yield return attackDelay;

        if(attackCnt >= 2)
        {
            attackState = AttackState.Attack2;
            // 적 공격(만든 함수 재활용)
            yield return attackDelay;
        }

        if (attackCnt == 3)
        {
            attackState = AttackState.Attack3;
            // 적 공격(만든 함수 재활용)
            yield return attackDelay;
        }

        isAttack = false;
        attackCnt = 0;
        playerState = PlayerState.Idle;
        attackState = AttackState.None;
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
        Gizmos.DrawRay(transform.position, Vector2.down * 0.9f);
    }
}

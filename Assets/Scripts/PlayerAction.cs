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

    // 구르기
    public bool isRoll;
    public bool canRoll;
    WaitForSeconds rollDuration;
    WaitForSeconds rollCoolTime;

    // 방어
    public bool isBlock;

    private void Awake()
    {
        playerState = PlayerState.Idle;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        // 이동 방향 값 획득
        // 상태가 Idle일 때는 상태 전환
        // 상태가 Jump 혹은 Fall일 때는 전환x
        // 상태가 Attack, Roll, Hurt, Death, Block일 경우에는 이동 불가
        // 상태가 WallSlide일 때는 아래로 이동하는 값만 획득, 좌우 이동 불가
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        // 입력과 동시에 점프
        // 어느 상태에서든 동작
        // 상태가 Attack, Roll, Hurt, Death일 경우에는 불가
        // 상태가 Block일 경우 Block을 하기 위해 변경되었던 값들을 원상태로 수정
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
}

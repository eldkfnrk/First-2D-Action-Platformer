using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Playables;

public class PlayerStateMachine : MonoBehaviour
{
    public enum State
    {
        Idle,
        Run,
        Jump,
        Fall,
        WallSlide,
        Roll,
    }
    public State playerState;  // 변하는 데이터 값이지만 예외적으로 상태 데이터이기 때문에 직접 관리

    Dictionary<State, BaseState> states;
    BaseState currentState;

    public PlayerData constantData;  // constant - 상수, 변하지 않는 데이터라는 의미로 붙인 변수명
    public PlayerRuntimeData variableData;  // variable - 변수, 변하는 데이터라는 의미로 붙인 변수명
    public PlayerAnimation PlayerAnimation;  // 애니메이션 관련 스크립트
    public Rigidbody2D rigid;
    public SpriteRenderer spriteR;
    public Collider2D coll;

    private void Awake()
    {
        variableData = GetComponent<PlayerRuntimeData>();
        PlayerAnimation = GetComponent<PlayerAnimation>();
        rigid = GetComponent<Rigidbody2D>();
        spriteR = GetComponent<SpriteRenderer>();
        coll = GetComponent<Collider2D>();

        states = new Dictionary<State, BaseState>();
        states.Add(State.Idle, new IdleState(this));
        states.Add(State.Run, new RunState(this));
        states.Add(State.Jump, new JumpState(this));
        states.Add(State.Fall, new FallState(this));
        states.Add(State.WallSlide, new WallSlideState(this));        
        states.Add(State.Roll, new RollState(this));        
    }

    private void Start()
    {
        playerState = State.Idle;
        currentState = states[playerState];
        currentState.Enter();
    }

    private void Update()
    {
        switch (playerState)
        {
            case State.Idle:
                if (variableData.rollKeyDown)
                {
                    ChangeState(State.Roll);
                    break;
                }

                if (CheckJumpOrFall())
                    break;

                if (variableData.moveDirection != 0f)
                    ChangeState(State.Run);
                break;
            case State.Run:
                if (variableData.rollKeyDown)
                {
                    ChangeState(State.Roll);
                    break;
                }

                if (CheckJumpOrFall())
                    break;

                if (variableData.moveDirection == 0f)
                    ChangeState(State.Idle);
                break;
            case State.Jump:
                if (WallCheck())
                    break;

                if (variableData.groundCheck.collider != null)
                {
                    if (rigid.linearVelocityY <= 0f)
                        ChangeState(State.Idle);
                    break;
                }

                if (rigid.linearVelocityY < 0f)
                    ChangeState(State.Fall);
                break;
            case State.Fall:
                if (WallCheck())
                    break;

                if (variableData.groundCheck.collider != null)
                    ChangeState(State.Idle);
                break;
            case State.WallSlide:
                if (variableData.jumpKeyDown)
                {
                    ChangeState(State.Jump);
                    break;
                }

                if (variableData.groundCheck.collider != null)
                    ChangeState(State.Idle);                
                break;
            case State.Roll:
                if (!variableData.isRoll)
                {
                    if (variableData.groundCheck.collider == null)
                        ChangeState(State.Fall);
                    else
                        ChangeState(State.Idle);
                }
                break;
        }

        currentState.Update();
        Debug.Log(playerState);
    }

    private void LateUpdate()
    {
        variableData.sightDirection = spriteR.flipX ? -1f : 1f;
    }

    private void FixedUpdate()
    {
        variableData.groundCheck = Physics2D.Raycast(transform.position, Vector2.down, constantData.groundCheckDistance, constantData.groundLayer);
        variableData.wallCheck = Physics2D.Raycast(transform.position, Vector2.right * variableData.sightDirection, constantData.wallCheckDistance, constantData.groundLayer);
    }

    void ChangeState(State state)
    {
        currentState.Exit();
        currentState = states[state];
        currentState.Enter();
    }

    bool CheckJumpOrFall()
    {
        // 점프 입력이 있었으면 점프 상태로 전환
        // 착지 상태가 아니면 떨어지는 상태로 전환
        if (variableData.jumpKeyDown)
        {
            if (variableData.wallCheck.collider != null)
            {
                variableData.jumpKeyDown = false;
                return false;
            }
            else
            {
                ChangeState(State.Jump);
                return true;
            }
        }
        else if (variableData.groundCheck.collider == null)
        {
            ChangeState(State.Fall);
            return true;
        }

        return false;
    }

    bool WallCheck()
    {
        if (variableData.cantInput)
            return false;

        // 앞에 벽이 있는지 확인(이때 착지 상태가 아니어야 벽에 있다고 판단) - 벽에 붙어서 미끄러지는 상태 전환을 위한 함수
        if (variableData.isJump && variableData.wallCheck.collider != null)
        {
            ChangeState(State.WallSlide);
            return true;
        }

        return false;
    }

    public void CantInputChange()
    {
        StartCoroutine(CantInputChangeRoutine());
    }

    IEnumerator CantInputChangeRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        variableData.cantInput = false;
    }

    public void ChangeCanRoll()
    {
        StartCoroutine(RollRoutine());
    }

    IEnumerator RollRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        variableData.isRoll = false;
        coll.enabled = true;
        rigid.gravityScale = 1f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector2.down * constantData.groundCheckDistance);
        Gizmos.color = Color.darkRed;
        Gizmos.DrawRay(transform.position, Vector2.right * constantData.wallCheckDistance * variableData.sightDirection);
    }
}

public abstract class BaseState
{
    // 아래 두 문장에서 protected를 사용한 이유는 이 변수와 함수는 이 클래스와 상속 받는 클래스에서만 사용되어야 하기 때문이다.
    protected PlayerStateMachine fsmController;
    protected BaseState(PlayerStateMachine controller)
    {
        fsmController = controller;
    }

    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();

    protected void Move()
    {
        if (fsmController.variableData.cantInput)
            return;

        fsmController.rigid.linearVelocityX = fsmController.variableData.moveDirection * fsmController.constantData.moveSpeed;
        if (fsmController.variableData.sightDirection == -fsmController.variableData.moveDirection)  // 바라보는 방향과 이동 방향이 반대인 경우
            fsmController.spriteR.flipX = !fsmController.spriteR.flipX;
    }
}

public class IdleState : BaseState
{
    // 여기서 base는 부모 클래스의 생성자를 의미한다.(C# 문법)
    // public으로 하지 않으면 이 생성자를 호출할 수 없기 때문에 특정 경우를 제외하고는 외부에서 초기화가 가능하도록 public 선언을 해야 한다.
    public IdleState(PlayerStateMachine controller) : base(controller) { }

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Idle;
        fsmController.variableData.isJump = false;
        fsmController.rigid.gravityScale = 1f;
        fsmController.PlayerAnimation.PlayIdle();
    }

    public override void Update()
    {

    }

    public override void Exit()
    {

    }
}

public class RunState : BaseState
{
    public RunState(PlayerStateMachine controller) : base(controller) { }

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Run;
        fsmController.PlayerAnimation.PlayRun();
    }

    public override void Update()
    {
        Move();
    }

    public override void Exit()
    {
        fsmController.rigid.linearVelocityX = 0f;
    }
}

public class JumpState : BaseState
{
    public JumpState(PlayerStateMachine controller) : base(controller) { }

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Jump;
        fsmController.variableData.isJump = true;
        if (fsmController.variableData.isWall)
        {
            fsmController.variableData.wallJumpVec.x = -fsmController.variableData.sightDirection * fsmController.constantData.hitWallPower;
            fsmController.variableData.wallJumpVec.y = fsmController.constantData.jumpPower;
            fsmController.variableData.cantInput = true;
            fsmController.variableData.isWall = false;
            fsmController.rigid.AddForce(fsmController.variableData.wallJumpVec, ForceMode2D.Impulse);
            fsmController.spriteR.flipX = !fsmController.spriteR.flipX;
            fsmController.CantInputChange();
        }
        else
        {
            fsmController.rigid.AddForce(Vector2.up * fsmController.constantData.jumpPower, ForceMode2D.Impulse);
        }

        fsmController.variableData.jumpKeyDown = false;
        fsmController.PlayerAnimation.PlayJump();
    }

    public override void Update()
    {
        Move();  // 점프 중에도 좌우 이동은 가능하도록 설정
    }

    public override void Exit()
    {
        if (fsmController.variableData.moveDirection == 0f)
            fsmController.rigid.linearVelocityX = 0f;
    }
}

public class FallState : BaseState
{
    public FallState(PlayerStateMachine controller) : base(controller) { }

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Fall;
        fsmController.rigid.gravityScale = fsmController.constantData.fallSpeed;
        fsmController.variableData.isJump = true;
        fsmController.PlayerAnimation.PlayFall();
    }

    public override void Update()
    {
        Move();  // 떨어지는 중에도 좌우 이동은 가능하도록 설정
    }

    public override void Exit()
    {
        if (fsmController.variableData.moveDirection == 0f)
            fsmController.rigid.linearVelocityX = 0f;
    }
}

public class WallSlideState : BaseState
{
    // 여기서 base는 부모 클래스의 생성자를 의미한다.(C# 문법)
    // public으로 하지 않으면 이 생성자를 호출할 수 없기 때문에 특정 경우를 제외하고는 외부에서 초기화가 가능하도록 public 선언을 해야 한다.
    public WallSlideState(PlayerStateMachine controller) : base(controller) { }

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.WallSlide;
        fsmController.variableData.isWall = true;
        fsmController.variableData.isJump = false;
        fsmController.PlayerAnimation.PlayWallSlide();
    }

    public override void Update()
    {
        if (fsmController.variableData.downKeyPressed)
            fsmController.rigid.linearVelocityY = -2f;
        else
            fsmController.rigid.linearVelocityY = 0f;
    }

    public override void Exit()
    {
        
    }
}

public class RollState : BaseState
{
    // 여기서 base는 부모 클래스의 생성자를 의미한다.(C# 문법)
    // public으로 하지 않으면 이 생성자를 호출할 수 없기 때문에 특정 경우를 제외하고는 외부에서 초기화가 가능하도록 public 선언을 해야 한다.
    public RollState(PlayerStateMachine controller) : base(controller) { }

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Roll;
        fsmController.variableData.rollKeyDown = false;
        fsmController.PlayerAnimation.PlayRoll();
        fsmController.variableData.isRoll = true;
        fsmController.coll.enabled = false;
        fsmController.rigid.gravityScale = 0f;
        fsmController.ChangeCanRoll();
    }

    public override void Update()
    {
        if (fsmController.variableData.wallCheck.collider != null)
            fsmController.rigid.linearVelocityX = 0f;
        else
            fsmController.rigid.linearVelocityX = fsmController.constantData.rollSpeed * fsmController.variableData.sightDirection;
    }

    public override void Exit()
    {
        fsmController.rigid.linearVelocityX = 0f;
    }
}
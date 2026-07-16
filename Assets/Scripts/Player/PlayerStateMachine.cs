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
    }
    public State playerState;  // 변하는 데이터 값이지만 예외적으로 상태 데이터이기 때문에 직접 관리

    Dictionary<State, BaseState> states;
    BaseState currentState;

    public PlayerData constantData;  // constant - 상수, 변하지 않는 데이터라는 의미로 붙인 변수명
    public PlayerRuntimeData variableData;  // variable - 변수, 변하는 데이터라는 의미로 붙인 변수명
    public PlayerAnimation PlayerAnimation;  // 애니메이션 관련 스크립트
    public Rigidbody2D rigid;
    public SpriteRenderer spriteR;

    private void Awake()
    {
        variableData = GetComponent<PlayerRuntimeData>();
        PlayerAnimation = GetComponent<PlayerAnimation>();
        rigid = GetComponent<Rigidbody2D>();
        spriteR = GetComponent<SpriteRenderer>();

        states = new Dictionary<State, BaseState>();
        states.Add(State.Idle, new IdleState(this));
        states.Add(State.Run, new RunState(this));
        states.Add(State.Jump, new JumpState(this));
        states.Add(State.Fall, new FallState(this));

        playerState = State.Idle;
        currentState = states[playerState];
        currentState.Enter();
    }

    private void Update()
    {
        switch (playerState)
        {
            case State.Idle:
                if (variableData.jumpPressed)
                {
                    ChangeState(State.Jump);
                    break;
                }
                else if(variableData.groundCheck.collider == null)
                {
                    ChangeState(State.Fall);
                    break;
                }

                if (variableData.moveDirection != 0f)
                    ChangeState(State.Run);
                break;
            case State.Run:
                if (variableData.jumpPressed)
                {
                    ChangeState(State.Jump);
                    break;
                }
                else if (variableData.groundCheck.collider == null)
                {
                    ChangeState(State.Fall);
                    break;
                }

                if (variableData.moveDirection == 0f)
                    ChangeState(State.Idle);
                break;
            case State.Jump:
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
                if (variableData.groundCheck.collider != null)
                    ChangeState(State.Idle);
                break;
        }

        currentState.Update();
    }

    private void LateUpdate()
    {
        variableData.sightDirection = spriteR.flipX ? -1f : 1f;
    }

    private void FixedUpdate()
    {
        variableData.groundCheck = Physics2D.Raycast(transform.position, Vector2.down, constantData.groundCheckDistance, constantData.groundLayer);
    }

    void ChangeState(State state)
    {
        currentState.Exit();
        currentState = states[state];
        currentState.Enter();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector2.down * constantData.groundCheckDistance);
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
        fsmController.rigid.AddForce(Vector2.up * fsmController.constantData.jumpPower, ForceMode2D.Impulse);
        fsmController.variableData.isJump = true;
        fsmController.variableData.jumpPressed = false;
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
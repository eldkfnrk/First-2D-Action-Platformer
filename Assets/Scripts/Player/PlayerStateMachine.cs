using System.Collections.Generic;
using UnityEditor.Build.Reporting;
using UnityEngine;

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
    public Rigidbody2D rigid;

    private void Awake()
    {
        variableData = GetComponent<PlayerRuntimeData>();
        rigid = GetComponent<Rigidbody2D>();

        states = new Dictionary<State, BaseState>();
        states.Add(State.Idle, new IdleState(this));
        states.Add(State.Run, new RunState(this));
        states.Add(State.Jump, new JumpState(this));
        states.Add(State.Fall, new FallState(this));

        playerState = State.Idle;
        currentState = states[playerState];
    }

    private void Update()
    {
        switch (playerState)
        {
            case State.Idle:
                if (variableData.direction != 0f)
                    ChangeState(State.Run);
                break;
            case State.Run:
                if (variableData.direction == 0f)
                    ChangeState(State.Idle);
                break;
            case State.Jump:
                break;
            case State.Fall:
                break;
        }

        currentState.Update();
    }

    void ChangeState(State state)
    {
        currentState.Exit();
        currentState = states[state];
        currentState.Enter();
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
    }

    public override void Update()
    {
        fsmController.rigid.linearVelocityX = fsmController.variableData.direction * fsmController.constantData.moveSpeed;
        Debug.Log(fsmController.rigid.linearVelocityX);
        Debug.Log(fsmController.variableData.direction);
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

    }

    public override void Update()
    {

    }

    public override void Exit()
    {

    }
}

public class FallState : BaseState
{
    public FallState(PlayerStateMachine controller) : base(controller) { }

    public override void Enter()
    {

    }

    public override void Update()
    {

    }

    public override void Exit()
    {

    }
}
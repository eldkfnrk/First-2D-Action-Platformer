using UnityEngine;

public class PlayerState : MonoBehaviour
{

}

public abstract class BaseState
{
    // 아래 두 문장에서 protected를 사용한 이유는 이 변수와 함수는 이 클래스와 상속 받는 클래스에서만 사용되어야 하기 때문이다.
    protected PlayerStateMachine fsmController;
    // 기본 값은 아예 인풋이 불가능한 상태로 둔다. 상태에서 모든 입력을 막아야 할 때 굳이 override 하지 않고 그대로 사용하면 된다.
    public virtual InputControl inputControl => InputControl.None;  // 자식들은 다른 값을 가질 수 있도록 해야 하는데 그냥 대입을 하면 변수가 되고 C#에서 변수는 virtual을 사용할 수 없기에 람다식으로 값을 대입하여 virtual이 가능하도록 한다.(람다는 함수 취급이기에 가능)

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

        if (fsmController.variableData.lowWallCheck.collider != null)
            fsmController.rigid.linearVelocityX = 0f;
        else
            fsmController.rigid.linearVelocityX = fsmController.variableData.moveDirection * fsmController.constantData.moveSpeed;

        if (fsmController.variableData.moveDirection != 0f && fsmController.variableData.sightDirection != fsmController.variableData.moveDirection)  // 바라보는 방향과 이동 방향이 반대인 경우
            fsmController.ChangeSight();
    }
}

public class IdleState : BaseState
{
    // 여기서 base는 부모 클래스의 생성자를 의미한다.(C# 문법)
    // public으로 하지 않으면 이 생성자를 호출할 수 없기 때문에 특정 경우를 제외하고는 외부에서 초기화가 가능하도록 public 선언을 해야 한다.
    public IdleState(PlayerStateMachine controller) : base(controller) { }
    public override InputControl inputControl => InputControl.All;

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Idle;
        fsmController.rigid.gravityScale = 1f;
        fsmController.playerAnimation.PlayIdle();
    }

    public override void Update()
    {
        if (fsmController.variableData.groundCheck.collider == null)
        {
            fsmController.Fall();
            return;
        }

        if (fsmController.variableData.moveDirection != 0f)
        {
            fsmController.Run();
            return;
        }
    }

    public override void Exit()
    {

    }
}

public class RunState : BaseState
{
    public RunState(PlayerStateMachine controller) : base(controller) { }
    public override InputControl inputControl => InputControl.All;

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Run;
    }

    public override void Update()
    {
        if (fsmController.variableData.groundCheck.collider == null)
        {
            fsmController.Fall();
            return;
        }

        if (fsmController.variableData.moveDirection == 0f)
        {
            fsmController.Idle();
            return;
        }

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
    }

    public override void Update()
    {
        Move();  // 점프 중에도 좌우 이동은 가능하도록 설정
        if (fsmController.variableData.groundCheck.collider != null)
        {
            // rigidbody의 AddForce는 인자로 전달된 값만큼 x축의 속도와 y축의 속도를 수정한다.(ForceMode가 Impulse면 한 번에 그 속도에 도달하고 Force이면 서서히 도달한다.)
            // 즉, rigid.AddForce를 사용해서 점프를 구현하면 점프 키를 입력함과 동시에 설정한 점프 파워 값이 y축 속도가 된다.(gravityScale 값이 1인 경우)
            // 이후 중력 값에 의해 서서히 y축 속도가 줄어드는 원리이다.
            // 그러니 점프 직후 바로 Ray에 걸려 착지 판정이 되는 문제를 y축에 속도를 이용하여 점프 직후인지를 판단하도록 해서 해결할 수 있을 것이다.
            // 현재 점프 파워는 8. y축 속도가 점프 파워 값의 -2한 값 이하이고 시작할 때부터 착지 판정이 되도록 설정하면 점프 직후에 바로 착지가 되는 문제와 Fall 상태가 되지 않아도 착지 판정을 할 수 있는 문제를 동시에 해결할 수 있다.
            // 점프 애니메이션은 Jump에서 Fall로 넘어가야만 Idle로 갈 수 있는데 점프 상태에서 Idle 상태로 이동하면 애니메이션이 Fall로 넘어가지 않아서 애니메이션 전환이 되지 않는 문제가 발생
            // 해당 문제를 해결하기 위한 방법으로 점프 상태에서 Idle 상태가 되어야 할 때 한 번 Fall 상태로 넘겨서 Fall이 바로 Idle 상태로 넘기도록 하는 방법이 있다.
            // y축 속도가 0이상의 값이고 점프 파워보다 -2한 값보다 미만이면 Fall 상태로 넘어가도록 설정(상승 중인데 땅에 착지한 것을 판단하는 범위)
            if (fsmController.rigid.linearVelocityY < 6f && fsmController.rigid.linearVelocityY >= 0f)
                fsmController.Fall();  // Idle 상태로 보내기 전 애니메이션 전환을 위한 Fall 상태로 보내기
            return;
        }

        if (fsmController.variableData.highWallCheck.collider != null)
        {
            fsmController.WallSlide();
            return;
        }

        if (fsmController.rigid.linearVelocityY < 0f)
            fsmController.Fall();
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
    }

    public override void Update()
    {
        Move();  // 떨어지는 중에도 좌우 이동은 가능하도록 설정
        if (fsmController.variableData.groundCheck.collider != null)
        {
            fsmController.Idle();
            return;
        }

        if (fsmController.variableData.highWallCheck.collider != null)
        {
            fsmController.WallSlide();
            return;
        }
    }

    public override void Exit()
    {
        if (fsmController.variableData.moveDirection == 0f)
            fsmController.rigid.linearVelocityX = 0f;
    }
}

public class WallSlideState : BaseState
{
    public WallSlideState(PlayerStateMachine controller) : base(controller) { }
    public override InputControl inputControl => InputControl.Jump;

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.WallSlide;
        fsmController.rigid.linearVelocityX = 0f;
    }

    public override void Update()
    {
        // 벽을 검사하는 Ray가 중앙에 있어서 다리 부근이 벽과 충돌하면 그냥 멈추는 문제가 존재
        // 벽을 검사하는 Ray를 2개를 둬서 2개 다 벽을 감지해야 벽과 충돌한 상황이라고 보거나 overlapbox를 이용하여 벽을 판단하거나 해야 할 것으로 추측 중이다.

        if (fsmController.variableData.groundCheck.collider != null || fsmController.variableData.highWallCheck.collider == null)
        {
            fsmController.ChangeSight();
            fsmController.Idle();
            return;
        }

        if (fsmController.variableData.downKeyPressed)
        {
            fsmController.rigid.linearVelocityY = -2f;
            fsmController.playerAnimation.SlideDustSpeedUp();
        }
        else
        {
            fsmController.rigid.linearVelocityY = 0f;
            fsmController.playerAnimation.SlideDustSpeedDown();
        }
    }

    public override void Exit()
    {
        fsmController.playerAnimation.StopSlideDust();
    }
}

public class RollState : BaseState
{
    public RollState(PlayerStateMachine controller) : base(controller) { }

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Roll;
    }

    public override void Update()
    {
        if (!fsmController.variableData.isRoll)
        {
            if (fsmController.variableData.groundCheck.collider == null)
                fsmController.Fall();
            else
                fsmController.Idle();

            return;
        }

        fsmController.rigid.linearVelocityY = 0f;
        if (fsmController.variableData.lowWallCheck.collider != null)
            fsmController.rigid.linearVelocityX = 0f;
        else
            fsmController.rigid.linearVelocityX = fsmController.constantData.rollSpeed * fsmController.variableData.sightDirection;
    }

    public override void Exit()
    {
        fsmController.rigid.linearVelocityX = 0f;
    }
}

public class AttackState : BaseState
{
    public AttackState(PlayerStateMachine controller) : base(controller) { }
    public override InputControl inputControl => InputControl.Attack | InputControl.Roll;

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Attack;
        fsmController.variableData.isAttack = true;
    }

    public override void Update()
    {
        if (fsmController.variableData.atkRoutine)
            return;

        if (!fsmController.variableData.isAttack && fsmController.variableData.rollKeyDown)
        {
            fsmController.Roll();
            return;
        }

        if (fsmController.variableData.atkKeyDownCount > fsmController.variableData.atkCount)
        {
            ++fsmController.variableData.atkCount;
            fsmController.Attack(fsmController.variableData.atkCount);
        }

        if (!fsmController.variableData.isAttack)
        {
            fsmController.Idle();
        }
    }

    public override void Exit()
    {

    }
}

public class BlockState : BaseState
{
    public BlockState(PlayerStateMachine controller) : base(controller) { }
    public override InputControl inputControl => InputControl.Jump | InputControl.Roll | InputControl.Block;

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Block;
    }

    public override void Update()
    {
        if (fsmController.variableData.successBlock)
            return;

        if (fsmController.variableData.moveDirection != 0f && fsmController.variableData.moveDirection != fsmController.variableData.sightDirection)
        {
            fsmController.ChangeSight();
            fsmController.BlockBoxChangeLoc(fsmController.variableData.sightDirection);
        }

        if (!fsmController.variableData.isBlock)
            fsmController.Idle();
    }

    public override void Exit()
    {
        fsmController.variableData.isBlock = false;  // 키를 뗄 때도 방어가 해제되지만 키를 누르고 있음에도 점프, 공격 등의 키 입력이 있으면 상태 전환이 되기 때문에 반드시 방어 상태가 아님을 알리기 위해 바꿔준다.
        fsmController.blockRangeBox.SetActive(false);
        fsmController.playerAnimation.StopBlock();
    }
}

public class HitState : BaseState
{
    public HitState(PlayerStateMachine controller) : base(controller) { }

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Hit;
    }

    public override void Update()
    {
        if (!fsmController.variableData.isHit)
        {
            if (fsmController.variableData.groundCheck.collider != null)
                fsmController.Idle();
            else
                fsmController.Fall();
        }
    }

    public override void Exit()
    {

    }
}

public class DeathState : BaseState
{
    public DeathState(PlayerStateMachine controller) : base(controller) { }

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Death;
        GameManager.instance.NotifyPlayerDeath();
    }

    public override void Update()
    {
        if (fsmController.variableData.isRevival)
        {
            if (fsmController.variableData.groundCheck.collider == null)
                fsmController.Idle();
            else
                fsmController.Fall();
        }
    }

    public override void Exit()
    {
        fsmController.playerAnimation.ParameterReset();
        fsmController.variableData.isDead = false;
        fsmController.variableData.isRevival = false;
        fsmController.coll.enabled = true;
        fsmController.variableData.curHP = fsmController.constantData.maxHp;
        fsmController.variableData.cantInput = true;
        fsmController.CantInputChange();
    }
}

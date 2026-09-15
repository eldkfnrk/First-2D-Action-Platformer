using System.Collections.Generic;
using UnityEngine;

public class BossFSM : MonoBehaviour
{
    Dictionary<Boss.ActionState, BossBaseState> states;
    BossBaseState curState;
    public Boss bossAction;

    private void Awake()
    {
        bossAction = GetComponent<Boss>();

        states = new Dictionary<Boss.ActionState, BossBaseState>();

        states.Add(Boss.ActionState.Idle, new BossIdleState(this));
        states.Add(Boss.ActionState.Move, new BossMoveState(this));
        states.Add(Boss.ActionState.Dash, new BossDashState(this));
        states.Add(Boss.ActionState.Attack, new BossAttackState(this));
    }

    private void Start()
    {
        curState = states[Boss.ActionState.Idle];
        curState.StateEnter();
    }

    void Update()
    {
        curState.StateUpdate();
    }

    public void ChangeState(Boss.ActionState state)
    {
        if (curState == null)
            return;

        if (bossAction.curActionState == state)
            return;

        curState.StateExit();
        curState = states[state];
        curState.StateEnter();
    }
}

public abstract class BossBaseState
{
    public BossFSM fsm;

    public BossBaseState(BossFSM fsm)
    {
        this.fsm = fsm;
    }

    public abstract void StateEnter();
    public abstract void StateUpdate();
    public abstract void StateExit();
}

public class BossIdleState : BossBaseState
{
    public BossIdleState(BossFSM fsm) : base(fsm) { }

    public override void StateEnter()
    {
        fsm.bossAction.EnterIdle();
    }

    public override void StateUpdate()
    {
        fsm.bossAction.ActionIdle();
    }

    public override void StateExit() { }
}

public class BossMoveState : BossBaseState
{
    public BossMoveState(BossFSM fsm) : base(fsm) { }

    public override void StateEnter()
    {
        fsm.bossAction.EnterMove();
    }

    public override void StateUpdate()
    {
        fsm.bossAction.ActionMove();
    }

    public override void StateExit() { }
}

public class BossDashState : BossBaseState
{
    public BossDashState(BossFSM fsm) : base(fsm) { }

    public override void StateEnter()
    {
        fsm.bossAction.EnterDash();
    }

    public override void StateUpdate() { }

    public override void StateExit() { }
}

public class BossAttackState : BossBaseState
{
    public BossAttackState(BossFSM fsm) : base(fsm) { }

    public override void StateEnter()
    {
        fsm.bossAction.EnterAttack();
    }

    public override void StateUpdate() { }

    public override void StateExit() { }
}
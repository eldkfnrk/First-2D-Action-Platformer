using System.Collections.Generic;
using UnityEngine;

public class EnemyFSM : MonoBehaviour
{
    public Enemy enemyType;
    public EnemyBaseState currentState;
    public Dictionary<Enemy.State, EnemyBaseState> enemyStates;

    private void Awake()
    {
        enemyType = GetComponent<Enemy>();

        enemyStates = new Dictionary<Enemy.State, EnemyBaseState>();
        enemyStates.Add(Enemy.State.Idle, new EnemyIdleState(this));
        enemyStates.Add(Enemy.State.Move, new EnemyMoveState(this));
    }

    public void ChangeState(Enemy.State state)
    {
        if (currentState != null)
            currentState.StateExit();
        currentState = enemyStates[state];
        currentState.StateEnter();
    }
}


public abstract class EnemyBaseState
{
    protected EnemyFSM fsmController;

    protected EnemyBaseState(EnemyFSM fsm)
    {
        fsmController = fsm;
    }

    public abstract void StateEnter();
    public abstract void StateUpdate();
    public abstract void StateExit();
}

public class EnemyIdleState : EnemyBaseState
{
    public EnemyIdleState(EnemyFSM fsmController) : base(fsmController) { }

    public override void StateEnter()
    {
        fsmController.enemyType.enemyState = Enemy.State.Idle;
        fsmController.enemyType.enemyAnimation.PlayIdle();
    }

    public override void StateUpdate()
    {
        
    }

    public override void StateExit()
    {
        
    }
}

public class EnemyMoveState : EnemyBaseState
{
    public EnemyMoveState(EnemyFSM fsmController) : base(fsmController) { }

    public override void StateEnter()
    {
        fsmController.enemyType.enemyState = Enemy.State.Move;
        fsmController.enemyType.enemyAnimation.PlayMove();
    }

    public override void StateUpdate()
    {
        fsmController.enemyType.EnemyMove();
    }

    public override void StateExit()
    {
        
    }
}

public class EnemyAttackState : EnemyBaseState
{
    public EnemyAttackState(EnemyFSM fsmController) : base(fsmController) { }

    public override void StateEnter()
    {
        fsmController.enemyType.enemyState = Enemy.State.Attack;
    }

    public override void StateUpdate()
    {
        
    }

    public override void StateExit()
    {

    }
}

public class EnemyHitState : EnemyBaseState
{
    public EnemyHitState(EnemyFSM fsmController) : base(fsmController) { }

    public override void StateEnter()
    {
        fsmController.enemyType.enemyState = Enemy.State.Hit;
    }

    public override void StateUpdate()
    {
        
    }

    public override void StateExit()
    {

    }
}

public class EnemyDeathState : EnemyBaseState
{
    public EnemyDeathState(EnemyFSM fsmController) : base(fsmController) { }

    public override void StateEnter()
    {
        fsmController.enemyType.enemyState = Enemy.State.Death;
    }

    public override void StateUpdate()
    {
        
    }

    public override void StateExit()
    {

    }
}
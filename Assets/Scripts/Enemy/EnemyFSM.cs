using System.Collections.Generic;
using UnityEngine;

public class EnemyFSM : MonoBehaviour
{
    public Enemy enemy;
    public EnemyBaseState currentState;
    public Dictionary<Enemy.State, EnemyBaseState> enemyStates;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        enemyStates = new Dictionary<Enemy.State, EnemyBaseState>();
        enemyStates.Add(Enemy.State.Idle, new EnemyIdleState(this));
        enemyStates.Add(Enemy.State.Move, new EnemyMoveState(this));
        enemyStates.Add(Enemy.State.Chase, new EnemyChaseState(this));
        enemyStates.Add(Enemy.State.Attack, new EnemyAttackState(this));
        enemyStates.Add(Enemy.State.Hit, new EnemyHitState(this));
        enemyStates.Add(Enemy.State.Death, new EnemyDeathState(this));
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
        fsmController.enemy.enemyState = Enemy.State.Idle;
        fsmController.enemy.variableData.moveDir = 0f;
        fsmController.enemy.enemyAnimation.PlayIdle();
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
    float plusMaxRange;
    float minusMaxRange;
    float currentEnemyPosX;

    public EnemyMoveState(EnemyFSM fsmController) : base(fsmController) {
        plusMaxRange = fsmController.enemy.variableData.spawnLoc.x + fsmController.enemy.constantData.moveMaxRange;
        minusMaxRange = fsmController.enemy.variableData.spawnLoc.x - fsmController.enemy.constantData.moveMaxRange;
    }

    public override void StateEnter()
    {
        fsmController.enemy.enemyState = Enemy.State.Move;
        fsmController.enemy.variableData.esacpeRange = false;
        fsmController.enemy.enemyAnimation.PlayMove();
    }

    public override void StateUpdate()
    {
        fsmController.enemy.EnemyMove();
        currentEnemyPosX = fsmController.transform.position.x;
        if (!fsmController.enemy.variableData.goBack)
            fsmController.enemy.variableData.esacpeRange = (currentEnemyPosX > plusMaxRange || currentEnemyPosX < minusMaxRange) ? true : false;
    }

    public override void StateExit()
    {
        fsmController.enemy.EnemyStop();
    }
}

public class EnemyChaseState : EnemyBaseState
{
    public EnemyChaseState(EnemyFSM fsmController) : base(fsmController) { }

    public override void StateEnter()
    {
        fsmController.enemy.enemyState = Enemy.State.Chase;
        fsmController.enemy.enemyAnimation.PlayMove();
    }

    public override void StateUpdate()
    {
        fsmController.enemy.EnemyChaseMove();
        if (fsmController.enemy.variableData.playerEnemyXDistance >= fsmController.enemy.constantData.maxDistance)
            fsmController.enemy.variableData.goBack = true;
    }

    public override void StateExit()
    {
        fsmController.enemy.EnemyStop();
    }
}

public class EnemyAttackState : EnemyBaseState
{
    public EnemyAttackState(EnemyFSM fsmController) : base(fsmController) { }

    public override void StateEnter()
    {
        fsmController.enemy.enemyState = Enemy.State.Attack;
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
        fsmController.enemy.enemyState = Enemy.State.Hit;
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
        fsmController.enemy.enemyState = Enemy.State.Death;
    }

    public override void StateUpdate()
    {
        
    }

    public override void StateExit()
    {

    }
}
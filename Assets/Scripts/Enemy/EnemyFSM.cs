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
        enemyStates.Add(Enemy.State.GoBack, new EnemyGoBackState(this));
        enemyStates.Add(Enemy.State.Attack, new EnemyAttackState(this));
        enemyStates.Add(Enemy.State.Hit, new EnemyHitState(this));
        enemyStates.Add(Enemy.State.Death, new EnemyDeathState(this));
    }

    public void ChangeTransitions()
    {
        // 반드시 확인해야 하는 피격 및 사망 체크는 동작을 관장하는 각 스크립트의 상태 체크가 아닌 이 함수에서 통합 관리한다.
        // 예를 들어 피격과 사망 상태 돌입은 거의 모든 상황에서 동시에 진행해야 하기 때문에 여기서 상태 전환을 수행하는 것이다.
        // 그리고 이 피격과 사망에서도 우선 순위를 둬서 순서대로 체크하도록 하여야 한다.
        // 우선 순위는 사망 - 피격 순이다.
        if (enemy.variableData.isDead)
        {
            ChangeState(Enemy.State.Death);
            return;
        }

        if (enemy.variableData.isHit)
        {
            ChangeState(Enemy.State.Hit);
            return;
        }
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
    public EnemyMoveState(EnemyFSM fsmController) : base(fsmController) { }

    public override void StateEnter()
    {
        fsmController.enemy.enemyState = Enemy.State.Move;
        fsmController.enemy.enemyAnimation.PlayMove();
    }

    public override void StateUpdate()
    {
        fsmController.enemy.EnemyMove();
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
        if (fsmController.enemy.variableData.cantMove)
            return;

        fsmController.enemy.EnemyChaseMove();
        if (fsmController.enemy.variableData.playerEnemyXDistance >= fsmController.enemy.constantData.maxDistance
            || fsmController.enemy.variableData.floorCheck.collider == null || fsmController.enemy.variableData.frontCheck.collider != null)
        {
            fsmController.enemy.variableData.cantMove = true;
            fsmController.enemy.enemyAnimation.PlayIdle();
        }
    }

    public override void StateExit()
    {
        fsmController.enemy.variableData.cantMove = false;
    }
}

public class EnemyGoBackState : EnemyBaseState
{
    public EnemyGoBackState(EnemyFSM fsmController) : base(fsmController) { }

    public override void StateEnter()
    {
        fsmController.enemy.enemyState = Enemy.State.GoBack;
        fsmController.enemy.ChangeDirection();
        fsmController.enemy.enemyAnimation.PlayMove();
    }

    public override void StateUpdate()
    {
        fsmController.enemy.EnemyMove();
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
        fsmController.enemy.actionTimer = 0f;
        fsmController.enemy.EnemyStop();
        fsmController.enemy.variableData.isHit = false;
        Debug.Log("Hit" + fsmController.gameObject.name);
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
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

    private void Start()
    {
        currentState = enemyStates[Enemy.State.Move];
        currentState.StateEnter();
    }

    public void ChangeState(Enemy.State state)
    {
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
        fsmController.enemyType.actionTimer += Time.deltaTime;
    }

    public override void StateUpdate()
    {
        fsmController.enemyType.actionTimer += Time.deltaTime;
    }

    public override void StateExit()
    {
        fsmController.enemyType.actionTimer = 0f;
    }
}

public class EnemyMoveState : EnemyBaseState
{
    public EnemyMoveState(EnemyFSM fsmController) : base(fsmController) { }

    public override void StateEnter()
    {
        fsmController.enemyType.enemyState = Enemy.State.Move;
        fsmController.enemyType.actionTimer += Time.deltaTime;
    }

    public override void StateUpdate()
    {
        fsmController.enemyType.Move();
        fsmController.enemyType.actionTimer += Time.deltaTime;
    }

    public override void StateExit()
    {
        fsmController.enemyType.actionTimer = 0f;
    }
}
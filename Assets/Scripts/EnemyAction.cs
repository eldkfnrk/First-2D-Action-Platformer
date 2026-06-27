using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class EnemyAction : MonoBehaviour
{
    public enum EnemyActionState
    {
        Idle,
        Run,
        Fall,
        Dash,
        DashAttack,
        Attack,
        Slide,
        Hurt,
        Croush,
        Jump,
        Death,
    }

    public EnemyActionState actionState;

    public enum EnemyState
    {
        Phase1,
        Phase2,
        None,
    }

    public EnemyState enemyState;

    
}

public abstract class BaseState
{
    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void ExitState();
}

public class IdleState : BaseState
{
    public override void EnterState()
    {

    }
    public override void UpdateState()
    {

    }
    public override void ExitState()
    {

    }
}

public class RunState : BaseState
{
    public override void EnterState()
    {

    }
    public override void UpdateState()
    {

    }
    public override void ExitState()
    {

    }
}

public class DashState : BaseState
{
    public override void EnterState()
    {

    }
    public override void UpdateState()
    {

    }
    public override void ExitState()
    {

    }
}
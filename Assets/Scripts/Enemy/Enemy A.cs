using System.Collections;
using UnityEngine;

public class EnemyA : Enemy
{
    // 간단하게 캐릭터 상태 전환 시험용으로 일정 시간이 지나면 상태가 변하도록 만들어 보고 이를 바탕으로 FSM을 적용해 볼 예정
    // 이 적은 이동과 피격, 사망 상태가 있다.

    public override void RealizePlayerDeath()
    {
        if (variableData.isAttack)
        {
            variableData.isAttack = false;
            variableData.isCrush = false;
            fsm.ChangeState(State.Move);
        }
    }

    public override void ActionMove()
    {
        EnemyMove();
    }

    public override void ActionHit()
    {
        if (!variableData.isHit)
        {
            StateMove();
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector2.right * variableData.sightDirection * constantData.frontCheckDistance);
        Gizmos.DrawRay(variableData.floorCheckOrigin, Vector2.down * constantData.floorCheckDistance);
    }
}

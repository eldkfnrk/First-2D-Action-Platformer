using System.Collections;
using UnityEngine;

public class EnemyA : Enemy
{
    // 간단하게 캐릭터 상태 전환 시험용으로 일정 시간이 지나면 상태가 변하도록 만들어 보고 이를 바탕으로 FSM을 적용해 볼 예정
    // 이 적은 이동과 피격, 사망 상태가 있다.

    private void Awake()
    {
        fsm = GetComponent<EnemyFSM>();
        rigid = GetComponent<Rigidbody2D>();
        sprtieR = GetComponent<SpriteRenderer>();
        variableData = GetComponent<EnemyRuntimeData>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        variableData.spawnLoc = transform.position;

        variableData.sightDirection = sprtieR.flipX ? 1f : -1f;
    }

    private void Update()
    {
        switch (enemyState)
        {
            case State.Move:
                if (variableData.isHit)
                    fsm.ChangeState(State.Hit);
                break;
            case State.Hit:
                if (!variableData.isHit)
                    fsm.ChangeState(State.Move);
                break;
        }

        fsm.ChangeTransitions();
        fsm.currentState.StateUpdate();
    }

    private void FixedUpdate()
    {
        WallFloorCheck();
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector2.right * variableData.sightDirection * constantData.frontCheckDistance);
        Gizmos.DrawRay(variableData.floorCheckOrigin, Vector2.down * constantData.floorCheckDistance);
    }
}

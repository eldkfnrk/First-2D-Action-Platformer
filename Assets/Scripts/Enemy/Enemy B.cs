using System.Collections;
using UnityEngine;

public class EnemyB : Enemy
{
    int changeActionRanNum;

    // 일정 범위 내에서 움직이다가 플레이어를 탐지하면 플레이어를 향해 달려드는 몬스터
    // "이동 - 멈춤 - 이동" 혹은 "이동 - 방향 전환 - 이동" 같이 정해진 범위 내에서 이동과 정지, 방향 전환 등을 수행
    // 그러다 플레이어를 탐지하면 플레이어를 향해 돌진
    // 이때 일정 거리 이상으로 떨어지면 더 이상 따라오지 않고 다시 원래 있던 곳으로 돌아가서 동일한 행동 수행
    // 탐지 범위는 몬스터의 앞으로 일정 거리의 범위, 뒤로는 가까이 오면 탐지할 수 있게 앞보다는 적은 범위
    // 따라가지 않는 거리는 플레이어와 몬스터 사이의 x축 거리를 사용할 것이고 이 거리는 몬스터의 탐지 범위보다 더 길게 설정할 예정
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

    private void Start()
    {
        fsm.ChangeState(State.Move);
    }

    private void Update()
    {
        Debug.Log(enemyState);
        switch (enemyState)
        {
            case State.Idle:
                actionTimer += Time.deltaTime;
                if (ChangeStateChase())
                {
                    actionTimer = 0f;
                    fsm.ChangeState(State.Move);
                }
                ChangeAction(State.Move);
                break;
            case State.Move:
                if (ChangeStateChase())
                {
                    actionTimer = 0f;
                    break;
                }

                actionTimer += Time.deltaTime;

                ChangeAction(State.Idle);
                break;
            case State.Chase:
                if (variableData.cantMove)
                {
                    actionTimer += Time.deltaTime;
                    if (actionTimer >= 1f)
                    {
                        actionTimer = 0f;
                        fsm.ChangeState(State.GoBack);
                    }
                }
                break;
            case State.GoBack:
                if (ChangeStateChase())
                {
                    fsm.ChangeState(State.Chase);
                    break;
                }

                if (Mathf.Abs(variableData.spawnLoc.x - transform.position.x) < 0.1f)
                    fsm.ChangeState(State.Idle);
                break;
            case State.Hit:
                actionTimer += Time.deltaTime;
                if (actionTimer > 0.5f)
                {
                    actionTimer = 0f;
                    fsm.ChangeState(State.Idle);
                }
                break;
        }

        fsm.ChangeTransitions();
        fsm.currentState.StateUpdate();
    }

    private void FixedUpdate()
    {
        WallFloorCheck();
    }

    bool ChangeStateChase()
    {
        if (DetectPlayer())
        {
            fsm.ChangeState(State.Chase);
            variableData.cantMove = false;
            actionTimer = 0f;
            return true;
        }

        return false;
    }

    void ChangeAction(State changeState)
    {
        if (actionTimer < 1.5f)
            return;

        actionTimer = 0f;
        changeActionRanNum = Random.Range(1, 11);

        // 20% 확률로 상태 전환
        if (changeActionRanNum > 8)
        {
            fsm.ChangeState(changeState);
        }
    }

    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.red;
        //Gizmos.DrawWireCube(variableData.detectPlayerBoxPos, constantData.detectPlayerBoxSize);
    }
}

using UnityEngine;

public class EnemyA : Enemy
{
    // 간단하게 캐릭터 상태 전환 시험용으로 일정 시간이 지나면 상태가 변하도록 만들어 보고 이를 바탕으로 FSM을 적용해 볼 예정

    private void Awake()
    {
        fsm = GetComponent<EnemyFSM>();
        rigid = GetComponent<Rigidbody2D>();
        sprtieR = GetComponent<SpriteRenderer>();
        variableData = GetComponent<EnemyRuntimeData>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        enemyAnimation.PlayIdle();  // 이 적은 애니메이션이 하나이기 때문에 딱 한 번 애니메이션을 호출한다.

        variableData.sightDirection = sprtieR.flipX ? 1f : -1f;
    }

    private void Update()
    {
        switch (enemyState)
        {
            case State.Idle:
                if (actionTimer > 2f)
                    fsm.ChangeState(State.Move);
                break;
            case State.Move:
                if (actionTimer > 2f)
                    fsm.ChangeState(State.Idle);
                break;
        }

        fsm.currentState.StateUpdate();
    }

    private void FixedUpdate()
    {
        variableData.frontCheck = Physics2D.Raycast(transform.position, Vector2.right * variableData.sightDirection, constantData.frontCheckDistance, constantData.groundLayer);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector2.right * variableData.sightDirection * constantData.frontCheckDistance);
    }

    // 모든 적의 공통점
    // 적과 충돌 시 플레이어는 반드시 뒤로 넉백이 일어나며 데미지를 입는다.

    // 좌우로만 이동하는 몬스터
    // 앞에 벽이 있거나 앞에 땅이 없다면 방향 전환하는 방식으로 행동
    // 방향 전환이 있기 전까지는 한 방향으로 계속하여 전진
    // 플레이어에게 데미지를 입으면 잠깐의 시간 동안 정지 후 다시 이동(이때 캐릭터는 애니메이션이 작동하지 않도록 설정)

    //public float moveSpeed;
    //public float frontCheckDistance;
    //public float floorCheckDistance;
    //public LayerMask floorLayer;
    //float direction;
    //RaycastHit2D frontCheck;
    //RaycastHit2D floorCheck;

    //// 넉백을 위한 딜레이 시간
    //public float knockbackTime;
    //WaitForSeconds knockbackDelay;

    //public RuntimeAnimatorController enemyAnimController;
    //Animator animator;
    //Rigidbody2D rigid;
    //SpriteRenderer spriteR;

    //private void Awake()
    //{
    //    animator = GetComponent<Animator>();
    //    animator.runtimeAnimatorController = enemyAnimController;
    //    rigid = GetComponent<Rigidbody2D>();
    //    spriteR = GetComponent<SpriteRenderer>();
    //    direction = Random.Range(0, 2) == 0 ? -1f : 1f;  // 랜덤하게 진행 방향 설정(0이 나오면 왼쪽 1이 나오면 오른쪽으로 진행)
    //    spriteR.flipX = direction == 1f ? true : false;
    //    knockbackDelay = new WaitForSeconds(knockbackTime);
    //}

    //private void FixedUpdate()
    //{
    //    frontCheck = Physics2D.Raycast(transform.position, Vector2.right * direction, frontCheckDistance, floorLayer);
    //    floorCheck = Physics2D.Raycast(transform.position, Vector2.down, floorCheckDistance, floorLayer);

    //    if (frontCheck.collider != null || floorCheck.collider == null)
    //    {
    //        direction *= -1f;
    //        spriteR.flipX = !spriteR.flipX;
    //    }

    //    rigid.linearVelocityX = direction * moveSpeed;
    //}

    //// 임시 피격 함수
    //public void Hit()
    //{
    //    Debug.Log(gameObject.name + " hit");
    //}

    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawRay(transform.position, Vector2.right * direction * frontCheckDistance);
    //    Gizmos.color = Color.darkRed;
    //    Gizmos.DrawRay(transform.position, Vector2.down * floorCheckDistance);
    //}
}

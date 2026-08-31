using System.Collections;
using UnityEngine;

public class EnemyC : Enemy
{
    // 기본적으로 날아다니는 상태(Idle)
    // 이 적은 자신의 영역이 있고 이 영역 안에 들어온 플레이어를 공격하는 적
    // 영역을 벗어나면 더 이상 공격하지 않음
    // 플레이어 탐지 전까지는 자신이 있는 자리에서 제자리 비행
    // 플레이어 탐지 시 경계 상태로 전환(Chase)
    // 경계 태세에서는 고도를 낮추고 플레이어와 가까워지도록 이동
    // 일정 시간 후 하늘에서 내리꽂는 다이브 공격을 실행(Attack)
    // 다이브 공격은 바닥과 충돌하면 종료
    // 공격 종료 이후 바로 상승하여 다시 경계 태세로 전환(Chase)
    // 플레이어가 영역에서 일정 범위를 벗어나면 더 이상 경계하지 않고 원래 자리로 귀환(GoBack->Idle)

    ContactFilter2D playerFilter;
    Collider2D[] detectPlayer;

    float goHighTimer;

    protected override void Awake()
    {
        base.Awake();
        variableData.detectPlayerBoxPos = (Vector2)transform.position + constantData.detectPlayerBoxOffset;  // 이 타입의 적은 자신의 영역의 침범을 검사하는 것이기에 고정된 위치를 사용. 그래서 이를 Awake에서 적용.
        detectPlayer = new Collider2D[4];  // 플레이어의 하위 오브젝트가 추가될 수 있고 그 중 판단해야 하는 것이 있을 수 있으니 넉넉한 크기로 잡고 수행
        playerFilter.useLayerMask = true;
        playerFilter.SetLayerMask(constantData.playerLayer);
    }

    protected override void Start()
    {
        StateIdle();
    }

    protected override void OnEnable()
    {
        variableData.curHP = constantData.maxHP;
        variableData.isDead = false;
        if (GameManager.instance != null)
        {
            GameManager.instance.playerDeathEvent -= RealizePlayerDeath;
            GameManager.instance.playerDeathEvent += RealizePlayerDeath;
        }
        if (fsm == null)
            return;
        StateIdle();
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (variableData.isAttack)
            {
                variableData.isCrush = true;
                EnemyStop();
                CrushRoutineStart();
            }
            else
            {

            }
        }
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Block"))
        {
            if (variableData.isAttack)
            {
                variableData.isCrush = true;
                EnemyStop();
                CrushRoutineStart();
            }
            else
            {

            }
        }
    }

    //public override bool DetectPlayer()
    //{
    //    int isDetect = Physics2D.OverlapBox(variableData.detectPlayerBoxPos, variableData.detectPlayerBoxSize, 0, playerFilter, detectPlayer);

    //    return isDetect == 1;
    //}

    public override bool ArriveSpawnLoc()
    {
        if (Mathf.Abs(variableData.spawnLoc.x - transform.position.x) < 0.1f)
        {
            StateIdle();
            return true;
        }

        return false;
    }

    public override void StateMove()
    {
        fsm.ChangeState(State.Move);
        variableData.moveDir.x = 0f;
        variableData.moveDir.y = 1f;
        enemyAnimation.PlayIdle();
    }

    public override void StateChase()
    {
        fsm.ChangeState(State.Chase);
        variableData.cantMove = false;
        actionTimer = 0f;
        enemyAnimation.PlayIdle();
        rigid.linearVelocityY = -0.5f;
        //variableData.detectPlayerBoxSize.x += 6f;
    }

    public override void StateGoBack()
    {
        fsm.ChangeState(State.GoBack);
        variableData.cantMove = false;
        variableData.moveDir = (variableData.spawnLoc - transform.position).normalized;
        float goBackDirection = variableData.moveDir.x / Mathf.Abs(variableData.moveDir.x);
        if (goBackDirection != variableData.sightDirection)
            ChangeDirection();
    }

    public override void StateHit()
    {
        // 공격 상황에서 피격 당하면 이 함수가 여러 번 호출되고 있는 문제 발생
        // isHit가 false가 되어야 피격이 중지되는데 isHit를 false로 바꾸는 작업이 진행되지 않아 FSM에서 계속 Hit 상태가 되도록 호출하였기 때문이었다.
        actionTimer = 0f;
        variableData.curHP -= 1f;
        
        // 바닥이나 벽, 플레이어 등과 충돌해서 더 이상 공격 동작은 아니지만 공격 상태인 경우 피격 동작을 하도록 isCrush를 검사
        if(enemyState != State.Attack || variableData.isCrush)
        {
            variableData.isCrush = false;
            variableData.isAttack = false;
            fsm.ChangeState(State.Hit);
            EnemyStop();
            EnemyHit();
        }
        else
        {
            variableData.isHit = false;  // 공격 중이니 데미지만 입고 더 이상의 피격은 일어나지 않도록 설정이 필요함
            // 만약 데미지 이펙트가 있다면 그것을 재생시키는 코드 추가
        }

    }

    public override void ActionIdle()
    {
        // 다른 동작을 넣을 것은 없지만 이 함수를 그대로 사용하면 Move 상태로 변하기 때문에 override 해서 내용을 비워두었다. 추후에 추가할 내용이 있다면 추가할 예정
    }

    public override void ActionMove()
    {
        EnemyMove();
        ChangeAction();
    }

    public override void ActionChase()
    {
        EnemyChaseMove();
    }

    public override void ActionAttack()
    {
        if (!variableData.isAttack)
        {
            actionTimer = 0f;
            StateMove();
            return;
        }

        if (variableData.isCrush)
            return;

        actionTimer += Time.deltaTime;

        if (variableData.frontCheck.collider != null || variableData.floorCheck.collider != null)
        {
            variableData.isCrush = true;
            actionTimer = 0f;
            EnemyStop();
            CrushRoutineStart();
        }

        // 플레이어, 벽, 바닥 이 3가지 중 하나와도 충돌하지 않고 2초가 지나면 일정 시간 경직을 추고 다시 Chase 상태로 바꾸도록 진행
        if (actionTimer > 2.5f)
        {
            actionTimer = 0f;
            StartCoroutine(AttackStopRoutine());
        }
    }

    IEnumerator AttackStopRoutine()
    {
        variableData.isCrush = true;
        EnemyStop();
        yield return new WaitForSeconds(0.3f);
        variableData.isCrush = false;
        variableData.isAttack = false;
        StateChase();
    }

    [SerializeField] private float boundaryHeight;

    public override void ActionHit()
    {
        if (!variableData.isHit)
        {
            if (transform.position.y - GameManager.instance.player.transform.position.y < boundaryHeight)
                fsm.ChangeState(State.Move);
            else
                fsm.ChangeState(State.Chase);
        }
    }

    // 아이디어
    // 1. 적이 공격이 끝나면 자신의 범위 내에서 플레이어를 탐지
    // 2. 탐지를 하지 못했다면 영역 내에 있는지 탐지
    // 3. 피격 시 경계 상태로 전환 - 경계 상태에서는 탐지 범위를 카메라 범위까지 증가(이를 따로 측정하여 지정)
    // 4. 플레이어의 카메라 범위에 적이 없다면 해당 적은 경계 태세를 풀고 원래 자리로 돌아가도록 설정
    // 5. 타입 C 적은 자신의 영역이 있고 경계 상태가 된 후에는 자신의 탐지 범위가 있도록 설정
    // 6. 이러면 많이 멀어져도 플레이어를 쫓도록 만들 수 있고 돌아가다가 등을 노리는 플레이어를 탐지할 수도 있기 때문으로 각각 동작하도록 설정

    // Hit 상태가 될 때 주의점
    // 탐지 범위를 따로 고정하는 값이 필요(Chase나 Move 등에서도 활용할 수 있도록 설정)
    // 공통점(Attack 제외) - 속도가 있었을 것을 가정하고 모든 속도를 일시적으로 0으로 만든다.
    // Idle - 없음
    // Chase - 공격을 하기 위한 대기 시간(actionTimer)을 0.25초 정도만 줄여서 공격 준비 시간을 
    // Attack - 데미지만 입고 공격은 지속되도록 설정해야 한다.
    // Move - 바로 Chase 상태가 되도록 하기 위해 필요한 설정들을 확인
    // GoBack - 없음

    // Hit 상태 동작
    // 피격된 방향의 반대 방향으로 넉백 발생
    // 일시적으로 행동 불가

    // Hit 상태 후 동작
    // Hit -> Chase -> Attack or GoBack
    // 수정본 : Hit -> 플레이어와 이 오브젝트의 높이 차이를 확인하여 일정 수치 이하라면 Move 일정 수치 위에 있다면 Chase 상태로 전환
    // 해당 적에만 있는 변수를 하나 만들고 여기에 수치를 저장하여 활용할 계획

    // 버그 리포트
    // 플레이어를 향한 돌진 공격을 하고 벽이나 바닥, 플레이어를 만나지 않으면 멈추지 않는 논리적 오류가 발생
    // 공격 후 Move 상태가 될 때 피격을 당하면 바로 Chase 상태가 되어버리고 제대로 된 애니메이션 전환 및 공격 진행이 안 되는 논리적 오류 발생
    // 공격 중에도 동일한 문제 발생


    public override void ChangeAction()
    {
        goHighTimer += Time.deltaTime;

        if(goHighTimer > 1f)
        {
            if (!DetectPlayer())
                StateGoBack();
            else
                StateChase();

            goHighTimer = 0f;
            //variableData.detectPlayerBoxSize.x -= 6f;
        }
    }

    public override void EnemyMove()
    {
        rigid.linearVelocity = variableData.moveDir * constantData.moveSpeed;
    }

    public override void EnemyChaseMove()
    {
        actionTimer += Time.deltaTime;
        if (actionTimer > 1f)
        {
            StateAttack();
            return;
        }

        variableData.playerEnemyXDistance = transform.position.x - GameManager.instance.player.transform.position.x;
        variableData.moveDir.x = variableData.playerEnemyXDistance / Mathf.Abs(variableData.playerEnemyXDistance);
        rigid.linearVelocityX = variableData.moveDir.x * constantData.moveSpeed * 0.25f;  // 평시 속도의 반의 반 정도 속도로 플레이어에게서 x축으로 멀어지도록 설정

        // 이 적은 플레이어를 바라보면서 멀어질 것이기 때문에 바라보는 방향과 이동 방향이 다르도록 설정
        if (variableData.moveDir.x == variableData.sightDirection)
            ChangeDirection();
    }

    public override void EnemyAttack()
    {
        enemyAnimation.PlayAttack();
        variableData.attackDir = (GameManager.instance.player.transform.position - transform.position).normalized;
        rigid.linearVelocity = variableData.attackDir * constantData.diveSpeed;
    }

    protected override IEnumerator HitRoutine()
    {
        // 피격 시 넉백 발생
        rigid.AddForce(variableData.knockbackDir * constantData.knockbackPower, ForceMode2D.Impulse);
        enemyAnimation.PlayHit();  // 현재는 여기서 애니메이션을 재생하지만 추후에는 상태 변경 시 재생하는 것으로 변경 예정
        yield return new WaitForSeconds(0.15f);

        EnemyStop();

        yield return new WaitForSeconds(0.15f);
        variableData.isHit = false;
        if (variableData.curHP <= 0f)
        {
            variableData.curHP = 0f;
            variableData.isDead = true;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.red;
        //Gizmos.DrawWireCube(variableData.detectPlayerBoxPos, variableData.detectPlayerBoxSize);
        Gizmos.DrawRay(variableData.floorCheckOrigin, Vector2.down * constantData.floorCheckDistance);
        Gizmos.DrawRay(transform.position, Vector2.right * constantData.frontCheckDistance * variableData.sightDirection);
    }
}

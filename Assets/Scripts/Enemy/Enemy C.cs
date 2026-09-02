using System.Collections;
using UnityEngine;

public class EnemyC : Enemy
{
    ContactFilter2D playerFilter;
    Collider2D[] detectPlayer;
    int isDetect;

    // 이 적에서만 사용할 데이터이기 때문에 따로 선언
    Vector2 territoryCenterPoint;  // 영역 중심점

    float goHighTimer;

    protected override void Awake()
    {
        base.Awake();
        territoryCenterPoint = (Vector2)transform.position + constantData.detectPlayerBoxOffset;  // 이 타입의 적은 자신의 영역의 침범을 검사하는 것이기에 고정된 위치를 사용. 그래서 이를 Awake에서 적용.
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

    public override bool DetectPlayer()
    {
        variableData.detectPlayerBoxPos.x = transform.position.x + variableData.sightDirection * constantData.detectPlayerBoxOffset.x;
        variableData.detectPlayerBoxPos.y = transform.position.y + constantData.detectPlayerBoxOffset.y;
        variableData.detectPlayer = Physics2D.BoxCast(variableData.detectPlayerBoxPos, constantData.detectPlayerBoxSize, 0f, Vector2.zero, 0f, constantData.playerLayer);

        // 앞에 벽이 있는 경우 탐지하지 못하는 것으로 설정
        if (variableData.frontCheck.collider != null)
            return false;

        return variableData.detectPlayer.collider != null;
    }

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
        isDetect = Physics2D.OverlapBox(territoryCenterPoint, constantData.detectPlayerBoxSize, 0, playerFilter, detectPlayer);
        if (isDetect == 1)
        {
            StateChase();
            return;
        }

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
        Gizmos.DrawWireCube(variableData.detectPlayerBoxPos, constantData.detectPlayerBoxSize);
        Gizmos.DrawRay(variableData.floorCheckOrigin, Vector2.down * constantData.floorCheckDistance);
        Gizmos.DrawRay(transform.position, Vector2.right * constantData.frontCheckDistance * variableData.sightDirection);
    }
}

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

    public override bool DetectPlayer()
    {
        int isDetect = Physics2D.OverlapBox(variableData.detectPlayerBoxPos, constantData.detectPlayerBoxSize, 0, playerFilter, detectPlayer);

        return isDetect == 1;
    }

    public override void StateChase()
    {
        rigid.linearVelocityY = -0.5f;
        base.StateChase();
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
            StateMove();
            return;
        }

        if (variableData.isCrush)
            return;

        if(variableData.frontCheck.collider != null || variableData.floorCheck.collider != null)
        {
            variableData.isCrush = true;
            EnemyStop();
            CrushRoutineStart();
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
        rigid.linearVelocityY = constantData.moveSpeed;
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
        variableData.moveDir = variableData.playerEnemyXDistance / Mathf.Abs(variableData.playerEnemyXDistance);
        rigid.linearVelocityX = variableData.moveDir * constantData.moveSpeed * 0.25f;  // 평시 속도의 반의 반 정도 속도로 플레이어에게서 x축으로 멀어지도록 설정

        // 이 적은 플레이어를 바라보면서 멀어질 것이기 때문에 바라보는 방향과 이동 방향이 다르도록 설정
        if (variableData.moveDir == variableData.sightDirection)
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

using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    // 모든 적의 공통점
    // 적과 충돌 시 플레이어는 반드시 뒤로 넉백이 일어나며 데미지를 입는다.

    public enum State
    {
        Idle,
        Move,
        Chase,
        GoBack,
        Attack,
        Hit,
        Death,
    }

    public State enemyState;
    protected EnemyFSM fsm;
    protected Rigidbody2D rigid;
    protected SpriteRenderer sprtieR;
    public EnemyRuntimeData variableData;
    public EnemyData constantData;
    public EnemyAnimation enemyAnimation;
    public float actionTimer;

    int ranNum;

    protected void Start()
    {
        StateMove();
    }

    protected virtual void OnEnable()
    {
        variableData.curHP = constantData.maxHP;
        variableData.isDead = false;
        GameManager.instance.playerDeathEvent += RealizePlayerDeath;
        if (fsm == null)  // fsm 컴포넌트가 없다는 것은 아직 Awake가 진행되지 않았다는 것을 의미 -> Awake 하기 전 즉, 게임 시작인 경우에는 이 밑으로는 동작하지 않도록 제한하는 역할(다른 Awake에서 초기화하는 컴포넌트도 가능. 그러나 이런 구조는 약점이 있으니 수정 필요)
            return;
        StateMove();
    }

    protected virtual void OnDisable()
    {
        GameManager.instance.playerDeathEvent -= RealizePlayerDeath;
    }

    protected void InitializeComponents()
    {
        fsm = GetComponent<EnemyFSM>();
        rigid = GetComponent<Rigidbody2D>();
        sprtieR = GetComponent<SpriteRenderer>();
        variableData = GetComponent<EnemyRuntimeData>();
        enemyAnimation = GetComponent<EnemyAnimation>();
    }

    public virtual void RealizePlayerDeath()
    {
        // 공격 중도 아니였고 추격 중도 아니었다면 아무 동작을 하지 않아도 되기에 바로 함수 종료
        if (enemyState != State.Chase && enemyState != State.Attack)
            return;

        // 플레이어를 공격 중이었다면 이 공격 중인 상황을 종료하기 위하여 변수 값 변환
        if (variableData.isAttack)
        {
            variableData.isAttack = false;
            variableData.isCrush = false;
        }

        // 플레이어 사망 후 잠시 멈췄다가 제자리로 돌아가도록 하기 위하여 Chase 상태로 돌아가서 GoBack 상태로 바꿀 수 있는 변수 값을 변환
        variableData.cantMove = true;
        EnemyStop();
        enemyAnimation.PlayIdle();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            variableData.isCrush = true;
            StateAttack();
        }
    }

    public virtual void WallFloorCheck()
    {
        variableData.floorCheckOrigin.x = transform.position.x + constantData.floorCheckOffsetX * variableData.sightDirection;
        variableData.floorCheckOrigin.y = transform.position.y;
        variableData.frontCheck = Physics2D.Raycast(transform.position, Vector2.right * variableData.sightDirection, constantData.frontCheckDistance, constantData.groundLayer);
        variableData.floorCheck = Physics2D.Raycast(variableData.floorCheckOrigin, Vector2.down, constantData.floorCheckDistance, constantData.groundLayer);
    }

    // 플레이어 탐지
    public bool DetectPlayer()
    {
        variableData.detectPlayerBoxPos.x = transform.position.x + variableData.sightDirection * constantData.detectPlayerBoxOffset.x;
        variableData.detectPlayerBoxPos.y = transform.position.y + constantData.detectPlayerBoxOffset.y;
        variableData.detectPlayer = Physics2D.BoxCast(variableData.detectPlayerBoxPos, constantData.detectPlayerBoxSize, 0f, Vector2.zero, 0f, constantData.playerLayer);

        // 앞에 벽이 있는 경우 탐지하지 못하는 것으로 설정
        if(variableData.frontCheck.collider != null)
            return false;

        return variableData.detectPlayer.collider != null;
    }

    // 바라보는 방향 전환
    public void ChangeDirection()
    {
        variableData.sightDirection *= -1f;
        sprtieR.flipX = !sprtieR.flipX;
    }

    // 적 타입 B를 기준으로 작성(다른 타입의 적들은 각각 알맞게 상태 전환)
    public virtual void ChangeAction()
    {
        if (actionTimer < 1.5f)
        {
            actionTimer += Time.deltaTime;
            return;
        }

        actionTimer = 0f;

        ranNum = Random.Range(1, 11);

        // 20% 확률로 상태 전환
        if (ranNum > 8)
        {
            switch (enemyState)
            {
                case State.Idle:
                    StateMove();
                    break;
                case State.Move:
                    StateIdle();
                    break;
            }
        }
    }

    // 각 상태로 전환되는 함수들

    public virtual void StateIdle()
    {
        variableData.moveDir = 0f;
        fsm.ChangeState(State.Idle);
        enemyAnimation.PlayIdle();
    }

    public virtual void ActionIdle()
    {
        ChangeAction();
    }

    public virtual void StateMove()
    {
        fsm.ChangeState(State.Move);
        enemyAnimation.PlayMove();
    }

    public virtual void ActionMove()
    {
        EnemyMove();
        ChangeAction();
    }

    public virtual void StateChase()
    {
        fsm.ChangeState(State.Chase);
        variableData.cantMove = false;
        actionTimer = 0f;
        enemyAnimation.PlayMove();
    }

    public void ChangeChase()
    {
        actionTimer = 0f;
        variableData.cantMove = false;
    }

    public virtual void StateGoBack()
    {
        fsm.ChangeState(State.GoBack);
        variableData.cantMove = false;
        ChangeDirection();
        enemyAnimation.PlayMove();
    }

    public void ChangeGoBack()
    {
        actionTimer += Time.deltaTime;

        if (actionTimer > 1f)
        {
            actionTimer = 0f;
            StateGoBack();
        }
    }

    // GoBack 상태에서 스폰한 위치에 도착하면 상태 전환을 하기 위한 함수
    public bool ArriveSpawnLoc()
    {
        if (Mathf.Abs(variableData.spawnLoc.x - transform.position.x) < 0.1f)
        {
            StateIdle();
            return true;
        }

        return false;
    }

    public virtual void StateAttack()
    {
        variableData.isAttack = true;
        fsm.ChangeState(State.Attack);
        if (variableData.isCrush)  // 플레이어와의 충돌인가
        {
            // 충돌 루틴 시행
            EnemyStop();
            EnemyCrush();
        }
        else  // 공격 동작을 수행하고 있는가
        {

        }
    }

    public virtual void StateHit()
    {
        fsm.ChangeState(State.Hit);
        EnemyStop();
        EnemyHit();
        // 피격 애니메이션 있거나 피격 이펙트를 만든 경우 여기서 재생 예정
    }

    public virtual void StateDeath()
    {
        fsm.ChangeState(State.Death);
        EnemyDeath();
    }

    // 모든 적이 공통적으로 갖는 동작 - 이동, 멈춤, 타격, 피격, 사망

    // 일반 이동
    public virtual void EnemyMove()
    {
        rigid.linearVelocityX = constantData.moveSpeed * variableData.sightDirection;
        if (variableData.frontCheck.collider != null || variableData.floorCheck.collider == null)
            ChangeDirection();
    }

    // 추적 이동(적 타입 B를 기준으로 설정)
    public virtual void EnemyChaseMove()
    {
        variableData.playerEnemyXDistance = GameManager.instance.player.transform.position.x - transform.position.x;
        variableData.moveDir = variableData.playerEnemyXDistance / Mathf.Abs(variableData.playerEnemyXDistance);
        variableData.playerEnemyXDistance = Mathf.Abs(variableData.playerEnemyXDistance);
        rigid.linearVelocityX = variableData.moveDir * constantData.moveSpeed;
        if (variableData.moveDir != variableData.sightDirection)
            ChangeDirection();
    }

    // 멈춤
    public void EnemyStop()
    {
        rigid.linearVelocityX = 0f;
        enemyAnimation.PlayIdle();
    }

    // 플레이어 타격 - 행동에 공격이 있는 경우 이를 사용
    // 타격에 종류는 실제로 공격 모션이 주어지는 경우와 플레이어와의 충돌 시 타격이 가해지는 경우가 있다.
    // 충돌이 공격인 타입 A, B에 대한 것을 정의 후 다른 공격이 있는 경우 이를 override해서 구현
    public virtual void EnemyAttack()
    {
        
    }

    public void EnemyCrush()
    {
        StartCoroutine(CrushRoutine());
    }

    IEnumerator CrushRoutine()
    {
        yield return new WaitForSeconds(0.3f);
        variableData.isAttack = false;
        variableData.isCrush = false;
    }

    // 피격
    public void EnemyHit()
    {
        variableData.curHP -= 1f;  // 지금 당장 플레이어의 공격 데미지가 없기 때문에 임시로 1f라는 값을 써서 피격 시 1f씩 피가 닳도록 설정
        StartCoroutine(HitRoutine());
    }

    // 피격 행동이 다른 적이 있을 수 있기에 가상 함수로 선언
    protected virtual IEnumerator HitRoutine()
    {
        // 피격 시 넉백 발생
        rigid.AddForce(variableData.knockbackDir * constantData.knockbackPower, ForceMode2D.Impulse);
        enemyAnimation.PlayIdle();  // 현재는 여기서 애니메이션을 재생하지만 추후에는 상태 변경 시 재생하는 것으로 변경 예정
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

    // 사망
    public void EnemyDeath()
    {
        gameObject.SetActive(false);
    }
}

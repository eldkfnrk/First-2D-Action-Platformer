using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.XR;

// [System.Flags] - 하나의 열거형 변수에 여러 개의 값을 동시에 지정(다중 선택)할 수 있게 해주는 기능
// 비트 연산을 기반으로 작동 => 예시) 1-A, 2-B, 4-C가 있다고 하면 A=0001, B=0010, C=0100 이라는 값을 갖고 열거형 변수가 A와 C의 값을 동시에 갖고 있게 하고자 하면 A와 C를 OR 연산을 통해 얻은 0101이라는 값을 저장하는 식으로 볼 수 있다.
[System.Flags]
public enum InputControl
{
    None = 0,
    Jump = 1<<0,
    Roll = 1<<1,
    Attack = 1<<2,
    Block = 1<<3,
    All = ~0,  // ~0는 모든 비트 값이 0이 아니라는 뜻으로 8비트 짜리 변수라 가정하면 11111111이라는 값을 가진다는 의미이다.
}

public class PlayerStateMachine : MonoBehaviour
{
    public enum State
    {
        Idle,
        Run,
        Jump,
        Fall,
        WallSlide,
        Roll,
        Attack,
        Block,
        Hit,
        Death,
    }
    
    ContactFilter2D hitFilter;
    Collider2D[] hitEnemies;  

    public State playerState;  // 변하는 데이터 값이지만 예외적으로 상태 데이터이기 때문에 직접 관리
    public GameObject blockRangeBox;

    Dictionary<State, BaseState> states;
    BaseState currentState;

    public BaseState CurrentState
    {
        get { return currentState; }
    }

    public PlayerData constantData;  // constant - 상수, 변하지 않는 데이터라는 의미로 붙인 변수명
    public PlayerRuntimeData variableData;  // variable - 변수, 변하는 데이터라는 의미로 붙인 변수명
    public PlayerAnimation playerAnimation;  // 애니메이션 관련 스크립트
    public Rigidbody2D rigid;
    public SpriteRenderer spriteR;
    public Collider2D coll;

    private void Awake()
    {
        hitEnemies = new Collider2D[15];
        hitFilter.useLayerMask = true;
        hitFilter.useTriggers = false;
        hitFilter.SetLayerMask(constantData.enemyLayer);

        variableData = GetComponent<PlayerRuntimeData>();
        playerAnimation = GetComponent<PlayerAnimation>();
        rigid = GetComponent<Rigidbody2D>();
        spriteR = GetComponent<SpriteRenderer>();
        coll = GetComponent<Collider2D>();

        states = new Dictionary<State, BaseState>();
        states.Add(State.Idle, new IdleState(this));
        states.Add(State.Run, new RunState(this));
        states.Add(State.Jump, new JumpState(this));
        states.Add(State.Fall, new FallState(this));
        states.Add(State.WallSlide, new WallSlideState(this));        
        states.Add(State.Roll, new RollState(this));        
        states.Add(State.Attack, new AttackState(this));        
        states.Add(State.Block, new BlockState(this));        
        states.Add(State.Hit, new HitState(this));        
        states.Add(State.Death, new DeathState(this));

        constantData.attackDuration = new WaitForSeconds(constantData.attackDurationTime);
        constantData.knockbackDuration = new WaitForSeconds(constantData.knockbackDurationTime);
        constantData.rollDuration = new WaitForSeconds(constantData.rollDurationTime);
        constantData.cantInputDuration = new WaitForSeconds(constantData.cantInputDurationTime);
        variableData.blockBoxPos.y = 0f;
    }

    private void Start()
    {
        playerState = State.Idle;
        currentState = states[playerState];
        currentState.Enter();
    }

    private void OnEnable()
    {
        variableData.curHP = constantData.maxHp;
    }

    private void Update()
    {
        EssenetialCheckLsit();
        switch (playerState)
        {
            case State.Death:
                if (variableData.isRevival)
                    ChangeState(State.Idle);
                break;
        }
        currentState.Update();
    }

    private void LateUpdate()
    {
        variableData.sightDirection = spriteR.flipX ? -1f : 1f;
    }

    private void FixedUpdate()
    {
        variableData.groundCheck = Physics2D.Raycast(transform.position, Vector2.down, constantData.groundCheckDistance, constantData.groundLayer);
        variableData.lowWallCheckOriginPos.x = transform.position.x;
        variableData.lowWallCheckOriginPos.y = transform.position.y + constantData.lowWallCheckOffset;
        variableData.highWallCheck = Physics2D.BoxCast(transform.position, constantData.wallCheckBoxSize, 0f, Vector2.right * variableData.sightDirection, constantData.wallCheckDistance, constantData.groundLayer);
        variableData.lowWallCheck = Physics2D.Raycast(variableData.lowWallCheckOriginPos, Vector2.right * variableData.sightDirection, constantData.wallCheckDistance, constantData.groundLayer);
    }

    void EssenetialCheckLsit()
    {
        if (variableData.isDead && playerState != State.Death)
        {
            Death();
            return;
        }

        if (variableData.isHit && playerState != State.Hit)
            Hit();
    }

    void ChangeState(State state)
    {
        if (playerState == state)
            return;

        currentState.Exit();
        currentState = states[state];
        currentState.Enter();
    }

    public void Idle()
    {
        if (rigid.gravityScale != 1f)
            rigid.gravityScale = 1f;
        variableData.isJump = false;
        variableData.isWall = false;
        ChangeState(State.Idle);
        playerAnimation.PlayIdle();
    }

    public void Run()
    {
        ChangeState(State.Run);
        playerAnimation.PlayRun();
    }

    public void Jump()
    {
        variableData.isJump = true;
        ChangeState(State.Jump);
        if (variableData.isWall)
        {
            // 벽 점프를 할 때 벽을 체크해버리면서 점프와 동시에 WallSlide로 변해버리는 문제가 있었고 이를 방지하기 위해 아예 점프 시에 몸을 반대로 돌렸다 생각하고 반대를 체크하도록 설정(이러면 점프할 때 바로 벽을 감지하지 않으면서 정상 작동한다.)
            ChangeSight();
            variableData.highWallCheck = Physics2D.BoxCast(transform.position, constantData.wallCheckBoxSize, 0f, Vector2.right * variableData.sightDirection, constantData.wallCheckDistance, constantData.groundLayer); ;
            variableData.lowWallCheck = Physics2D.Raycast(variableData.lowWallCheckOriginPos, Vector2.right * variableData.sightDirection, constantData.wallCheckDistance, constantData.groundLayer);
            variableData.wallJumpVec.x = variableData.sightDirection * constantData.hitWallPower;
            variableData.wallJumpVec.y = constantData.jumpPower;
            variableData.cantInput = true;
            variableData.isWall = false;
            rigid.linearVelocityY = 0f;  // 벽에서 미끄러지는 상태에서 아래 키를 눌러서 y축 속도를 건들이고 있을 경우를 대비하여 점프 직전 0으로 수정하여 점프 높이에 영향이 가지 않도록 하기 위한 설정
            rigid.AddForce(variableData.wallJumpVec, ForceMode2D.Impulse);
            CantInputChange();
        }
        else
        {
            rigid.AddForce(Vector2.up * constantData.jumpPower, ForceMode2D.Impulse);
        }
        playerAnimation.PlayJump();
    }

    public void Fall()
    {
        if (!variableData.isJump)
            variableData.isJump = true;
        if (variableData.isWall)
            variableData.isWall = false;

        rigid.gravityScale = constantData.fallSpeed;
        ChangeState(State.Fall);
        playerAnimation.PlayFall();
    }

    public void WallSlide()
    {
        variableData.isJump = false;
        variableData.isWall = true;
        rigid.gravityScale = 1f;
        ChangeState(State.WallSlide);
        playerAnimation.PlayWallSlide();
    }

    public void Roll()
    {
        variableData.rollKeyDown = false;
        ChangeState(State.Roll);
        DoRoll();
        playerAnimation.PlayRoll();
    }

    void DoRoll()
    {
        StartCoroutine(RollRoutine());
    }

    IEnumerator RollRoutine()
    {
        rigid.gravityScale = 0f;
        coll.enabled = false;
        variableData.isRoll = true;

        yield return constantData.rollDuration;

        rigid.gravityScale = 1f;
        coll.enabled = true;
        variableData.isRoll = false;
    }

    public void Attack(int atkCount)
    {
        ChangeState(State.Attack);
        DoAttack(atkCount);
        playerAnimation.PlayAttack(atkCount);
    }

    public void DoAttack(int atkCount)
    {
        variableData.atkRoutine = true;
        StartCoroutine(AttackRoutine(atkCount));
    }

    bool attackBox;

    IEnumerator AttackRoutine(int atkCount)
    {
        attackBox = true;
        yield return constantData.attackDuration;
        attackBox = false;

        variableData.atkRoutine = false;

        if (variableData.rollKeyDown || variableData.atkKeyDownCount == atkCount)
        {
            variableData.atkKeyDownCount = 0;
            variableData.atkCount = 0;
            variableData.isAttack = false;
        }
    }

    // 지금까지는 공격 판정을 코루틴 내에서 시간 단위로 끊어서 해야만 한다고 생각하였는데 AI와의 질의응답을 거치다 우연히 공격 판정을 애니메이션 이벤트로 처리하는 것이 좋다는 것을 알아내었다.
    public void AttackHitJudege()
    {
        variableData.attackBoxPos.x = transform.position.x + variableData.sightDirection;
        variableData.attackBoxPos.y = transform.position.y;
        int hitCount = Physics2D.OverlapBox(variableData.attackBoxPos, constantData.attackBoxSize, 0f, hitFilter, hitEnemies);

        // 게임 매니저에 전달 - 게임 매니저가 전투 판정을 관할
        if (hitCount != 0)
            GameManager.instance.AttackEnemies(hitEnemies, variableData.attackBoxPos);  // 일반 공격이기 때문에 플레이어가 바라보는 방향으로 공격을 했을 것이기에 이와 같은 값을 공격 방향으로 전달
    }

    public void Block()
    {
        if (playerState == State.Block)
            return;

        variableData.isBlock = true;
        blockRangeBox.SetActive(true);
        BlockBoxChangeLoc(variableData.sightDirection);
        ChangeState(State.Block);
        // 방어 범위 및 판정을 콜라이더(콜라이더를 포함한 오브젝트)를 활성화
        playerAnimation.PlayBlock();
    }

    // 방어 범위를 정하는 박스의 위치를 조정하는 함수
    public void BlockBoxChangeLoc(float sightDirection)
    {
        variableData.blockBoxPos.x = constantData.blockBoxXPos * sightDirection;
        blockRangeBox.transform.localPosition = variableData.blockBoxPos;  // localPosition으로 부모 객체 기반 위치를 사용해야만 정상적인 작동이 가능하다.
    }

    public void SuccessBlock()
    {
        // 방어 범위임을 나타내는 오브젝트의 콜라이더에 충돌하는데 hit 판정이 되는 버그가 존재 수정 필요
        StartCoroutine(SuccessBlockRoutine());
    }

    IEnumerator SuccessBlockRoutine()
    {
        variableData.blockKnockbackDir.x = constantData.blockKnockbackPower * variableData.sightDirection * -1f;  // 블락 넉백은 방어하는 방향의 반대 방향으로 밀리는 기능이기 때문에 -1f를 수행하여 바라보는 방향의 반대 방향으로 보내는 것이다.
        rigid.AddForce(variableData.blockKnockbackDir, ForceMode2D.Impulse);
        playerAnimation.PlaySuccessBlock();

        // 한 프레임 쉬고 IdleBlock 파라미터를 false로 바꾸는 이유는 IdleBlock 파라미터가 true여야 기본 방어 애니메이션이 재생되고 이 상태에서 Block 트리거를 활성화시켜야 방어 성공 애니메이션이 재생되기 때문이다.
        // 근데 왜 IdleBlock 파라미터를 false로 바꾸냐면 기본 방어 애니메이션은 Any State에서 즉, 어떠한 상태에서도 파라미터 값이 만족한다면 재생되기 때문에 방어 성공 애니메이션 재생을 위한 트리거를 활성화시켜서 상태를 넘기고 나서
        // IdleBlock 파라미터 값을 false로 해야만 정상적으로 방어 성공 애니메이션이 재생되기 때문이다.
        yield return null;  

        playerAnimation.StopBlock();

        yield return new WaitForSeconds(0.4f);

        variableData.successBlock = false;
        playerAnimation.PlayBlock();
    }

    public void Hit()
    {
        ChangeState(State.Hit);
        rigid.gravityScale = 1f;
        KnockBack();
        playerAnimation.PlayHit();
    }

    public void KnockBack()
    {
        StartCoroutine(KnockBackRoutine());
    }

    IEnumerator KnockBackRoutine()
    {
        // 넉백
        rigid.AddForce(variableData.knockbackDir, ForceMode2D.Impulse);
        yield return constantData.knockbackDuration;
        variableData.isHit = false;
        if (variableData.curHP <= 0f)
        {
            variableData.isDead = true;
            yield break;
        }

        if (variableData.obstacleHit)
        {
            variableData.obstacleHit = false;
            GameManager.instance.RespawnPlayer(variableData.RespawnPoint);
        }
    }

    public void Death()
    {
        ChangeState(State.Death);
        coll.enabled = false;
        rigid.gravityScale = 0f;
        rigid.linearVelocity = Vector2.zero;
        playerAnimation.PlayDeath();
    }

    public void CantInputChange()
    {
        StartCoroutine(CantInputChangeRoutine());
    }

    IEnumerator CantInputChangeRoutine()
    {
        yield return constantData.cantInputDuration;
        variableData.cantInput = false;
    }

    public void ChangeSight()
    {
        spriteR.flipX = !spriteR.flipX;
        variableData.sightDirection = spriteR.flipX ? -1f : 1f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            variableData.isHit = true;
            variableData.knockbackDir.x = (transform.position.x - collision.transform.position.x) * constantData.knockbackXPower;
            variableData.knockbackDir.y = constantData.knockbackYPower;
            variableData.curHP -= 1f;  // 아직 적의 데미지라는 수치가 없기 때문에 임시로 1f라는 값을 사용
        }

        if (collision.gameObject.CompareTag("Obstacle"))
        {
            variableData.isHit = true;
            variableData.knockbackDir.x = (transform.position.x - collision.transform.position.x) * constantData.knockbackXPower;
            variableData.knockbackDir.y = (transform.position.y - collision.transform.position.y) * constantData.knockbackYPower;
            variableData.obstacleHit = true;
            variableData.curHP -= 1f;  // 아직 장애물의 데미지라는 수치가 없기 때문에 임시로 1f라는 값을 사용(장애물의 데미지를 어떻게 저장하고 어떻게 꺼내 쓸 것인가에 대한 고민도 필요하다.)
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        GameObject collisionObject = collision.gameObject;
        switch (collisionObject.tag)
        {
            case "Door":
                variableData.canInteractive = true;
                variableData.doorSpawnPoint = collision.gameObject.GetComponent<SpawnPoint>();
                break;
            case "Enemy":
                variableData.successBlock = true;
                SuccessBlock();
                break;
            case "CheckPoint":
                variableData.RespawnPoint = collisionObject.GetComponent<CheckPoint>().RespawnPoint;
                break;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Door"))
        {
            variableData.canInteractive = false;
            variableData.doorSpawnPoint = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;  // 계속하여 에러가 발생해서 플레이 중이 아닐 땐 꺼놓도록 설정(이건 추후에 수정해서 씬에서 볼 수 있도록 변경 예정)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector2.down * constantData.groundCheckDistance);
        Gizmos.color = Color.darkRed;
        Vector2 highWallBoxPos;
        highWallBoxPos.x = transform.position.x + constantData.wallCheckDistance * variableData.sightDirection;
        highWallBoxPos.y = transform.position.y;
        Gizmos.DrawWireCube(highWallBoxPos, constantData.wallCheckBoxSize);
        Gizmos.DrawRay(variableData.lowWallCheckOriginPos, Vector2.right * variableData.sightDirection * constantData.wallCheckDistance);
        Gizmos.color = Color.black;
        if (attackBox)
            Gizmos.DrawWireCube(variableData.attackBoxPos, constantData.attackBoxSize);
    }
}
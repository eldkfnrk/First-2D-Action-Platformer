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
        variableData.knockbackDir.y = constantData.knockbackYPower;
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
            variableData.isDead = true;
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
            variableData.curHP -= 1f;  // 아직 적의 데미지라는 수치가 없기 때문에 임시로 1f라는 값을 사용
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            variableData.successBlock = true;
            SuccessBlock();
        }

        if (collision.gameObject.CompareTag("Door"))
        {
            variableData.canInteractive = true;
            variableData.doorSpawnPoint = collision.gameObject.GetComponent<SpawnPoint>();
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

public abstract class BaseState
{
    // 아래 두 문장에서 protected를 사용한 이유는 이 변수와 함수는 이 클래스와 상속 받는 클래스에서만 사용되어야 하기 때문이다.
    protected PlayerStateMachine fsmController;
    // 기본 값은 아예 인풋이 불가능한 상태로 둔다. 상태에서 모든 입력을 막아야 할 때 굳이 override 하지 않고 그대로 사용하면 된다.
    public virtual InputControl inputControl => InputControl.None;  // 자식들은 다른 값을 가질 수 있도록 해야 하는데 그냥 대입을 하면 변수가 되고 C#에서 변수는 virtual을 사용할 수 없기에 람다식으로 값을 대입하여 virtual이 가능하도록 한다.(람다는 함수 취급이기에 가능)

    protected BaseState(PlayerStateMachine controller)
    {
        fsmController = controller;
    }

    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();

    protected void Move()
    {
        if (fsmController.variableData.cantInput)
            return;

        if (fsmController.variableData.lowWallCheck.collider != null)
            fsmController.rigid.linearVelocityX = 0f;
        else
            fsmController.rigid.linearVelocityX = fsmController.variableData.moveDirection * fsmController.constantData.moveSpeed;

        if (fsmController.variableData.moveDirection != 0f && fsmController.variableData.sightDirection != fsmController.variableData.moveDirection)  // 바라보는 방향과 이동 방향이 반대인 경우
            fsmController.ChangeSight();
    }
}

public class IdleState : BaseState
{
    // 여기서 base는 부모 클래스의 생성자를 의미한다.(C# 문법)
    // public으로 하지 않으면 이 생성자를 호출할 수 없기 때문에 특정 경우를 제외하고는 외부에서 초기화가 가능하도록 public 선언을 해야 한다.
    public IdleState(PlayerStateMachine controller) : base(controller) { }
    public override InputControl inputControl => InputControl.All;

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Idle;
        fsmController.rigid.gravityScale = 1f;
        fsmController.playerAnimation.PlayIdle();
    }

    public override void Update()
    {
        if(fsmController.variableData.groundCheck.collider == null)
        {
            fsmController.Fall();
            return;
        }

        if(fsmController.variableData.moveDirection != 0f)
        {
            fsmController.Run();
            return;
        }
    }

    public override void Exit()
    {

    }
}

public class RunState : BaseState
{
    public RunState(PlayerStateMachine controller) : base(controller) { }
    public override InputControl inputControl => InputControl.All;

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Run;
    }

    public override void Update()
    {
        if (fsmController.variableData.groundCheck.collider == null)
        {
            fsmController.Fall();
            return;
        }

        if(fsmController.variableData.moveDirection == 0f)
        {
            fsmController.Idle();
            return;
        }

        Move();
    }

    public override void Exit()
    {
        fsmController.rigid.linearVelocityX = 0f;
    }
}

public class JumpState : BaseState
{
    public JumpState(PlayerStateMachine controller) : base(controller) { }

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Jump;
    }

    public override void Update()
    {
        Move();  // 점프 중에도 좌우 이동은 가능하도록 설정
        if (fsmController.variableData.groundCheck.collider != null)
        {
            // rigidbody의 AddForce는 인자로 전달된 값만큼 x축의 속도와 y축의 속도를 수정한다.(ForceMode가 Impulse면 한 번에 그 속도에 도달하고 Force이면 서서히 도달한다.)
            // 즉, rigid.AddForce를 사용해서 점프를 구현하면 점프 키를 입력함과 동시에 설정한 점프 파워 값이 y축 속도가 된다.(gravityScale 값이 1인 경우)
            // 이후 중력 값에 의해 서서히 y축 속도가 줄어드는 원리이다.
            // 그러니 점프 직후 바로 Ray에 걸려 착지 판정이 되는 문제를 y축에 속도를 이용하여 점프 직후인지를 판단하도록 해서 해결할 수 있을 것이다.
            // 현재 점프 파워는 8. y축 속도가 점프 파워 값의 -2한 값 이하이고 시작할 때부터 착지 판정이 되도록 설정하면 점프 직후에 바로 착지가 되는 문제와 Fall 상태가 되지 않아도 착지 판정을 할 수 있는 문제를 동시에 해결할 수 있다.
            // 점프 애니메이션은 Jump에서 Fall로 넘어가야만 Idle로 갈 수 있는데 점프 상태에서 Idle 상태로 이동하면 애니메이션이 Fall로 넘어가지 않아서 애니메이션 전환이 되지 않는 문제가 발생
            // 해당 문제를 해결하기 위한 방법으로 점프 상태에서 Idle 상태가 되어야 할 때 한 번 Fall 상태로 넘겨서 Fall이 바로 Idle 상태로 넘기도록 하는 방법이 있다.
            // y축 속도가 0이상의 값이고 점프 파워보다 -2한 값보다 미만이면 Fall 상태로 넘어가도록 설정(상승 중인데 땅에 착지한 것을 판단하는 범위)
            if (fsmController.rigid.linearVelocityY < 6f && fsmController.rigid.linearVelocityY >= 0f)
                fsmController.Fall();  // Idle 상태로 보내기 전 애니메이션 전환을 위한 Fall 상태로 보내기
            return;
        }

        if(fsmController.variableData.highWallCheck.collider != null)
        {
            fsmController.WallSlide();
            return;
        }

        if (fsmController.rigid.linearVelocityY < 0f)
            fsmController.Fall();
    }

    public override void Exit()
    {
        if (fsmController.variableData.moveDirection == 0f)
            fsmController.rigid.linearVelocityX = 0f;
    }
}

public class FallState : BaseState
{
    public FallState(PlayerStateMachine controller) : base(controller) { }

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Fall;
    }

    public override void Update()
    {
        Move();  // 떨어지는 중에도 좌우 이동은 가능하도록 설정
        if(fsmController.variableData.groundCheck.collider != null)
        {
            fsmController.Idle();
            return;
        }

        if(fsmController.variableData.highWallCheck.collider != null)
        {
            fsmController.WallSlide();
            return;
        }
    }

    public override void Exit()
    {
        if (fsmController.variableData.moveDirection == 0f)
            fsmController.rigid.linearVelocityX = 0f;
    }
}

public class WallSlideState : BaseState
{
    public WallSlideState(PlayerStateMachine controller) : base(controller) { }
    public override InputControl inputControl => InputControl.Jump;

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.WallSlide;
        fsmController.rigid.linearVelocityX = 0f;
    }

    public override void Update()
    {
        // 벽을 검사하는 Ray가 중앙에 있어서 다리 부근이 벽과 충돌하면 그냥 멈추는 문제가 존재
        // 벽을 검사하는 Ray를 2개를 둬서 2개 다 벽을 감지해야 벽과 충돌한 상황이라고 보거나 overlapbox를 이용하여 벽을 판단하거나 해야 할 것으로 추측 중이다.

        if (fsmController.variableData.groundCheck.collider != null || fsmController.variableData.highWallCheck.collider == null)
        {
            fsmController.ChangeSight();
            fsmController.Idle();
            return;
        }

        if (fsmController.variableData.downKeyPressed)
        {
            fsmController.rigid.linearVelocityY = -2f;
            fsmController.playerAnimation.SlideDustSpeedUp();
        }
        else
        {
            fsmController.rigid.linearVelocityY = 0f;
            fsmController.playerAnimation.SlideDustSpeedDown();
        }
    }

    public override void Exit()
    {
        fsmController.playerAnimation.StopSlideDust();
    }
}

public class RollState : BaseState
{
    public RollState(PlayerStateMachine controller) : base(controller) { }

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Roll;
    }

    public override void Update()
    {
        if (!fsmController.variableData.isRoll)
        {
            if (fsmController.variableData.groundCheck.collider == null)
                fsmController.Fall();
            else
                fsmController.Idle();

            return;
        }

        fsmController.rigid.linearVelocityY = 0f;
        if (fsmController.variableData.lowWallCheck.collider != null)
            fsmController.rigid.linearVelocityX = 0f;
        else
            fsmController.rigid.linearVelocityX = fsmController.constantData.rollSpeed * fsmController.variableData.sightDirection;
    }

    public override void Exit()
    {
        fsmController.rigid.linearVelocityX = 0f;
    }
}

public class AttackState : BaseState
{
    public AttackState(PlayerStateMachine controller) : base(controller) { }
    public override InputControl inputControl => InputControl.Attack | InputControl.Roll;

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Attack;
        fsmController.variableData.isAttack = true;
    }

    public override void Update()
    {
        if (fsmController.variableData.atkRoutine)
            return;

        if(!fsmController.variableData.isAttack && fsmController.variableData.rollKeyDown)
        {
            fsmController.Roll();
            return;
        }

        if(fsmController.variableData.atkKeyDownCount > fsmController.variableData.atkCount)
        {
            ++fsmController.variableData.atkCount;
            fsmController.Attack(fsmController.variableData.atkCount);
        }

        if (!fsmController.variableData.isAttack)
        {
            fsmController.Idle();
        }
    }

    public override void Exit()
    {
        
    }
}

public class BlockState : BaseState
{
    public BlockState(PlayerStateMachine controller) : base(controller) { }
    public override InputControl inputControl => InputControl.Jump | InputControl.Roll | InputControl.Block;

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Block;
    }

    public override void Update()
    {
        if (fsmController.variableData.successBlock)
            return;

        if (fsmController.variableData.moveDirection != 0f && fsmController.variableData.moveDirection != fsmController.variableData.sightDirection)
        {
            fsmController.ChangeSight();
            fsmController.BlockBoxChangeLoc(fsmController.variableData.sightDirection);
        }

        if (!fsmController.variableData.isBlock)
            fsmController.Idle();
    }

    public override void Exit()
    {
        fsmController.variableData.isBlock = false;  // 키를 뗄 때도 방어가 해제되지만 키를 누르고 있음에도 점프, 공격 등의 키 입력이 있으면 상태 전환이 되기 때문에 반드시 방어 상태가 아님을 알리기 위해 바꿔준다.
        fsmController.blockRangeBox.SetActive(false);
        fsmController.playerAnimation.StopBlock();
    }
}

public class HitState : BaseState
{
    public HitState(PlayerStateMachine controller) : base(controller) { }

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Hit;
    }

    public override void Update()
    {
        if (!fsmController.variableData.isHit)
        {
            if (fsmController.variableData.groundCheck.collider != null)
                fsmController.Idle();
            else
                fsmController.Fall();
        }
    }

    public override void Exit()
    {

    }
}

public class DeathState : BaseState
{
    public DeathState(PlayerStateMachine controller) : base(controller) { }

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Death;
        GameManager.instance.NotifyPlayerDeath();
    }

    public override void Update()
    {
        if (fsmController.variableData.isRevival)
        {
            if (fsmController.variableData.groundCheck.collider == null)
                fsmController.Idle();
            else
                fsmController.Fall();
        }
    }

    public override void Exit()
    {
        fsmController.playerAnimation.ParameterReset();
        fsmController.variableData.isDead = false;
        fsmController.variableData.isRevival = false;
        fsmController.coll.enabled = true;
        fsmController.variableData.curHP = fsmController.constantData.maxHp;
        fsmController.variableData.cantInput = true;
        fsmController.CantInputChange();
    }
}
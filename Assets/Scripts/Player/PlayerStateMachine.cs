using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Playables;

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
            case State.Roll:
                if (!variableData.isRoll)
                {
                    if (variableData.groundCheck.collider == null)
                        ChangeState(State.Fall);
                    else
                        ChangeState(State.Idle);
                }
                break;
            case State.Attack:
                // 공격이 종료되면 구르도록 설정
                if (variableData.rollKeyDown && !variableData.atkRoutine)
                {
                    ChangeState(State.Roll);
                    break;
                }

                if (!variableData.isAttack)
                    ChangeState(State.Idle);
                break;
            case State.Hit:
                if (!variableData.isHit)
                    ChangeState(State.Fall);
                break;
            case State.Death:
                if (variableData.isRevival)
                    ChangeState(State.Idle);
                break;
        }
        Debug.Log(playerState);
        currentState.Update();
    }

    private void LateUpdate()
    {
        variableData.sightDirection = spriteR.flipX ? -1f : 1f;
    }

    private void FixedUpdate()
    {
        variableData.groundCheck = Physics2D.Raycast(transform.position, Vector2.down, constantData.groundCheckDistance, constantData.groundLayer);
        variableData.wallCheck = Physics2D.Raycast(transform.position, Vector2.right * variableData.sightDirection, constantData.wallCheckDistance, constantData.groundLayer);
    }

    void EssenetialCheckLsit()
    {
        if (variableData.isDead && playerState != State.Death)
        {
            ChangeState(State.Death);
            return;
        }

        if (variableData.isHit && playerState != State.Hit)
            ChangeState(State.Hit);
    }

    void ChangeState(State state)
    {
        currentState.Exit();
        currentState = states[state];
        currentState.Enter();
    }
    public void Idle()
    {
        if (rigid.gravityScale != 1f)
            rigid.gravityScale = 1f;
        variableData.isJump = false;
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
        if (variableData.isWall)
        {
            // 벽 점프를 할 때 벽을 체크해버리면서 점프와 동시에 WallSlide로 변해버리는 문제가 있었고 이를 방지하기 위해 아예 점프 시에 몸을 반대로 돌렸다 생각하고 반대를 체크하도록 설정(이러면 점프할 때 바로 벽을 감지하지 않으면서 정상 작동한다.)
            spriteR.flipX = !spriteR.flipX;
            variableData.sightDirection = spriteR.flipX ? -1f : 1f;
            variableData.wallCheck = Physics2D.Raycast(transform.position, Vector2.right * variableData.sightDirection, constantData.wallCheckDistance, constantData.groundLayer);
            variableData.wallJumpVec.x = variableData.sightDirection * constantData.hitWallPower;
            variableData.wallJumpVec.y = constantData.jumpPower;
            variableData.cantInput = true;
            variableData.isWall = false;
            rigid.AddForce(variableData.wallJumpVec, ForceMode2D.Impulse);
            CantInputChange();
        }
        else
        {
            rigid.AddForce(Vector2.up * constantData.jumpPower, ForceMode2D.Impulse);
        }
        ChangeState(State.Jump);
        playerAnimation.PlayJump();
    }

    public void Fall()
    {
        if (!variableData.isJump)
            variableData.isJump = true;

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
        DoRoll();
        ChangeState(State.Roll);
        playerAnimation.PlayRoll();
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
            GameManager.instance.AttackEnemies(hitEnemies, Vector2.right * variableData.sightDirection);  // 일반 공격이기 때문에 플레이어가 바라보는 방향으로 공격을 했을 것이기에 이와 같은 값을 공격 방향으로 전달
    }

    public void KnockBack()
    {
        StartCoroutine(KnockBackRoutine());
    }

    IEnumerator KnockBackRoutine()
    {
        // 넉백
        // 넉백 버그가 발생했던 이유
        // 충돌 시 isHit라는 값을 true로 변환시키고 어떤 상태이든 상관 없이 바로 Hit 상태로 전환시킨다.
        // 이때 단 한 번만 넉백을 시행하고 피격이 끝났음을 알리기 위해 isHit를 false로 수정하는 것이였는데 내가 한 구현에서는 isHit가 true면 계속하여 피격 상태로의 전환을 시도하였기에 넉백 코루틴이 계속하여 호출되었을 것이다.
        // 원래대로라면 플레이어의 상태가 피격 상태라면 더 이상 피격 상태로의 전환이 이뤄져서는 안 되었으나 그걸 방지하지 못하면서 코루틴이 중첩되었고 피격 상태가 끝난 후에도 남아서 y축으로 치솟는 동작을 하게 만들었던 것이다.
        rigid.AddForce(variableData.knockbackDir, ForceMode2D.Impulse);
        yield return constantData.knockbackDuration;
        variableData.isHit = false;
        if (variableData.curHP <= 0f)
            variableData.isDead = true;
    }

    public void Block()
    {
        // 스테미너는 특정 행동이 가능한 수치를 의미
        // 스테미너를 소모하는 행동은 방어와 구르기
        // 방어는 성공 시 스테미너를 소모
        // 구르기는 즉시 스테미너를 소모
        // 두 행동 모두 사용할 수 있는 소모량보다 적은 스테미너 상태라면 사용 불가

        // 다음 날 할 거
        // 비트 마스크를 적용하여 인풋 조건 허용 만들기
        // 상태 전환 각 State가 관장하도록 설정하기
        // 위의 변화에 맞도록 각 상태 수정하기
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

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;  // 계속하여 에러가 발생해서 플레이 중이 아닐 땐 꺼놓도록 설정(이건 추후에 수정해서 씬에서 볼 수 있도록 변경 예정)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector2.down * constantData.groundCheckDistance);
        Gizmos.color = Color.darkRed;
        Gizmos.DrawRay(transform.position, Vector2.right * constantData.wallCheckDistance * variableData.sightDirection);
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

        fsmController.rigid.linearVelocityX = fsmController.variableData.moveDirection * fsmController.constantData.moveSpeed;
        if (fsmController.variableData.sightDirection == -fsmController.variableData.moveDirection)  // 바라보는 방향과 이동 방향이 반대인 경우
            fsmController.spriteR.flipX = !fsmController.spriteR.flipX;
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

        Move();

        if(fsmController.variableData.moveDirection == 0f)
        {
            fsmController.Idle();
            return;
        }
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

        if(fsmController.variableData.groundCheck.collider != null)
        {
            if (fsmController.rigid.linearVelocityY <= 0f)
                fsmController.Idle();
            return;
        }

        if(fsmController.variableData.wallCheck.collider != null)
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
        fsmController.variableData.isJump = false;
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

        if(fsmController.variableData.wallCheck.collider != null)
        {
            fsmController.WallSlide();
            return;
        }
    }

    public override void Exit()
    {
        if (fsmController.variableData.moveDirection == 0f)
            fsmController.rigid.linearVelocityX = 0f;
        fsmController.variableData.isJump = false;
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
        if (fsmController.variableData.downKeyPressed)
            fsmController.rigid.linearVelocityY = -2f;
        else
            fsmController.rigid.linearVelocityY = 0f;
    }

    public override void Exit()
    {
        
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
        fsmController.rigid.linearVelocityY = 0f;
        if (fsmController.variableData.wallCheck.collider != null)
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
        ++fsmController.variableData.atkCount;
        fsmController.Attack(fsmController.variableData.atkCount);
        fsmController.playerAnimation.PlayAttack(fsmController.variableData.atkCount);
    }

    public override void Update()
    {
        if (fsmController.variableData.atkRoutine)
            return;

        if (fsmController.variableData.atkKeyDownCount <= 3 && fsmController.variableData.atkKeyDownCount > 1)
        {
            ++fsmController.variableData.atkCount;
            fsmController.Attack(fsmController.variableData.atkCount);
            fsmController.playerAnimation.PlayAttack(fsmController.variableData.atkCount);
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

    }

    public override void Exit()
    {

    }
}

public class HitState : BaseState
{
    public HitState(PlayerStateMachine controller) : base(controller) { }

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Hit;
        fsmController.rigid.gravityScale = 1f;
        fsmController.KnockBack();
        fsmController.playerAnimation.PlayHit();
    }

    public override void Update()
    {
        
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
        fsmController.playerAnimation.PlayDeath();
        fsmController.coll.enabled = false;
        fsmController.rigid.gravityScale = 0f;
        fsmController.rigid.linearVelocity = Vector2.zero;
    }

    public override void Update()
    {
        
    }

    public override void Exit()
    {
        fsmController.playerAnimation.ParameterReset();
        fsmController.variableData.isDead = false;
        fsmController.variableData.isRevival = false;
        fsmController.coll.enabled = true;
        fsmController.variableData.curHP = fsmController.constantData.maxHp;
    }
}
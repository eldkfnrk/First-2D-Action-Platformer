using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Playables;

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
        Hit,
        Death,
    }
    // OverlapBox는 매번 배열 객체를 힙 메모리에 생성하기 때문에 GC 할당이 발생 -> 이는 성능 감소로 이어지게 된다.
    // GC로 인한 성능 감소를 막기 위해 미리 배열을 만들고 여기에 OverlapBox 배열 객체를 전달하는 방식을 사용해야 한다.
    // 과거에는 이를 OverlapBoxNonAlloc이라는 함수가 대신하였으나 유니티 6에 들어오고 해당 함수를 더 이상 사용하지 않게 되었다.
    // 그렇지만 이와 동일한 성능을 내도록 하는 방법이 ContactFilter를 이용하여 OverlapBoxNonAlloc 함수의 기능을 구현하는 것이다.
    // ContactFilter를 통해 검사할 레이어, 트리거 여부 등을 지정하고 이를 매개 변수로 전달하여 동작하게 한다.
    // 원래는 변하는 데이터를 저장하는 곳에 있어야 하지만 이것은 다른 곳에서는 사용하지 않을 것이기 때문에 따로 저장
    ContactFilter2D hitFilter;
    Collider2D[] hitEnemies;  

    public State playerState;  // 변하는 데이터 값이지만 예외적으로 상태 데이터이기 때문에 직접 관리

    Dictionary<State, BaseState> states;
    BaseState currentState;

    public PlayerData constantData;  // constant - 상수, 변하지 않는 데이터라는 의미로 붙인 변수명
    public PlayerRuntimeData variableData;  // variable - 변수, 변하는 데이터라는 의미로 붙인 변수명
    public PlayerAnimation PlayerAnimation;  // 애니메이션 관련 스크립트
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
        PlayerAnimation = GetComponent<PlayerAnimation>();
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
    }

    private void Start()
    {
        playerState = State.Idle;
        currentState = states[playerState];
        currentState.Enter();
    }

    private void Update()
    {
        switch (playerState)
        {
            case State.Idle:
                if (variableData.atkKeyDownCount > 0)
                {
                    ChangeState(State.Attack);
                    break;
                }

                if (variableData.rollKeyDown)
                {
                    ChangeState(State.Roll);
                    break;
                }

                if (CheckJumpOrFall())
                    break;

                if (variableData.moveDirection != 0f)
                    ChangeState(State.Run);
                break;
            case State.Run:
                if (variableData.atkKeyDownCount > 0)
                {
                    ChangeState(State.Attack);
                    break;
                }

                if (variableData.rollKeyDown)
                {
                    ChangeState(State.Roll);
                    break;
                }

                if (CheckJumpOrFall())
                    break;

                if (variableData.moveDirection == 0f)
                    ChangeState(State.Idle);
                break;
            case State.Jump:
                if (WallCheck())
                    break;

                if (variableData.groundCheck.collider != null)
                {
                    if (rigid.linearVelocityY <= 0f)
                        ChangeState(State.Idle);
                    break;
                }

                if (rigid.linearVelocityY < 0f)
                    ChangeState(State.Fall);
                break;
            case State.Fall:
                if (WallCheck())
                    break;

                if (variableData.groundCheck.collider != null)
                    ChangeState(State.Idle);
                break;
            case State.WallSlide:
                if (variableData.jumpKeyDown)
                {
                    ChangeState(State.Jump);
                    break;
                }

                if (variableData.groundCheck.collider != null)
                    ChangeState(State.Idle);                
                break;
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
        variableData.wallCheck = Physics2D.Raycast(transform.position, Vector2.right * variableData.sightDirection, constantData.wallCheckDistance, constantData.groundLayer);
    }

    // 플레이어 공격과 해당 공격에 피격당한 적
    // 플레이어 공격 시 BoxCastAll을 통해 BoxCast 범위에 들어온 모든 적들은 피격 판정을 전달
    // 피격 판정을 실행할 방법은 2가지이다.
    // 1번 - BoxCastAll을 통해 획득한 적들의 피격 함수를 직접 호출하여 피격 진행
    // 2번 - BoxCastAll을 통해 획득한 적들의 명단을 습득하여 게임 매니저에게 전달하고 게임 매니저가 이 명단에 있는 적들에게 통보
    // 1번 방법은 너무 엮이는 것이 아닌가 하는 우려가 있고 2번 방법은 게임 매니저가 각 적에게 어떻게 통보해야 하는가를 고려해야 하는 정보의 부재가 있다.
    // 우선 2번 방법을 중심으로 진행하되 진행이 더디다면 1번 혹은 AI에게 질의하는 방식으로 진행할 예정

    void ChangeState(State state)
    {
        currentState.Exit();
        currentState = states[state];
        currentState.Enter();
    }

    bool CheckJumpOrFall()
    {
        // 점프 입력이 있었으면 점프 상태로 전환
        // 착지 상태가 아니면 떨어지는 상태로 전환
        if (variableData.jumpKeyDown)
        {
            if (variableData.wallCheck.collider != null)
            {
                variableData.jumpKeyDown = false;
                return false;
            }
            else
            {
                ChangeState(State.Jump);
                return true;
            }
        }
        else if (variableData.groundCheck.collider == null)
        {
            ChangeState(State.Fall);
            return true;
        }

        return false;
    }

    bool WallCheck()
    {
        if (variableData.cantInput)
            return false;

        // 앞에 벽이 있는지 확인(이때 착지 상태가 아니어야 벽에 있다고 판단) - 벽에 붙어서 미끄러지는 상태 전환을 위한 함수
        if (variableData.isJump && variableData.wallCheck.collider != null)
        {
            ChangeState(State.WallSlide);
            return true;
        }

        return false;
    }

    public void CantInputChange()
    {
        StartCoroutine(CantInputChangeRoutine());
    }

    IEnumerator CantInputChangeRoutine()
    {
        yield return new WaitForSeconds(0.3f);
        variableData.cantInput = false;
    }

    public void ChangeCanRoll()
    {
        StartCoroutine(RollRoutine());
    }

    IEnumerator RollRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        variableData.isRoll = false;
        coll.enabled = true;
        rigid.gravityScale = 1f;
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
        yield return new WaitForSeconds(0.45f);
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

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Idle;
        fsmController.rigid.gravityScale = 1f;
        fsmController.PlayerAnimation.PlayIdle();
    }

    public override void Update()
    {

    }

    public override void Exit()
    {

    }
}

public class RunState : BaseState
{
    public RunState(PlayerStateMachine controller) : base(controller) { }

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Run;
        fsmController.PlayerAnimation.PlayRun();
    }

    public override void Update()
    {
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
        fsmController.variableData.isJump = true;
        if (fsmController.variableData.isWall)
        {
            fsmController.variableData.wallJumpVec.x = -fsmController.variableData.sightDirection * fsmController.constantData.hitWallPower;
            fsmController.variableData.wallJumpVec.y = fsmController.constantData.jumpPower;
            fsmController.variableData.cantInput = true;
            fsmController.variableData.isWall = false;
            fsmController.rigid.AddForce(fsmController.variableData.wallJumpVec, ForceMode2D.Impulse);
            fsmController.spriteR.flipX = !fsmController.spriteR.flipX;
            fsmController.CantInputChange();
        }
        else
        {
            fsmController.rigid.AddForce(Vector2.up * fsmController.constantData.jumpPower, ForceMode2D.Impulse);
        }

        fsmController.variableData.jumpKeyDown = false;
        fsmController.PlayerAnimation.PlayJump();
    }

    public override void Update()
    {
        Move();  // 점프 중에도 좌우 이동은 가능하도록 설정
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
        fsmController.rigid.gravityScale = fsmController.constantData.fallSpeed;
        fsmController.variableData.isJump = true;
        fsmController.PlayerAnimation.PlayFall();
    }

    public override void Update()
    {
        Move();  // 떨어지는 중에도 좌우 이동은 가능하도록 설정
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

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.WallSlide;
        fsmController.rigid.gravityScale = 1f;
        fsmController.variableData.isWall = true;
        fsmController.variableData.isJump = false;
        fsmController.PlayerAnimation.PlayWallSlide();
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
        fsmController.variableData.rollKeyDown = false;
        fsmController.rigid.gravityScale = 0f;
        fsmController.PlayerAnimation.PlayRoll();
        fsmController.variableData.isRoll = true;
        fsmController.coll.enabled = false;
        fsmController.ChangeCanRoll();
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

    public override void Enter()
    {
        fsmController.playerState = PlayerStateMachine.State.Attack;
        ++fsmController.variableData.atkCount;
        fsmController.Attack(fsmController.variableData.atkCount);
        fsmController.PlayerAnimation.PlayAttack(fsmController.variableData.atkCount);
    }

    public override void Update()
    {
        if (fsmController.variableData.atkRoutine)
            return;

        if (fsmController.variableData.atkKeyDownCount <= 3 && fsmController.variableData.atkKeyDownCount > 1)
        {
            ++fsmController.variableData.atkCount;
            fsmController.Attack(fsmController.variableData.atkCount);
            fsmController.PlayerAnimation.PlayAttack(fsmController.variableData.atkCount);
        }
    }

    public override void Exit()
    {
        
    }
}
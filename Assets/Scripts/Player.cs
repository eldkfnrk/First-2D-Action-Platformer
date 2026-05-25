using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    bool cantAnyKeyInput;  // 모든 키 입력 및 행동 막는 변수

    float moveSpeed;
    public float defaultMoveSpeed;
    public float blockMoveSpeed;
    public float jumpPower;
    float direction;

    public LayerMask groundLayer;
    public LayerMask enemyLayer;
    
    RaycastHit2D groundCheck;
    public float fallSpeed;
    public bool isGround;


    bool isAttack;
    int attackCount;  // 공격 횟수를 저장할 변수

    float sightDirection;  // 캐릭터가 바라보고 있는 방향을 저장하는 변수(화면 기준 -1이면 왼쪽을 1이면 오른쪽을 바라보고 있다고 판정)

    RaycastHit2D wallCheck;
    public bool isWall;
    bool isWallJump;
    float wallBoxDirection;

    public float rollSpeed;
    public float rollDurationTime;  // 구르기 하는 시간
    public float rollCoolTime;  // 구르기 대기 시간(쿨타임)
    bool isRoll;  // 대쉬(구르기) 중인지 확인하는 변수
    bool canRoll;  // 구르기가 가능한지 여부 확인 변수
    WaitForSeconds rollDuration;
    WaitForSeconds rollCool;

    bool doBlock;

    Rigidbody2D rigid;
    SpriteRenderer spriteR;
    Animator animator;
    CapsuleCollider2D coll;

    WaitForSeconds attackDelay;
    Collider2D attackedEnemy;

    bool m_Started;  // overlapbox의 크기 확인을 위한 기즈모 on/off 결정 변수

    public Vector2 attackBoxSize;  // overlapbox 크기
    float attackBoxDirection;  // 플레이어 기준 overlapbox가 나올 방향(-방향인지 +방향인지)
    Vector2 attackBoxPosition;  // overlapbox가 생기는 위치

    enum AttackState
    {
        Attack1,
        Attack2,
        Attack3,
        None,
    }

    AttackState attackState;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteR = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        coll = GetComponent<CapsuleCollider2D>();
        moveSpeed = defaultMoveSpeed;
        isGround = true;
        canRoll = true;
        rollDuration = new WaitForSeconds(rollDurationTime);
        rollCool = new WaitForSeconds(rollCoolTime - rollDurationTime);
        attackDelay = new WaitForSeconds(0.42f);
        animator.SetBool("Grounded", isGround);
    }

    private void Update()
    {
        if (cantAnyKeyInput)
            return;

        if (isRoll || isAttack)
            return;

        sightDirection = spriteR.flipX ? -1f : 1f;

        // 벽에 붙어있을 때 불가능한 것
        // 공격, 구르기, 방어
        // 벽에 붙어 있어도 가능한 것
        // 점프(대신 벽 점프라는 다른 기능으로 수행), 아래로 이동(벽에 붙어있을 때만 가능), 벽을 바라보고 있는 방향과 반대되는 방향 키 일정 시간 입력 시 벽에서 떨어지기

        // 앞에 벽이 있는지 확인
        wallCheck = Physics2D.Raycast(transform.position, Vector2.right * sightDirection, 0.5f, groundLayer);
        groundCheck = Physics2D.Raycast(transform.position, Vector2.down, 0.9f, groundLayer);

        // 벽에 붙는 조건
        // 나의 진행 방향에 벽이 있다, 바닥에서 떨어져 있다, 구르지 않고 있다, 공격하지 않고 있다.
        if (!isWall && wallCheck.collider != null && groundCheck.collider == null)  // 벽에 붙어있지 않는 상태인데 앞에 벽이 있고 바닥에 착지하고 있지 않은 상태
        {
            isWall = true;
            isGround = false;
            animator.SetBool("WallSlide", isWall);
            animator.SetBool("Grounded", isGround);
            rigid.linearVelocityY = 0f;  // 벽에 붙으면 멈추도록 y축 속도를 0으로 고정시킨다.
            animator.SetFloat("AirSpeedY", -0.1f);
        }
        else if (isWall && (wallCheck.collider == null || groundCheck.collider != null))  // 벽에 붙어있는데 앞에 벽이 없거나 바닥에 착지하고 있는 상태
        {
            // 착지 여부는 이곳이 아닌 아래에서 진행 - 왜냐하면 벽에서 떨어지는데 땅이 아닐 수도 있기 때문에 값을 바꿔버리면 문제가 발생할 수가 있어서이다.
            isWall = false;
            animator.SetBool("WallSlide", isWall);
        }

        Debug.Log(rigid.linearVelocityY);
        
        if(!isWall && !isGround && rigid.linearVelocityY < 0f)
        {
            if (groundCheck.collider != null)
            {
                isGround = true;
                rigid.gravityScale = 1f;
                animator.SetBool("Grounded", isGround);
                animator.SetFloat("AirSpeedY", 0f);
            }
            else
            {
                rigid.gravityScale = fallSpeed;
                animator.SetFloat("AirSpeedY", rigid.linearVelocityY);
            }
        }

        // 문제 발생
        // 1. 벽에 붙어도 y축 속도가 0이 되지 않음
        // 2. 벽에 붙어 있다가 바닥에 착지하거나 벽 방향과 반대 방향 키를 입력하면 "'Player' AnimationEvent 'AE_SlideDust' on animation 'HeroKnight_WallSlide' has no receiver! Are you missing a component?"라는 문구 발생
        // 3. 벽에 붙어 있을 때 x축을 떨어뜨리기 위한 코드를 실행할 때 제대로 동작하지 않고 애니메이션도 정상 작동하지 않음

        // 벽에 붙을 시 애니메이션 관련
        // 점프를 했을 수도 있으니 점프 여부 상관 없이 Grounded 파라미터 값을 true로 변환, 벽에 붙어 있는 것이기 때문에 WallSide 파라미터 값 true로 변환, 떨어지는 중이 아니기 때문에 AirSpeedY 파라미터 값을 0으로 변환
        // 벽에서 떨어졌을 시 애니메이션 관련
        // 벽에서 떨어진 것이기 때문에 WallSide 파라미터 값 false로 변환, 떨어지고 있을 수도 안 떨어지고 있을 수도 있으니 현재 rigid의 y축 속도를 AirSpeedY 파라미터 값으로 삽입
    }

    void LateUpdate()
    {
        if (cantAnyKeyInput)
            return;

        if (isRoll || isAttack || isWall)
            return;

        if (direction > 0)
            spriteR.flipX = false;
        else if (direction < 0)
            spriteR.flipX = true;
    }

    private void FixedUpdate()
    {
        if (cantAnyKeyInput)
            return;

        if (isRoll || isWall)
            return;

        if (isAttack)
        {
            rigid.linearVelocityX = 0f;
            return;
        }

        rigid.linearVelocityX = direction * moveSpeed;

        if (direction != 0f)
        {
            animator.SetInteger("AnimState", 1);
        }
        else
        {
            animator.SetInteger("AnimState", 0);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (cantAnyKeyInput)
            return;

        direction = context.ReadValue<Vector2>().x;

        if (direction > 0f)
            direction = 1f;
        else if (direction < 0f)
            direction = -1f;

        if (isWall && direction != sightDirection)  // 벽에 붙어있는 상황에서 벽의 방향과 반대 방향 키 입력 시
        {
            isWall = false;
            animator.SetBool("WallSlide", isWall);
            animator.SetFloat("AirSpeedY", -1f);
            rigid.AddForce(Vector2.right * direction * 0.3f, ForceMode2D.Impulse);  // 살짝 반대 쪽으로 튕겨 나가도록 설정
            cantAnyKeyInput = true;
            Invoke("CanAnyKeyInput", 0.1f);
        }

        // 주의점 - 벽에 붙어있을 때는 벽에 딱 달라붙어 있고 아래 키 입력은 밑으로 느리게 내려가도록 한다. 벽을 타고 오르는 것은 허락하지 않는다.
        if (isWall && context.ReadValue<Vector2>().y < 0f)
        {
            rigid.linearVelocityY = -0.5f;
        }
    }

    void CanAnyKeyInput()
    {
        cantAnyKeyInput = false;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (cantAnyKeyInput)
            return;

        if (isRoll || isAttack)
            return;

        if (context.started)
        {
            // 벽 점프의 조건
            // 벽에 붙어있는 중, 점프 키 입력
            // 벽 점프 동작
            // 벽과 반대되는 방향으로 살짝 튕겨 나가면서 위로 점프
            if (isWall)
            {

            }
            else if (isGround)
            {
                isGround = !isGround;
                rigid.gravityScale = 1f;
                animator.SetBool("Grounded", isGround);
                animator.SetTrigger("Jump");
                rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            }
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (cantAnyKeyInput)
            return;

        if (isWall)
            return;

        // J키 입력 - 첫 번째 공격
        // 첫 번째 공격 중 J키 입력 - 두 번째 공격
        // 두 번째 공격 중 J키 입력 - 세 번째 공격
        // 공격이 끝날 때까지 후속 입력이 없다면 해당 공격 종료 후 초기화
        if (!isAttack && isGround && !isRoll && context.started)
        {
            ++attackCount;
            attackBoxDirection = spriteR.flipX ? -1f : 1f;
            StartCoroutine(AttackRoutine());
        } else if (context.started && isAttack && attackCount < 3)
        {
            ++attackCount;
        }
    }


    IEnumerator AttackRoutine()
    {
        isAttack = true;
        attackBoxPosition.x = transform.position.x + attackBoxDirection;
        attackBoxPosition.y = transform.position.y;
        attackState = AttackState.Attack1;
        AttackEnemy();

        m_Started = true;

        yield return attackDelay;

        if (attackCount >= 4)
        {
            Debug.Log("입력 제어 실패");
            attackCount = 0;
            m_Started = false;
            isAttack = false;
            StopCoroutine(AttackRoutine());
        }

        if (attackCount >= 2)
        {
            attackState = AttackState.Attack2;
            AttackEnemy();
            yield return attackDelay;
        }

        if (attackCount == 3)
        {
            attackState = AttackState.Attack3;
            AttackEnemy();
            yield return attackDelay;
        }

        attackCount = 0;
        m_Started = false;

        isAttack = false;
    }

    void AttackEnemy()
    {
        animator.SetTrigger(attackState.ToString());
        attackedEnemy = Physics2D.OverlapBox(attackBoxPosition, attackBoxSize, 0f, enemyLayer);

        if (attackedEnemy != null)
        {
            attackedEnemy.GetComponent<Enemy>().curHp -= 5f;
        }
    }

    void StopAttack()
    {
        StopCoroutine(AttackRoutine());

        // 공격 진행 중에 코루틴을 종료시켰기 때문에 공격에 필요한 값들이 초기화되지 않은 상태가 되기 때문에 초기화를 따로 시켜주어야 한다.
        attackCount = 0;
        isAttack = false;
        m_Started = false;
    }

    public void OnRoll(InputAction.CallbackContext context)
    {
        if (cantAnyKeyInput)
            return;

        if (canRoll && isGround)
        {
            StartCoroutine(RollRoutine());
            doBlock = false;
        }
    }

    IEnumerator RollRoutine()
    {
        // 공격 중이어도 구르기 버튼을 누르면 공격을 중단하고 구르도록 설정
        if (isAttack)
        {
            StopAttack();
        }

        if (doBlock)
        {
            doBlock = false;
            moveSpeed = defaultMoveSpeed;
            animator.SetBool("Idle_Block", doBlock);
        }

        // 구르기를 누르면 애니메이션만 작동되고 이동이 되지 않는 문제가 발생. 해결 요망
        
        isRoll = true;
        canRoll = false;
        rigid.gravityScale = 0f;
        float rollDirection = spriteR.flipX ? -1f : 1f;
        rigid.linearVelocityX = rollDirection * rollSpeed;
        rigid.linearVelocityY = 0f;
        coll.enabled = false;  // 구르는 시간 동안은 무적이 되도록 콜라이더를 잠시 꺼준다.
        animator.SetTrigger("Roll");

        yield return rollDuration;

        isRoll = false;
        coll.enabled = true;
        rigid.gravityScale = fallSpeed;

        yield return rollCool;

        canRoll = true;
    }

    public void OnBlock(InputAction.CallbackContext context)
    {
        if (cantAnyKeyInput)
            return;

        if (context.started)
        {
            doBlock = true;
            moveSpeed = blockMoveSpeed;
            animator.SetBool("IdleBlock", doBlock);
        }

        if (context.canceled || isRoll || isAttack)
        {
            doBlock = false;
            moveSpeed = defaultMoveSpeed;
            animator.SetBool("IdleBlock", doBlock);
        }
    }

    // 방어 중 적에게 공격 피격 시 공격을 방어한 애니메이션을 호출하고 실제로 동작을 막은 것을 표현하는 기능을 추가하여야 한다.

    // overlapbox의 범위를 씬 화면에 그려주기 위해 호출한 함수(함수 내에서 설정한 기즈모를 그려주는 함수로 추정된다.)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.mediumPurple;
        Gizmos.DrawRay(transform.position, Vector2.down * 0.9f);
        Gizmos.DrawRay(transform.position, Vector2.right * sightDirection * 0.5f);
        Gizmos.color = Color.red;
        Vector2 boxCastPos = new Vector2(transform.position.x + 0.235f * wallBoxDirection, transform.position.y);
        if (m_Started)
        {
            Gizmos.DrawWireCube(attackBoxPosition, attackBoxSize);
        }
    }
}

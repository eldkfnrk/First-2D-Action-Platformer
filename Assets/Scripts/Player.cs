using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
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

    // 벽에 붙은 것으로 인식하는 조건
    // 1. 플레이어와 바닥 간 일정 거리 이상 떨어져 있다.
    // 2. 플레이어의 이동 방향에 벽이 존재한다.
    // 점점 내려가서 바닥에 착지하였다고 판단하면 벽에 붙은 것이 아닌 것으로 인식하도록 설정하여야 한다. 그리고 이 거리는 크면 안 되고 작게 가져가야 한다.

    // 벽에 붙은 경우
    // 1. 벽 방향으로 이동 불가, 위로 이동 불가, 아래는 떨어지는 속도가 늘어나도록 설정(점프를 제외한 거의 모든 동작이 실행 불가능하도록 설정하여야 한다.)
    // 2. 벽에 붙었을 때는 자연스럽게 보이도록 조금씩 내려가도록 설정 -> 이는 Update에서 rigidbody의 y축 속도를 0으로 조절하면 알아서 중력이 계산되기에 Update에서 y축 속도를 조절(FixedUpdate에서 조절하면 딱 0이 되어서 떨어지지 않는다. 이 방법도 되는지 실험해 볼 필요는 있다.)
    // 3. 벽 방향과 반대되는 방향 키를 입력하면 살짝의 튕겨나가는 힘과 함께 벽에서 떨어지도록 설정
    // 4. 점프를 누르면 벽과 반대되는 방향으로 튕겨나가면서 점프되도록 설정
    // 3, 4번의 경우 잠깐 동안 다른 키를 입력할 수 없게 막아야 한다. 그리고 이 동작은 AddForce라는 물리 동작을 사용할 것이기 때문에 FixedUpdate에서 실행할 방법을 모색하여야 한다.

    // 벽에 붙지 않은 경우
    // 이동하기, 구르기, 공격하기, 방어하기, 점프하기
    // 구르기 - 구르는 중 다른 방향 이동, 공격, 방어, 점프 불가능(구를 때 앞에 벽이 있는 경우 x축 속도가 0이 되도록 하여야 한다. 구를 때는 콜라이더를 꺼서 충돌 처리가 되지 않기 때문에 수동으로 조작해줘야 한다.)
    // 공격하기 - 공격 중 다른 방향 이동, 점프, 방어 불가능
    // 방어하기 - 방어 중 이동 속도 저하, 구르기나 공격하기 점프하기를 하면 방어 상태 해제
    // 점프하기 - 점프 중 공격, 방어, 구르기 불가능


    // 캐릭터 애니메이션 분석
    // 애니메이션은 한 애니메이션만 동작하고 이 애니메이션이 동작 중인 상황에서 이 애니메이션에 설정된 트랜잭션의 컨디션 상황이 적용되면 트랜잭션을 따라 다른 애니메이션으로 이동하는 구조로 되어 있다.
    // 기본 값 : Idle
    // Idle 상태에서 왔다갔다하는 관계
    // Idle -> Run (AnimState = 1, Grounded = true)
    // Run -> Idle (AnimState != 1)
    // Run -> Fall (Grounded = false, AirSpeedY < 0f)
    // Idle -> Fall (Grounded = false, AirSpeedY < 0f)
    // Fall -> Idle (Grounded = true)
    // Any State : 어떤 애니메이션이 동작 중이던 트랜잭션의 컨디션에 맞는 상황이 오면 바로 해당 애니메이션으로 옮기는 것을 의미
    // Any State -> Idle_Block (IdleBlock = true)
    // Any State -> Attack1 (Attack1 트리거)
    // Any State -> Attack2 (Attack2 트리거)
    // Any State -> Attack3 (Attack3 트리거)
    // Any State -> Roll (Roll 트리거)
    // Any State -> WallSlide (WallSlide = true, Grounded = false, AirSpeedY < 0f)
    // Any State -> Hurt (Hurt 트리거)
    // Any State -> Death (noBlood = false, Death 트리거)
    // Any State -> DeathNoBlood (noBlood = true, Death 트리거)
    // Any State -> Jump (Jump 트리거)
    // Jump는 올라갈 때와 내려갈 때가 나뉘어져 있다.
    // Any State에선 Jump는 트리거로 동작하지만 점프 후 떨어질 때 애니메이션도 따로 있어서 해당 애니메이션 동작을 정의하여야 한다.
    // Jump -> Fall (AirSpeedY < 0f)
    // Any State에서 출발한 애니메이션들 중 트리거로 동작하는 애니메이션들은 애니메이션 동작이 끝나면 Idle로 돌아간다. 그러나 다른 애니메이션들은 조건이 맞아야만 Idle로 돌아가는 애니메이션들도 있다. 그것들을 아래와 같이 정의한다.
    // Idle_Block -> Idle (IdleBlock = false)
    // WallSlide -> Idle (Grounded = true)
    // WallSlide -> Idle (AirSpeedY > 0f)
    // WallSlide 애니메이션의 트랜잭션은 2개로 2개의 조건 중 하나를 만족하면 Idle로 넘어간다.

    // 각 상황에 맞는 설정해야 할 애니메이션 파라미터 값
    // 벽에 붙은 경우 - Grounded = false, WallSlide = true, AirSpeedY < 0f
    // 벽에서 떨어지는 경우(벽과 반대되는 방향 키 입력) - WallSlide = false, AirSpeedY = rigidbody y축 속도(Grounded는 착지를 했을 경우에만 변경)
    // 벽 점프 - Jump 트리거, WallSlide = false, AirSpeedY = rigidbody y축 속도

    private void Update()
    {
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

        if (isWall)
        {
            rigid.linearVelocityY = 0f;
            if (wallCheck.collider == null || groundCheck.collider != null)  // 벽에 붙어있는데 앞에 벽이 없거나 바닥에 착지하고 있는 상태
            {
                // 착지 여부는 이곳이 아닌 아래에서 진행 - 왜냐하면 벽에서 떨어지는데 땅이 아닐 수도 있기 때문에 값을 바꿔버리면 문제가 발생할 수가 있어서이다.
                isWall = false;
                animator.SetBool("WallSlide", isWall);
            }
        }
        // 벽에 붙는 조건
        // 나의 진행 방향에 벽이 있다, 바닥에서 떨어져 있다, 구르지 않고 있다, 공격하지 않고 있다.
        else
        {
            if (wallCheck.collider != null && groundCheck.collider == null)  // 벽에 붙어있지 않는 상태인데 앞에 벽이 있고 바닥에 착지하고 있지 않은 상태
            {
                isWall = true;
                isGround = false;
                animator.SetBool("WallSlide", isWall);
                animator.SetBool("Grounded", isGround);
                rigid.linearVelocityY = 0f;  // 벽에 붙으면 멈추도록 y축 속도를 0으로 고정시킨다.
                animator.SetFloat("AirSpeedY", -0.1f);
            }
        }
        
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

        // 2026-06-01
        // 벽에 붙는 것은 성공했으나 애니메이션 전환이 원할하지 않음. 벽에서 떨어지는 것이 무슨 이유에서인지 한 번 씩 안 되는 문제가 있음(이는 Update와 FixedUpdate, 이벤트 함수 호출이 어긋나기 때문으로 추정 중)

        // 현재 모든 문제가 정확히 파악 없이 진행되는 것에서 야기된 문제이기 때문에 먼저 애니메이션 파악과 벽에 붙어있을 때와 떨어져 있을 때를 구분하여 정확하게 판단하는 것을 우선으로 가지고 가야 뭔가가 이뤄질 것이다.
        // 그럼에도 안 되었다면 아예 지식이 없는 상태인데 문제를 풀어보겠다는 것이기 때문에 계란으로 바위를 부수겠다는 행동이니 이를 지양하도록 검색과 AI를 활용한 해결을 진행하도록 할 것이다. 이는 1~2일 안에 결정하여야 한다.

        // 벽에 붙을 시 애니메이션 관련
        // 점프를 했을 수도 있으니 점프 여부 상관 없이 Grounded 파라미터 값을 true로 변환, 벽에 붙어 있는 것이기 때문에 WallSide 파라미터 값 true로 변환, 떨어지는 중이 아니기 때문에 AirSpeedY 파라미터 값을 0으로 변환
        // 벽에서 떨어졌을 시 애니메이션 관련
        // 벽에서 떨어진 것이기 때문에 WallSide 파라미터 값 false로 변환, 떨어지고 있을 수도 안 떨어지고 있을 수도 있으니 현재 rigid의 y축 속도를 AirSpeedY 파라미터 값으로 삽입
    }

    void LateUpdate()
    {
        if (isRoll || isAttack || isWall)
            return;

        if (direction > 0)
            spriteR.flipX = false;
        else if (direction < 0)
            spriteR.flipX = true;
    }

    private void FixedUpdate()
    {
        if (isRoll)
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
        direction = context.ReadValue<Vector2>().x;

        if (direction > 0f)
            direction = 1f;
        else if (direction < 0f)
            direction = -1f;

        if (isWall && direction != sightDirection)  // 벽에 붙어있는 상황에서 벽의 방향과 반대 방향 키 입력 시
        {
            
        }

        // 주의점 - 벽에 붙어있을 때는 벽에 딱 달라붙어 있고 아래 키 입력은 밑으로 느리게 내려가도록 한다. 벽을 타고 오르는 것은 허락하지 않는다.
        if (isWall && context.ReadValue<Vector2>().y < 0f)
        {
            
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
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
        if (isWall)
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

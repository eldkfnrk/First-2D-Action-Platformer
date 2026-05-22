using System.Collections;
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
    
    RaycastHit2D jumpCheck;
    public float fallSpeed;
    bool isGround;


    bool isAttack;
    int attackCount;  // 공격 횟수를 저장할 변수

    RaycastHit2D wallCheck;
    bool isWall;
    float wallBoxDirection;
    Vector2 wallCheckBoxSize;

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
        wallCheckBoxSize = new Vector2(transform.localScale.x * 0.25f, transform.localScale.y * 0.75f);
    }

    private void Update()
    {
        if (isRoll)
            return;

        // 벽에 붙는 조건
        // 나의 진행 방향에 벽이 있다 && 바닥과 떨어져 있다
        // 벽에 붙어 있을 때
        // 구르기, 공격, 방어 불가능 -> 점프를 누르면 벽과 반대되는 방향으로 떨어지면서 점프
        // 이동 키를 누르고 있어도 미끄러지도록 설정하여야 한다.
        wallBoxDirection = spriteR.flipX ? -1f : 1f;
        wallCheck = Physics2D.BoxCast(transform.position, wallCheckBoxSize, 0f, Vector2.right * wallBoxDirection, 0.35f, groundLayer);
        jumpCheck = Physics2D.Raycast(transform.position, Vector2.down, 1f, groundLayer);

        if (wallCheck.collider != null && jumpCheck.collider == null)
        {
            isWall = true;
            isGround = false;
            direction = 0f;
            rigid.linearVelocityY = -0.2f;  // 서서히 하락하도록
            animator.SetBool("WallSlide", isWall);
            Debug.Log(wallCheck.collider.gameObject);
        }
        else
        {
            isWall = false;
            animator.SetBool("WallSlide", isWall);
        }

        if(rigid.linearVelocityY <= 0 && !isWall)
        {
            animator.SetFloat("AirSpeedY", rigid.linearVelocityY);
            rigid.gravityScale = fallSpeed;
            if (jumpCheck.collider != null && !isGround)
            {
                isGround = true;
                animator.SetBool("Grounded", isGround);
            }
        }
    }

    void LateUpdate()
    {
        if (isRoll || isAttack)
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
        if (context.started || context.performed)
        {
            direction = context.ReadValue<Vector2>().x;
            if (direction > 0)
                direction = 1f;
            else if (direction < 0)
                direction = -1f;
        }
        else if (context.canceled)
            direction = 0f;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (isRoll || isAttack)
            return;

        if (context.started && isGround)
        {
            isGround = !isGround;
            rigid.gravityScale = 1f;
            animator.SetBool("Grounded", isGround);
            animator.SetTrigger("Jump");
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        }
        
        if(context.started && isWall)
        {
            float reverseDirection = spriteR.flipX ? 1f : -1f;  // 반대 방향을 얻는 값
            rigid.AddForce(new Vector2(reverseDirection * 2f, jumpPower), ForceMode2D.Impulse);
            isWall = false;
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
        Gizmos.color = Color.red;
        Vector2 boxCastPos = new Vector2(transform.position.x + 0.235f * wallBoxDirection, transform.position.y);
        Gizmos.DrawWireCube(boxCastPos, wallCheckBoxSize);
        if (m_Started)
        {
            Gizmos.DrawWireCube(attackBoxPosition, attackBoxSize);
        }
    }
}

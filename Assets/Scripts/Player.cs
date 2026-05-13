using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public float moveSpeed;
    public float jumpPower;
    float direction;

    public LayerMask groundLayer;
    public LayerMask enemyLayer;
    RaycastHit2D jumpCheck;
    public bool isGround;
    bool isAttack;

    Rigidbody2D rigid;
    SpriteRenderer spriteR;
    Animator animator;

    WaitForSeconds attackDelay;
    Collider2D attackedEnemy;

    public float distance;  // overlapbox가 플레이어로부터 x축으로 얼마나 떨어져 있도록 하는지 정하는 변수
    public Vector2 boxSize;  // overlapbox 크기

    bool m_Started;  // overlapbox의 크기 확인을 위한 기즈모 on/off 결정 변수

    float boxDirection;  // 플레이어 기준 overlapbox가 나올 방향(-방향인지 +방향인지)
    Vector2 boxPosition;  // overlapbox가 생기는 위치

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteR = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        isGround = true;
        attackDelay = new WaitForSeconds(0.5f);
        animator.SetBool("Grounded", isGround);
    }

    private void Update()
    {
        animator.SetFloat("AirSpeedY", rigid.linearVelocityY);

        if(rigid.linearVelocityY <= 0)
        {
            rigid.gravityScale = 1.75f;
            jumpCheck = Physics2D.Raycast(transform.position, Vector2.down, 1f, groundLayer);
            if (jumpCheck.collider != null && !isGround)
            {
                isGround = true;
                animator.SetBool("Grounded", isGround);
            }
        }
    }

    void LateUpdate()
    {
        if (direction > 0)
            spriteR.flipX = false;
        else if (direction < 0)
            spriteR.flipX = true;
    }

    private void FixedUpdate()
    {
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

    void OnMove(InputValue value)
    {
        if (isAttack)
        {
            direction = 0f;
            return;
        }

        direction = value.Get<Vector2>().x;
    }

    public void OnJump(InputValue value)
    {
        if (isGround)
        {
            isGround = !isGround;
            rigid.gravityScale = 1f;
            animator.SetBool("Grounded", isGround);
            animator.SetTrigger("Jump");
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        }
    }

    void OnAttack(InputValue value)
    {
        if (!isAttack && isGround)
        {
            boxDirection = spriteR.flipX ? -1f : 1f;
            StartCoroutine(AttackRoutine(boxDirection));
        }
    }

    IEnumerator AttackRoutine(float boxDirection)
    {
        isAttack = true;
        animator.SetTrigger("Attack1");
        boxPosition.x = transform.position.x + boxDirection;
        boxPosition.y = transform.position.y;
        attackedEnemy = Physics2D.OverlapBox(boxPosition, boxSize, 0f, enemyLayer);

        if(attackedEnemy != null)
        {
            attackedEnemy.GetComponent<Enemy>().curHp -= 5f;
        }

        m_Started = true;

        yield return attackDelay;

        m_Started = false;

        isAttack = false;
    }

    // overlapbox의 범위를 씬 화면에 그려주기 위해 호출한 함수(함수 내에서 설정한 기즈모를 그려주는 함수로 추정된다.)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (m_Started)
        {
            Gizmos.DrawWireCube(boxPosition, boxSize);
        }
    }
}

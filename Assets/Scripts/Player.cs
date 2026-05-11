using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public float moveSpeed;
    public float jumpPower;
    float direction;

    public LayerMask groundLayer;
    RaycastHit2D jumpCheck;
    bool isGround;
    bool isAttack;

    Rigidbody2D rigid;
    SpriteRenderer spriteR;
    Animator animator;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteR = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        isGround = true;
        animator.SetBool("Grounded", isGround);
    }

    private void Update()
    {
        animator.SetFloat("AirSpeedY", rigid.linearVelocityY);

        if(rigid.linearVelocityY <= 0)
        {
            jumpCheck = Physics2D.Raycast(transform.position, Vector2.down, 0.05f, groundLayer);
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
        direction = value.Get<Vector2>().x;
    }

    public void OnJump(InputValue value)
    {
        if (isGround)
        {
            isGround = !isGround;
            animator.SetBool("Grounded", isGround);
            animator.SetTrigger("Jump");
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        }
    }

    void OnAttack(InputValue value)
    {
        if (!isAttack && isGround)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttack = true;
        animator.SetTrigger("Attack1");

        yield return new WaitForSeconds(0.5f);

        isAttack = false;
    }
}

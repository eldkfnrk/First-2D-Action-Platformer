using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public float moveSpeed;
    public float jumpPower;
    float direction;

    bool isJump;

    Rigidbody2D rigid;
    SpriteRenderer spriteR;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteR = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        Debug.DrawRay(transform.position, Vector2.down, Color.red, 0.5f);

        RaycastHit2D jumpCheck = Physics2D.Raycast(transform.position, Vector2.down, 0.5f, LayerMask.GetMask("Ground"));

        if (jumpCheck.collider != null)
        {
            isJump = false;
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
        rigid.linearVelocityX = direction * moveSpeed;
    }

    void OnMove(InputValue value)
    {
        direction = value.Get<Vector2>().x;
    }

    public void OnJump(InputValue value)
    {
        if (!isJump)
        {
            isJump = true;
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        }
    }
}

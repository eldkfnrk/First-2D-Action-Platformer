using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static PlayerAction;

public class PlayerAction : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Run,
        Jump,
        Fall,
        Attack,
        Roll,
        Block,
        SuccessBlock,
        WallSlide,
        Hurt,
        Death,
    }

    public enum AttackState
    {
        Attack1,
        Attack2,
        Attack3,
        None,
    }

    string interactObjectTag;  // ��ȣ�ۿ� ������ ������Ʈ�� �±װ� �������� �����ϴ� ���ڿ�
    SpawnPoint teleportPoint;  // ��ȣ�ۿ� ������ ������Ʈ�� �����ϴ� ����

    public bool canInput;  // �Է� ���� ���θ� �����ϴ� ����(�Է��� ���ƾ� �ϴ� ��� ����� ���� ����)

    // �÷��̾� ����
    public PlayerState playerState;
    public AttackState attackState;
    float sightDirection;  // �÷��̾ �ٶ󺸰� �ִ� ����(1�̸� ������, -1�̸� ������ �ٶ󺸰� �ִ� ��)

    // �ٴ�, �� ���� �浹ü üũ
    RaycastHit2D groundCheck;
    RaycastHit2D wallCheck;
    public float groundCheckDistance;
    public float wallCheckDistance;
    public LayerMask groundLayer;
    public LayerMask enemyLayer;

    // ���� �پ��� �� �ൿ
    public float slideSpeed;
    float slideValue;  // �Է� ���ο� ���� ���� �����ų ����
    public float pressedSlide;  // �Ʒ� ���� Ű�� �Է��ϸ� slideSpeed �ӵ��� �� ��ġ��ŭ �谡 �Ǿ� ���������� ������ ����
    public bool isWall;
    public float wallJumpDelayTime;
    WaitForSeconds wallJumpDelay;

    // �¿� �̵�
    public float moveSpeed;
    public float moveDirection;

    // ����
    public float jumpPower;
    public bool isJump;
    public bool isGround;
    public float jumpTimer;  // ���� �� ��� ���� �ٴ� �˻縦 ���� �ʴ� �ð�
    public float fallSpeed;

    // ����
    public bool isAttack;
    public int attackCnt;
    Vector2 attackBoxPos;
    public Vector2 attackBoxSize;
    WaitForSeconds attackDelay;
    public float attackDelayData;

    // ������
    public bool isRoll;
    public bool canRoll;
    public float rollSpeed;
    WaitForSeconds rollDuration;
    public float rollDurationData;
    WaitForSeconds rollCoolTime;
    public float rollCoolTimeData;

    // ���
    public bool isBlock;
    public bool blockSuccess;

    // �ǰ�
    public bool isHurt;

    // ĳ���� ���
    public bool isDeath;

    // ������Ʈ
    Rigidbody2D rigid;
    SpriteRenderer spriteR;
    CapsuleCollider2D coll;

    private void Awake()
    {
        playerState = PlayerState.Idle;
        attackState = AttackState.None;
        canRoll = true;
        canInput = true;
        attackDelay = new WaitForSeconds(attackDelayData);
        rollDuration = new WaitForSeconds(rollDurationData);
        rollCoolTime = new WaitForSeconds(rollCoolTimeData - rollDurationData);
        wallJumpDelay = new WaitForSeconds(wallJumpDelayTime);
        rigid = GetComponent<Rigidbody2D>();
        spriteR = GetComponent<SpriteRenderer>();
        coll = GetComponent<CapsuleCollider2D>();
    }

    private void Update()
    {
        sightDirection = spriteR.flipX ? -1f : 1f;
    }

    private void LateUpdate()
    {
        if (isWall || !canInput || isBlock)
            return;

        if (moveDirection > 0f)
            spriteR.flipX = false;
        else if (moveDirection < 0f)
            spriteR.flipX = true;
    }

    private void FixedUpdate()
    {
        if (!canInput)
            return;

        groundCheck = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        wallCheck = Physics2D.Raycast(transform.position, Vector2.right * sightDirection, wallCheckDistance, groundLayer);

        if(groundCheck.collider == null && wallCheck.collider != null)
        {
            isGround = false;
            isJump = false;
            isWall = true;
            playerState = PlayerState.WallSlide;
        }

        rigid.linearVelocityX = moveDirection * moveSpeed;

        switch (playerState)
        {
            case PlayerState.Idle:
                // �̵� Ű�� �ԷµǾ� �ִµ��� �����̴� ���·� ������ �ʴ� ��츦 ���� ���ǹ�
                if (moveDirection != 0f)
                    playerState = PlayerState.Run;

                if (groundCheck.collider == null)
                {
                    isGround = false;
                    isJump = true;
                    playerState = PlayerState.Fall;
                }
                break;
            case PlayerState.Roll:
                if (wallCheck.collider!=null)
                    rigid.linearVelocityX = 0f;
                else
                    rigid.linearVelocityX = rollSpeed * sightDirection;
                break;
            case PlayerState.Attack:
            case PlayerState.Hurt:
            case PlayerState.Death:
                rigid.linearVelocityX = 0f;
                break;
            case PlayerState.Block:
                rigid.linearVelocityX = 0f;
                if (!isBlock)
                    playerState = PlayerState.Idle;
                break;
            case PlayerState.SuccessBlock:
                // ��� ���� ���� �ۼ� ����
                break;
            case PlayerState.WallSlide:
                // ������ �ٰ� ���� ������ ���� �ʰ� ���� ���� ��� �� ���� �ٴ� üũ�� ���� ���ϵ��� ���� Ÿ�̸Ӱ� �ʱ�ȭ�� ���� �ʾƼ� ���� �پ��ٰ� ������ ���Ŀ� ������ �ϸ� ������ ����� ������ ���ϰ� ���� ��ȯ�� ������ �̷����� ���Ͽ����⿡ �̷��� ���� ������ �����ش�.
                jumpTimer = 0f;  
                rigid.gravityScale = 1f;
                rigid.linearVelocityX = 0f;
                rigid.linearVelocityY = (-1f) * slideSpeed * slideValue;  // ������ �ϰ��Ϸ��� �ӵ��� -���� �ϱ� ������ -1f�� ���Ͽ���.

                if(groundCheck.collider != null)
                {
                    isWall = false;
                    isGround = true;
                    playerState = PlayerState.Idle;
                }
                break;
            case PlayerState.Run:
                if (moveDirection == 0f)
                    playerState = PlayerState.Idle;

                if (groundCheck.collider == null)
                {
                    isGround = false;
                    isJump = true;
                    playerState = PlayerState.Fall;
                }
                break;
            case PlayerState.Jump:
                // 0.25�� ���� �ٴ� üũ x -> ���� �ٴ� üũ�� ���� ���ڸ��� ������ ������ �����Ǵ� ������ �ذ�
                jumpTimer += Time.fixedDeltaTime;
                if (jumpTimer < 0.25f)
                    break;

                if (rigid.linearVelocityY < 0f)
                {
                    jumpTimer = 0f;
                    playerState = PlayerState.Fall;
                }
                else if (groundCheck.collider != null)
                {
                    isGround = true;
                    isJump = false;
                    jumpTimer = 0f;
                    playerState = PlayerState.Idle;
                }
                break;
            case PlayerState.Fall:
                if (rigid.gravityScale == 1f)
                {
                    rigid.gravityScale = fallSpeed;
                }

                if (groundCheck.collider != null)
                {
                    isGround = true;
                    isJump = false;
                    rigid.gravityScale = 1f;
                    playerState = PlayerState.Idle;
                }
                break;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!canInput)
            return;

        // �̵� ���� �� ȹ��
        // ���°� Idle�� ���� ���� ��ȯ
        // ���°� Jump Ȥ�� Fall�� ���� ��ȯx
        // ���°� Attack, Roll, Hurt, Death, Block�� ��쿡�� �̵� �Ұ�
        // ���°� WallSlide�� ���� �Ʒ��� �̵��ϴ� ���� ȹ��, �¿� �̵� �Ұ�
        moveDirection = context.ReadValue<Vector2>().x;
        if (moveDirection > 0f)
            moveDirection = 1f;
        else if (moveDirection < 0f)
            moveDirection = -1f;

        switch (playerState)
        {
            case PlayerState.Roll:
            case PlayerState.Attack:
                moveDirection = 0f;
                break;
            case PlayerState.WallSlide:
                slideValue = context.ReadValue<Vector2>().y < 0f ? pressedSlide : 1f;
                break;
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        // �Է°� ���ÿ� ����
        // ��� ���¿����� ����
        // ���°� Attack, Roll, Hurt, Death�� ��쿡�� �Ұ�
        // ���°� Block�� ��� Block�� �ϱ� ���� ����Ǿ��� ������ �����·� ����
        switch (playerState)
        {
            case PlayerState.Roll:
            case PlayerState.Attack:
            case PlayerState.Hurt:
            case PlayerState.Death:
            case PlayerState.Fall:
            case PlayerState.SuccessBlock:
                return;
            case PlayerState.Block:
                isBlock = false;
                break;
        }

        if (isJump || !canInput)
            return;

        if (context.started)
        {
            if (playerState == PlayerState.WallSlide)
            {
                StartCoroutine(WallJumpRoutine());
            }
            else
            {
                rigid.gravityScale = 1f;
                rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
                isGround = false;
                isJump = true;
                playerState = PlayerState.Jump;
            }
        }
    }

    IEnumerator WallJumpRoutine()
    {
        canInput = false;
        rigid.gravityScale = 1f;
        // 0.5f�� ������ ��¦ ƨ�ܳ������� �ϱ� ���� ��(-�� ������ ���� �ݴ� �������� �����ϱ� ����)
        rigid.AddForce(new Vector2(-5f * sightDirection, jumpPower), ForceMode2D.Impulse);
        isWall = false;
        isGround = false;
        isJump = true;
        playerState = PlayerState.Jump;

        yield return wallJumpDelay;

        canInput = true;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!canInput)
            return;

        // ���� - �ִ� 3ȸ���� ���� ���� ����
        // ���°� Jump, Roll, WallSlide�� ��� �Ұ�
        // ���°� Block�� ��� Block�� �ϱ� ���� ����Ǿ��� ������ �����·� ����
        switch (playerState)
        {
            case PlayerState.Roll:
            case PlayerState.Jump:
            case PlayerState.Hurt:
            case PlayerState.Death:
            case PlayerState.Fall:
            case PlayerState.WallSlide:
            case PlayerState.SuccessBlock:
                return;
            case PlayerState.Block:
                isBlock = false;
                break;
        }

        if (context.started)
        {
            if (attackCnt == 0)
            {
                ++attackCnt;
                StartCoroutine(AttackRoutine());
            }
            else if (attackCnt <= 2)
            {
                ++attackCnt;
            }
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttack = true;
        playerState = PlayerState.Attack;
        attackState = AttackState.Attack1;
        // overlapbox ��ġ ����
        attackBoxPos.x = transform.position.x + sightDirection;
        attackBoxPos.y = transform.position.y;
        yield return new WaitForSeconds(0.2f);  // 약간의 휘두르는 시간을 주는 작업(이게 없으면 휘두르지도 않았는데 공격을 한 판정이 된다.)
        EnemyAttack();

        //yield return attackDelay;
        yield return new WaitForSeconds(0.3f);  // 임시로 공격 지연 시간을 따로 설정(위의 휘두르는 시간을 주고 남은 시간만큼 더 지나야 다음 공격이 되도록 공격 딜레이 시간을 맞추기 위해 임시로 수행)

        if (attackCnt >= 2)
        {
            attackState = AttackState.Attack2;
            yield return new WaitForSeconds(0.2f);
            EnemyAttack();
            //yield return attackDelay;
            yield return new WaitForSeconds(0.3f);
        }

        if (attackCnt == 3)
        {
            attackState = AttackState.Attack3;
            yield return new WaitForSeconds(0.2f);
            EnemyAttack();
            //yield return attackDelay;
            yield return new WaitForSeconds(0.3f);
        }

        isAttack = false;
        attackCnt = 0;
        playerState = PlayerState.Idle;
        attackState = AttackState.None;
    }

    void EnemyAttack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(attackBoxPos, attackBoxSize, 0f, enemyLayer);

        // 일단 직접 적의 피격 함수를 호출(이후 더 나은 방법이 존재한다면 수정)
        foreach(Collider2D hitEnemy in hitEnemies)
        {
            // 우선은 이름을 나누어서 정리했기 때문에 이름에 따라 호출 방식을 다르게 가져간다.
            // 이후에는 하나의 큰 매니저를 만들거나 부모 클래스를 만들어서 사용할 것이다.
            string enemyName = hitEnemy.name.Substring(0, 7);
            if (enemyName == "Enemy A")
                hitEnemy.GetComponent<EnemyA>().Hit();
            else if (enemyName == "Enemy B")
                hitEnemy.GetComponent<EnemyB>().Hit();
        }
    }

    public void OnRoll(InputAction.CallbackContext context)
    {
        if (!canInput)
            return;

        // ������
        // ���°� Jump, WallSlide�� �� �Ұ�
        // ���°� Block�� ��� Block�� �ϱ� ���� ����Ǿ��� ������ �����·� ����
        // ���� ���� ��� �ش� ������ �����ϰ� ����Ǿ��� ������ �����·� ����
        switch (playerState)
        {
            case PlayerState.Jump:
            case PlayerState.Attack:
            case PlayerState.Hurt:
            case PlayerState.Death:
            case PlayerState.Fall:
            case PlayerState.WallSlide:
            case PlayerState.SuccessBlock:
                return;
            case PlayerState.Block:
                isBlock = false;
                break;
        }

        if (!isRoll && canRoll)
            StartCoroutine(RollRoutine());
    }

    IEnumerator RollRoutine()
    {
        isRoll = true;
        canRoll = false;
        rigid.gravityScale = 0f;
        rigid.linearVelocityX = rollSpeed * sightDirection;
        coll.enabled = false;
        playerState = PlayerState.Roll;

        yield return rollDuration;

        isRoll = false;
        rigid.gravityScale = 1f;
        coll.enabled = true;
        playerState = PlayerState.Idle;

        yield return rollCoolTime;

        canRoll = true;
    }

    public void OnBlock(InputAction.CallbackContext context)
    {
        if (!canInput || isAttack)
            return;

        // ���
        // ���°� Jump, WallSlide, Fall, Hurt, Death�� ��� �Ұ�
        // �����̰� �־��� ��쿡�� ��� �� �ڸ����� ���߰� ��� �¼��� ��ȯ
        switch (playerState)
        {
            case PlayerState.Roll:
            case PlayerState.Attack:
            case PlayerState.Hurt:
            case PlayerState.Death:
            case PlayerState.Fall:
            case PlayerState.Jump:
            case PlayerState.WallSlide:
                return;
        }

        if (context.canceled)
        {
            isBlock = false;
            playerState = PlayerState.Idle;
        }
        else
        {
            isBlock = true;
            playerState = PlayerState.Block;
        }
    }

    public void OnSuccessBlock(InputAction.CallbackContext context)
    {
        if (isBlock && !blockSuccess)
            StartCoroutine(SuccessBlockRoutine());
    }

    IEnumerator SuccessBlockRoutine()
    {
        blockSuccess = true;
        playerState = PlayerState.SuccessBlock;

        yield return new WaitForSeconds(0.4f);

        blockSuccess = false;

        if (isBlock)
            playerState = PlayerState.Block;
        else
            playerState = PlayerState.Idle;
    }

    public void OnDeath(InputAction.CallbackContext context)
    {
        switch (playerState)
        {
            case PlayerState.Roll:
            case PlayerState.Attack:
            case PlayerState.Hurt:
            case PlayerState.Fall:
            case PlayerState.Jump:
            case PlayerState.Block:
            case PlayerState.WallSlide:
            case PlayerState.SuccessBlock:
                return;
        }

        // �����ϴ����� Ȯ���ϵ��� Ű �Է����� ������ ��� �����ϵ��� ��� �߰�
        if (!isDeath)
        {
            isDeath = true;
            playerState = PlayerState.Death;
        }
        else
        {
            isDeath = false;
            playerState = PlayerState.Idle;
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (teleportPoint != null)
        {
            interactObjectTag = "";
            GameManager.instance.SceneChange(teleportPoint);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Enemy"))
        {
            StartCoroutine(HurtRoutine(collision.transform));
        }
    }

    IEnumerator HurtRoutine(Transform enemy)
    {
        playerState = PlayerState.Hurt;
        float knockbackDir = transform.position.x - enemy.position.x;
        knockbackDir /= Mathf.Abs(knockbackDir);  // 크기를 1로 만들고 +- 값만 획득
        // 넉백
        rigid.AddForce(new Vector2(knockbackDir * 6.5f, 3f), ForceMode2D.Impulse);
        canInput = false;
        Time.timeScale = 0.5f;
        yield return new WaitForSeconds(0.25f);
        playerState = PlayerState.Idle;
        Time.timeScale = 1f;
        canInput = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        interactObjectTag = collision.tag;

        if(interactObjectTag == "Door")
        {
            teleportPoint = collision.GetComponent<SpawnPoint>();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        interactObjectTag = "";
        if(teleportPoint != null)
        {
            teleportPoint = null;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(transform.position, Vector2.down * 0.9f);
    }
}

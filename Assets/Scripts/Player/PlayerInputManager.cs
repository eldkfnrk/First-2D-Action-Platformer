using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class PlayerInputManager : MonoBehaviour
{
    PlayerRuntimeData variableData;
    float directionData;

    private void Awake()
    {
        variableData = GetComponent<PlayerRuntimeData>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        directionData = context.ReadValue<Vector2>().x;

        // y키를 같이 누르게 되면 x의 값이 작아지게 되는데 그러면 이동 속도에 영향을 끼치게 되므로 값을 일정하게 유지할 수 있도록 설정
        if (directionData > 0f)
            variableData.direction = 1f;
        if (directionData < 0f)
            variableData.direction = -1f;

        if(context.canceled)
            variableData.direction = 0f;
    }

    //public void OnJump(InputAction.CallbackContext context)
    //{
    //    // �Է°� ���ÿ� ����
    //    // ��� ���¿����� ����
    //    // ���°� Attack, Roll, Hurt, Death�� ��쿡�� �Ұ�
    //    // ���°� Block�� ��� Block�� �ϱ� ���� ����Ǿ��� ������ �����·� ����
    //    switch (playerState)
    //    {
    //        case PlayerState.Roll:
    //        case PlayerState.Attack:
    //        case PlayerState.Hurt:
    //        case PlayerState.Death:
    //        case PlayerState.Fall:
    //        case PlayerState.SuccessBlock:
    //            return;
    //        case PlayerState.Block:
    //            isBlock = false;
    //            break;
    //    }

    //    if (isJump || !canInput)
    //        return;

    //    if (context.started)
    //    {
    //        if (playerState == PlayerState.WallSlide)
    //        {
    //            StartCoroutine(WallJumpRoutine());
    //        }
    //        else
    //        {
    //            rigid.gravityScale = 1f;
    //            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
    //            isGround = false;
    //            isJump = true;
    //            playerState = PlayerState.Jump;
    //        }
    //    }
    //}

    //IEnumerator WallJumpRoutine()
    //{
    //    canInput = false;
    //    rigid.gravityScale = 1f;
    //    // 0.5f�� ������ ��¦ ƨ�ܳ������� �ϱ� ���� ��(-�� ������ ���� �ݴ� �������� �����ϱ� ����)
    //    rigid.AddForce(new Vector2(-5f * sightDirection, jumpPower), ForceMode2D.Impulse);
    //    isWall = false;
    //    isGround = false;
    //    isJump = true;
    //    playerState = PlayerState.Jump;

    //    yield return wallJumpDelay;

    //    canInput = true;
    //}

    //public void OnAttack(InputAction.CallbackContext context)
    //{
    //    if (!canInput)
    //        return;

    //    // ���� - �ִ� 3ȸ���� ���� ���� ����
    //    // ���°� Jump, Roll, WallSlide�� ��� �Ұ�
    //    // ���°� Block�� ��� Block�� �ϱ� ���� ����Ǿ��� ������ �����·� ����
    //    switch (playerState)
    //    {
    //        case PlayerState.Roll:
    //        case PlayerState.Jump:
    //        case PlayerState.Hurt:
    //        case PlayerState.Death:
    //        case PlayerState.Fall:
    //        case PlayerState.WallSlide:
    //        case PlayerState.SuccessBlock:
    //            return;
    //        case PlayerState.Block:
    //            isBlock = false;
    //            break;
    //    }

    //    if (context.started)
    //    {
    //        if (attackCnt == 0)
    //        {
    //            ++attackCnt;
    //            StartCoroutine(AttackRoutine());
    //        }
    //        else if (attackCnt <= 2)
    //        {
    //            ++attackCnt;
    //        }
    //    }
    //}

    //IEnumerator AttackRoutine()
    //{
    //    isAttack = true;
    //    playerState = PlayerState.Attack;
    //    attackState = AttackState.Attack1;
    //    // overlapbox ��ġ ����
    //    attackBoxPos.x = transform.position.x + sightDirection;
    //    attackBoxPos.y = transform.position.y;
    //    yield return new WaitForSeconds(0.2f);  // 약간의 휘두르는 시간을 주는 작업(이게 없으면 휘두르지도 않았는데 공격을 한 판정이 된다.)
    //    EnemyAttack();

    //    //yield return attackDelay;
    //    yield return new WaitForSeconds(0.3f);  // 임시로 공격 지연 시간을 따로 설정(위의 휘두르는 시간을 주고 남은 시간만큼 더 지나야 다음 공격이 되도록 공격 딜레이 시간을 맞추기 위해 임시로 수행)

    //    if (attackCnt >= 2)
    //    {
    //        attackState = AttackState.Attack2;
    //        yield return new WaitForSeconds(0.2f);
    //        EnemyAttack();
    //        //yield return attackDelay;
    //        yield return new WaitForSeconds(0.3f);
    //    }

    //    if (attackCnt == 3)
    //    {
    //        attackState = AttackState.Attack3;
    //        yield return new WaitForSeconds(0.2f);
    //        EnemyAttack();
    //        //yield return attackDelay;
    //        yield return new WaitForSeconds(0.3f);
    //    }

    //    isAttack = false;
    //    attackCnt = 0;
    //    playerState = PlayerState.Idle;
    //    attackState = AttackState.None;
    //}

    //public void OnRoll(InputAction.CallbackContext context)
    //{
    //    if (!canInput)
    //        return;

    //    // ������
    //    // ���°� Jump, WallSlide�� �� �Ұ�
    //    // ���°� Block�� ��� Block�� �ϱ� ���� ����Ǿ��� ������ �����·� ����
    //    // ���� ���� ��� �ش� ������ �����ϰ� ����Ǿ��� ������ �����·� ����
    //    switch (playerState)
    //    {
    //        case PlayerState.Jump:
    //        case PlayerState.Attack:
    //        case PlayerState.Hurt:
    //        case PlayerState.Death:
    //        case PlayerState.Fall:
    //        case PlayerState.WallSlide:
    //        case PlayerState.SuccessBlock:
    //            return;
    //        case PlayerState.Block:
    //            isBlock = false;
    //            break;
    //    }

    //    if (!isRoll && canRoll)
    //        StartCoroutine(RollRoutine());
    //}

    //IEnumerator RollRoutine()
    //{
    //    isRoll = true;
    //    canRoll = false;
    //    rigid.gravityScale = 0f;
    //    rigid.linearVelocityX = rollSpeed * sightDirection;
    //    coll.enabled = false;
    //    playerState = PlayerState.Roll;

    //    yield return rollDuration;

    //    isRoll = false;
    //    rigid.gravityScale = 1f;
    //    coll.enabled = true;
    //    playerState = PlayerState.Idle;

    //    yield return rollCoolTime;

    //    canRoll = true;
    //}

    //public void OnBlock(InputAction.CallbackContext context)
    //{
    //    if (!canInput || isAttack)
    //        return;

    //    // ���
    //    // ���°� Jump, WallSlide, Fall, Hurt, Death�� ��� �Ұ�
    //    // �����̰� �־��� ��쿡�� ��� �� �ڸ����� ���߰� ��� �¼��� ��ȯ
    //    switch (playerState)
    //    {
    //        case PlayerState.Roll:
    //        case PlayerState.Attack:
    //        case PlayerState.Hurt:
    //        case PlayerState.Death:
    //        case PlayerState.Fall:
    //        case PlayerState.Jump:
    //        case PlayerState.WallSlide:
    //            return;
    //    }

    //    if (context.canceled)
    //    {
    //        isBlock = false;
    //        playerState = PlayerState.Idle;
    //    }
    //    else
    //    {
    //        isBlock = true;
    //        playerState = PlayerState.Block;
    //    }
    //}

    //public void OnSuccessBlock(InputAction.CallbackContext context)
    //{
    //    if (isBlock && !blockSuccess)
    //        StartCoroutine(SuccessBlockRoutine());
    //}

    //IEnumerator SuccessBlockRoutine()
    //{
    //    blockSuccess = true;
    //    playerState = PlayerState.SuccessBlock;

    //    yield return new WaitForSeconds(0.4f);

    //    blockSuccess = false;

    //    if (isBlock)
    //        playerState = PlayerState.Block;
    //    else
    //        playerState = PlayerState.Idle;
    //}

    //public void OnDeath(InputAction.CallbackContext context)
    //{
    //    switch (playerState)
    //    {
    //        case PlayerState.Roll:
    //        case PlayerState.Attack:
    //        case PlayerState.Hurt:
    //        case PlayerState.Fall:
    //        case PlayerState.Jump:
    //        case PlayerState.Block:
    //        case PlayerState.WallSlide:
    //        case PlayerState.SuccessBlock:
    //            return;
    //    }

    //    // �����ϴ����� Ȯ���ϵ��� Ű �Է����� ������ ��� �����ϵ��� ��� �߰�
    //    if (!isDeath)
    //    {
    //        isDeath = true;
    //        playerState = PlayerState.Death;
    //    }
    //    else
    //    {
    //        isDeath = false;
    //        playerState = PlayerState.Idle;
    //    }
    //}

    //public void OnInteract(InputAction.CallbackContext context)
    //{
    //    if (teleportPoint != null)
    //    {
    //        interactObjectTag = "";
    //        GameManager.instance.SceneChange(teleportPoint);
    //    }
    //}

    //private void OnCollisionEnter2D(Collision2D collision)
    //{
    //    if (collision.collider.CompareTag("Enemy"))
    //    {
    //        StartCoroutine(HurtRoutine(collision.transform));
    //    }
    //}

    //IEnumerator HurtRoutine(Transform enemy)
    //{
    //    playerState = PlayerState.Hurt;
    //    float knockbackDir = transform.position.x - enemy.position.x;
    //    knockbackDir /= Mathf.Abs(knockbackDir);  // 크기를 1로 만들고 +- 값만 획득
    //    // 넉백
    //    rigid.AddForce(new Vector2(knockbackDir * 6.5f, 3f), ForceMode2D.Impulse);
    //    canInput = false;
    //    Time.timeScale = 0.5f;
    //    yield return new WaitForSeconds(0.25f);
    //    playerState = PlayerState.Idle;
    //    Time.timeScale = 1f;
    //    canInput = true;
    //}
}

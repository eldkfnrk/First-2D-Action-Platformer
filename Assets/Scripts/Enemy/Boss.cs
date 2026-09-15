using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Boss : MonoBehaviour
{
    public enum ActionState
    {
        Idle,
        Move,
        Dash,
        Attack,  // 공격은 따로 어떤 공격인지 구분이 가능하도록 enum을 하나 사용
    }

    public enum AttackState
    {
        OneAtk,
        TwoAtk,
        SlideAtk,
        DashAtk,
        JumpAtk,
        None,
    }

    public ActionState curActionState;
    public AttackState atkState;
    public BossData constantData;
    Rigidbody2D rigid;
    Collider2D coll;
    SpriteRenderer sprireR;
    BossAnimation bossAnimation;
    BossRuntimeData runtimeData;
    BossFSM fsm;

    float moveDirection;  // 이동 시에만 사용하는 변수이기 때문에 지금은 여기다가 선언하여 진행(추후 runtimeData로 옮겨서 사용할 수도 있음)


    float timer;  // 테스트를 위한 임시 타이머

    private void Awake()
    {
        curActionState = ActionState.Idle;
        atkState = AttackState.None;
        rigid = GetComponent<Rigidbody2D>();
        coll = GetComponent<CapsuleCollider2D>();
        sprireR = GetComponent<SpriteRenderer>();
        bossAnimation = GetComponent<BossAnimation>();
        runtimeData = GetComponent<BossRuntimeData>();
        fsm = GetComponent<BossFSM>();

        runtimeData.sightDirection = sprireR.flipX ? -1f : 1f;
    }

    void ChangeDirection()
    {
        runtimeData.sightDirection *= -1f;
        sprireR.flipX = !sprireR.flipX;
    }

    public void EnterIdle()
    {
        curActionState = ActionState.Idle;
        rigid.linearVelocityX = 0f;
        runtimeData.isGround = true;
        bossAnimation.PlayIdle();
    }

    public void EnterMove()
    {
        curActionState = ActionState.Move;
        bossAnimation.PlayMove();
    }

    public void EnterDash()
    {
        curActionState = ActionState.Dash;
        StartCoroutine(DashRoutine());
    }

    public void EnterAttack()
    {
        curActionState = ActionState.Attack;

        // 연속 공격이라 해도 동작으로는 attack - idle(1프레임) - attack 방식으로 동작하도록 하여 fsm에 문제가 발생하지 않도록 설정
        switch (atkState)
        {
            case AttackState.OneAtk:
                StartCoroutine(OneAttackRoutine());
                break;
            case AttackState.TwoAtk:
                break;
            case AttackState.SlideAtk:
                StartCoroutine(SlideAttackRoutine());
                break;
            case AttackState.DashAtk:
                break;
            case AttackState.JumpAtk:
                break;
            default:
                // None 상태인데 Attack 상태로 들어왔다면 문제가 있는 것으로 간주하고 Idle 상태로 이전(혹시 모를 에러 방지)
                fsm.ChangeState(ActionState.Idle);
                break;
        }
    }

    public void ActionIdle()
    {
        timer += Time.deltaTime;

        if (timer > 2f)
        {
            timer = 0f;
            atkState = AttackState.OneAtk;
            fsm.ChangeState(ActionState.Attack);
        }
    }

    public void ActionMove()
    {
        // 플레이어와의 거리가 가까워지면 패턴을 수행하는 것으로 변경 예정
        // 지금은 당장 대쉬를 테스트 해보기 위한 타이머 동작을 해 볼 예정
        timer += Time.deltaTime;
        if (timer > 2f)
        {
            timer = 0f;
            fsm.ChangeState(ActionState.Dash);
            return;
        }

        moveDirection = GameManager.instance.player.transform.position.x - transform.position.x;
        moveDirection = moveDirection / Mathf.Abs(moveDirection);
        if (moveDirection != runtimeData.sightDirection)
            ChangeDirection();
        rigid.linearVelocityX = moveDirection * constantData.moveSpeed;
    }

    IEnumerator DashRoutine()
    {
        rigid.linearVelocityX = runtimeData.sightDirection * constantData.dashSpeed;
        rigid.gravityScale = 0f;
        coll.enabled = false;
        bossAnimation.PlayDash();
        yield return new WaitForSeconds(0.4f);
        rigid.gravityScale = 1f;
        coll.enabled = true;
        fsm.ChangeState(ActionState.Idle);
    }

    IEnumerator OneAttackRoutine()
    {
        bossAnimation.PlayOneAttack();
        // 애니메이터의 트랜지션의 조건으로 애니메이션 종료를 하기 위해서는 has exit time의 체크를 해제하여야 한다.(즉각적인 전환을 위해서는 필요하고 즉각적인 전환이 아닌 경우는 체크해야 한다.)
        // 이것 때문에 애니메이션이 끝나도 또 다시 재생되는 문제가 발생하였고 has exit time 체크를 해제하면서 해결하였다.

        yield return new WaitForSeconds(1.2f);

        atkState = AttackState.None;
        fsm.ChangeState(ActionState.Idle);
    }

    IEnumerator SlideAttackRoutine()
    {
        rigid.linearVelocityX = runtimeData.sightDirection * constantData.slideSpeed;
        rigid.gravityScale = 0f;
        bossAnimation.PlaySlideAttack();
        yield return new WaitForSeconds(0.15f);
        rigid.gravityScale = 1f;
        atkState = AttackState.None;
        fsm.ChangeState(ActionState.Idle);
    }
}

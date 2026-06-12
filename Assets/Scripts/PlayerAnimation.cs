using System.Collections;
using UnityEngine;
using static PlayerAction;

public class PlayerAnimation : MonoBehaviour
{
    Animator animator;
    PlayerAction playerAction;

    bool playingAttackAnimation;

    public float attackDelayTime;
    WaitForSeconds attackDelay;

    AnimatorStateInfo curAnimatorState;  // 애니메이터의 상태 정보를 취득하기 위한 변수

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerAction = GetComponent<PlayerAction>();
        animator.SetBool("Grounded", true);
        attackDelay = new WaitForSeconds(attackDelayTime);
    }

    void Update()
    {
        curAnimatorState = animator.GetCurrentAnimatorStateInfo(0);

        switch (playerAction.playerState)
        {
            case PlayerAction.PlayerState.Idle:
                animator.SetInteger("AnimState", 0);
                animator.SetFloat("AirSpeedY", 0f);  // 이 파라미터 값을 바꿔주지 않으면 계속하여 해당 파라미터 값이 -1f인 상황이 되고 그러면 점프 애니메이션이 꼬이는 문제가 발생한다.
                animator.SetBool("Grounded", true);
                animator.SetBool("IdleBlock", false);
                animator.SetBool("WallSlide", false);
                break;
            case PlayerAction.PlayerState.Run:
                animator.SetInteger("AnimState", 1);
                break;
            case PlayerAction.PlayerState.Jump:
                // Idle에서 AirSpeedY라는 파라미터 값을 바꿔주지 않으면 아래와 같은 상황이 벌어진다.
                // 트리거를 통해 Jump 애니메이션 재생 -> Fall 상태가 되면서 AirSpeedY 파라미터의 값이 -1f로 변환(맨 처음 점프 때 발생)
                // 트리거를 통해 Jump 애니메이션 재생 -> Jump 애니메이션이 된 상태에서 AirSpeedY 파라미터의 값이 -1f인 상태로 그대로 있어서 바로 Fall 애니메이션을 재생(맨 처음 이후 점프에서 발생)
                // 이렇게 되면서 계속 Jump만 진행되어야 하는데 Fall로 넘어가서 정상적으로 애니메이션이 동작하지 않게 된 것이었다.
                if (!curAnimatorState.IsName("Jump"))
                {
                    animator.SetTrigger("Jump");
                    animator.SetBool("Grounded", false);
                    animator.SetFloat("AirSpeedY", 1f);
                    animator.SetBool("WallSlide", false);
                }
                break;
            case PlayerAction.PlayerState.Fall:
                animator.SetFloat("AirSpeedY", -1f);
                break;
            case PlayerAction.PlayerState.Attack:
                if (!playingAttackAnimation)
                    StartCoroutine(AttackAnimationRoutine());
                break;
            case PlayerAction.PlayerState.Roll:
                if (!curAnimatorState.IsName("Roll"))
                    animator.SetTrigger("Roll");
                break;
            case PlayerAction.PlayerState.Block:
                animator.SetBool("IdleBlock", true);
                break;
            case PlayerAction.PlayerState.WallSlide:
                if (!curAnimatorState.IsName("WallSlide"))
                {
                    animator.SetBool("WallSlide", true);
                    animator.SetBool("Grounded", false);
                    animator.SetFloat("AirSpeedY", -1f);
                }
                break;
        }
    }

    IEnumerator AttackAnimationRoutine()
    {
        playingAttackAnimation = true;

        //if(playerAction.attackState == PlayerAction.AttackState.Attack1 && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack1"))
        //    animator.SetTrigger("Attack1");
        //else if(playerAction.attackState == PlayerAction.AttackState.Attack2 && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack2"))
        //    animator.SetTrigger("Attack2");
        //else if (playerAction.attackState == PlayerAction.AttackState.Attack3 && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack3"))
        //    animator.SetTrigger("Attack3");
        animator.SetTrigger("Attack1");

        yield return attackDelay;

        if (playerAction.attackState == PlayerAction.AttackState.Attack2)
        {
            animator.SetTrigger("Attack2");
            yield return attackDelay;
        }

        if (playerAction.attackState == PlayerAction.AttackState.Attack3)
        {
            animator.SetTrigger("Attack3");
            yield return attackDelay;
        }

        playingAttackAnimation = false;
    }
}

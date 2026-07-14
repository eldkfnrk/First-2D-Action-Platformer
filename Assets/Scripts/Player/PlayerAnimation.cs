//using System.Collections;
//using UnityEngine;

//public class PlayerAnimation : MonoBehaviour
//{
//    PlayerState _state;

//    Animator animator;

//    bool playingAttackAnimation;
//    bool playingHurtAnimation;
//    bool playingBlockAnimation;

//    public float attackDelayTime;
//    WaitForSeconds attackDelay;

//    AnimatorStateInfo curAnimatorState;  // 애니메이터의 상태 정보를 취득하기 위한 변수

//    private void Awake()
//    {
//        _state = GetComponent<PlayerState>();
//        animator = GetComponent<Animator>();
//        animator.SetBool("Grounded", true);
//        attackDelay = new WaitForSeconds(attackDelayTime);
//    }

//    void Update()
//    {
//        curAnimatorState = animator.GetCurrentAnimatorStateInfo(0);

//        switch (_state.playerState)
//        {
//            case PlayerState.State.Idle:
//                animator.SetInteger("AnimState", 0);
//                animator.SetFloat("AirSpeedY", 0f);  // 이 파라미터 값을 바꿔주지 않으면 계속하여 해당 파라미터 값이 -1f인 상황이 되고 그러면 점프 애니메이션이 꼬이는 문제가 발생한다.
//                animator.SetBool("Grounded", true);
//                animator.SetBool("IdleBlock", false);
//                animator.SetBool("WallSlide", false);
//                break;
//            case PlayerState.State.Run:
//                animator.SetInteger("AnimState", 1);
//                break;
//            case PlayerState.State.Jump:
//                // Idle에서 AirSpeedY라는 파라미터 값을 바꿔주지 않으면 아래와 같은 상황이 벌어진다.
//                // 트리거를 통해 Jump 애니메이션 재생 -> Fall 상태가 되면서 AirSpeedY 파라미터의 값이 -1f로 변환(맨 처음 점프 때 발생)
//                // 트리거를 통해 Jump 애니메이션 재생 -> Jump 애니메이션이 된 상태에서 AirSpeedY 파라미터의 값이 -1f인 상태로 그대로 있어서 바로 Fall 애니메이션을 재생(맨 처음 이후 점프에서 발생)
//                // 이렇게 되면서 계속 Jump만 진행되어야 하는데 Fall로 넘어가서 정상적으로 애니메이션이 동작하지 않게 된 것이었다.
//                if (!curAnimatorState.IsName("Jump"))
//                {
//                    animator.SetTrigger("Jump");
//                    animator.SetBool("Grounded", false);
//                    animator.SetFloat("AirSpeedY", 1f);
//                    animator.SetBool("WallSlide", false);
//                }
//                break;
//            case PlayerState.State.Fall:
//                animator.SetFloat("AirSpeedY", -1f);
//                break;
//            case PlayerState.State.Attack:
//                if (!playingAttackAnimation)
//                {
//                    animator.SetBool("IdleBlock", false);
//                    StartCoroutine(AttackAnimationRoutine());
//                }
//                break;
//            case PlayerState.State.Roll:
//                if (!curAnimatorState.IsName("Roll"))
//                {
//                    animator.SetBool("IdleBlock", false);
//                    animator.SetTrigger("Roll");
//                }
//                break;
//            case PlayerState.State.Block:
//                animator.SetBool("IdleBlock", true);
//                break;
//            case PlayerState.State.SuccessBlock:               
//                if (curAnimatorState.IsName("Idle Block") && !playingBlockAnimation)
//                    StartCoroutine(BlockAnimationRoutine());

//                // IdleBlock 파라미터가 true면 트리거로 Block 애니메이션을 재생하여도 바로 Idle Block 애니메이션으로 넘어가 버리는 것이 문제였다.
//                // 그래서 이 문제를 해결하기 위하여 Block 애니메이션이 재생 중일 땐 IdleBlock 파라미터 값을 false로 바꿔 Idle Block 애니메이션이 재생되지 못하도록 수정하였다.
//                if (curAnimatorState.IsName("Block"))
//                    animator.SetBool("IdleBlock", false);
//                break;
//            case PlayerState.State.WallSlide:
//                if (!curAnimatorState.IsName("WallSlide"))
//                {
//                    animator.SetBool("WallSlide", true);
//                    animator.SetBool("Grounded", false);
//                    animator.SetFloat("AirSpeedY", -1f);
//                }
//                break;
//            case PlayerState.State.Hurt:
//                if (!playingHurtAnimation)
//                    StartCoroutine(HurtAnimationRoutine());
//                break;
//            case PlayerState.State.Death:
//                if (!curAnimatorState.IsName("Death"))
//                {
//                    animator.SetTrigger("Death");
//                    animator.SetBool("noBlood", false);
//                }
//                break;
//        }
//    }

//    IEnumerator AttackAnimationRoutine()
//    {
//        playingAttackAnimation = true;

//        //if(_state.attackState == _state.AttackState.Attack1 && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack1"))
//        //    animator.SetTrigger("Attack1");
//        //else if(_state.attackState == _state.AttackState.Attack2 && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack2"))
//        //    animator.SetTrigger("Attack2");
//        //else if (_state.attackState == _state.AttackState.Attack3 && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack3"))
//        //    animator.SetTrigger("Attack3");
//        animator.SetTrigger("Attack1");

//        yield return attackDelay;

//        if (_state.attackState == PlayerState.AttackState.Attack2)
//        {
//            animator.SetTrigger("Attack2");
//            yield return attackDelay;
//        }

//        if (_state.attackState == PlayerState.AttackState.Attack3)
//        {
//            animator.SetTrigger("Attack3");
//            yield return attackDelay;
//        }

//        playingAttackAnimation = false;
//    }

//    IEnumerator HurtAnimationRoutine()
//    {
//        playingHurtAnimation = true;
//        animator.SetTrigger("Hurt");

//        yield return new WaitForSeconds(0.3f);

//        playingHurtAnimation = false;
//    }

//    IEnumerator BlockAnimationRoutine()
//    {
//        playingBlockAnimation = true;
//        animator.SetTrigger("Block");

//        yield return new WaitForSeconds(0.4f);
//        playingBlockAnimation = false;
//    }
//}

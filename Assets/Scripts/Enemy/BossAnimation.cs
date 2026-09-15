using UnityEngine;

public class BossAnimation : MonoBehaviour
{
    Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        animator.SetBool("IsGround", true);
    }

    public void PlayIdle()
    {
        animator.SetBool("IsGround", true);
        animator.SetBool("AttackEnd", true);
        animator.SetInteger("MoveValue", 0);
    }

    public void PlayMove()
    {
        animator.SetInteger("MoveValue", 1);
    }

    public void PlayDash()
    {
        animator.SetTrigger("Dash");
    }

    public void PlayOneAttack()
    {
        animator.SetTrigger("Attack");
        animator.SetBool("AttackEnd", false);
    }

    public void PlaySlideAttack()
    {
        animator.SetTrigger("Slide");
    }
}

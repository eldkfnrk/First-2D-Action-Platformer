using System.Collections;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void PlayIdle()
    {
        animator.SetBool("Grounded", true); 
        animator.SetFloat("AirSpeedY", 0f);
        animator.SetInteger("AnimState", 0);
    }

    public void PlayRun()
    {
        animator.SetInteger("AnimState", 1);
    }

    public void PlayJump()
    {
        animator.SetTrigger("Jump");
        animator.SetBool("Grounded", false);
        animator.SetBool("WallSlide", false);
        animator.SetFloat("AirSpeedY", 1f);
    }

    public void PlayFall()
    {
        animator.SetFloat("AirSpeedY", -1f);
        animator.SetInteger("AnimState", 0);
        animator.SetBool("Grounded", false);
    }

    public void PlayWallSlide()
    {
        animator.SetBool("WallSlide", true);
        animator.SetFloat("AirSpeedY", -1f);
    }

    public void PlayRoll()
    {
        animator.SetTrigger("Roll");
    }

    public void PlayAttack(int atkCount)
    {
        animator.SetInteger("AnimState", 0);
        string animationName = string.Format("Attack{0}", atkCount);
        animator.SetTrigger(animationName);
    }
}

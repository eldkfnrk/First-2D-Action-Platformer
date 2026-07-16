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
        animator.SetFloat("AirSpeedY", 0.1f);
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
    }

    public void PlayFall()
    {
        animator.SetFloat("AirSpeedY", -1f);
    }
}

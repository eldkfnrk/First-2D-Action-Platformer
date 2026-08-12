using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    Animator animator;
    [SerializeField] GameObject slideDustVFX;
    Animator slideDustAnim;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        slideDustAnim = GetComponentsInChildren<Animator>(true)[1];
        // 새로 알게된 사실 - 스프라이트 렌더러가 없다면 애니메이터가 스프라이트를 출력하지 못해서 애니메이션이 보이지 않는다.
    }

    public void ParameterReset()
    {
        animator.Play("Idle");
        animator.SetBool("IdleBlock", false);
        animator.SetBool("Grounded", true);
        animator.SetFloat("AirSpeedY", 0f);
        animator.SetInteger("AnimState", 0);
        animator.SetBool("WallSlide", false);
        animator.SetBool("noBlood", false);
    }

    public void PlayIdle()
    {
        animator.SetBool("Grounded", true); 
        animator.SetBool("IdleBlock", false); 
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

    public void PlayBlock()
    {
        animator.SetInteger("AnimState", 0);
        animator.SetBool("IdleBlock", true);
    }

    public void StopBlock()
    {
        animator.SetBool("IdleBlock", false);
    }

    public void PlaySuccessBlock()
    {
        animator.SetTrigger("Block");
    }

    public void PlayHit()
    {
        animator.SetBool("IdleBlock", false);
        animator.SetInteger("AnimState", 0);
        animator.SetTrigger("Hurt");
    }

    public void PlaySlideDust()
    {
        if (!slideDustVFX.activeSelf)
        {
            slideDustVFX.SetActive(true);
            slideDustVFX.GetComponent<SlideDustAnimation>().SlideDustReposition();
        }
    }

    public void SlideDustSpeedUp()
    {
        slideDustAnim.speed = 1f;
    }

    public void SlideDustSpeedDown()
    {
        slideDustAnim.speed = 0.5f;
    }

    public void StopSlideDust()
    {
        slideDustVFX.SetActive(false);
    }

    public void PlayDeath()
    {
        animator.SetTrigger("Death");
    }
}

using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    Animator animator;
    PlayerAction playerAction;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerAction = GetComponent<PlayerAction>();
        animator.SetBool("Grounded", true);
    }

    void Update()
    {
        switch (playerAction.playerState)
        {
            case PlayerAction.PlayerState.Idle:
                animator.SetInteger("AnimState", 0);
                animator.SetBool("Grounded", true);
                break;
            case PlayerAction.PlayerState.Run:
                animator.SetInteger("AnimState", 1);
                animator.SetBool("Grounded", true);
                break;
            case PlayerAction.PlayerState.Jump:
                animator.SetTrigger("Jump");
                animator.SetBool("Grounded", false);
                break;
            case PlayerAction.PlayerState.Fall:
                animator.SetFloat("AirSpeedY", -1f);
                break;
        }
    }
}

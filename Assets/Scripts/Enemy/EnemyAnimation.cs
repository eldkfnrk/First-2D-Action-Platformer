using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    // 규칙 : 아래의 배열에는 열거형 State 순서대로 값을 저장해 놓도록 하여야 한다.(나중에 추가되면 그때 인덱스를 전환해도 되니 일단 이 상태로 진행)
    // 만약 애니메이터가 하나라면 동작 쪽에서 알아서 애니메이션을 딱 한 번만 재생시키도록 설정한다.
    public RuntimeAnimatorController[] animators;
    Animator anim;

    void InitializeAnimator()
    {
        anim = GetComponent<Animator>();
    }

    // 오브젝트의 Awake, OnEnable 호출 시기가 다름에 따라 계속하여 Animator 컴포넌트가 초기화되지 않은 상황이 반복
    // 그걸 방지하기 위하여 따로 체크하고 이를 초기화하도록 설정
    // Idle과 Move에만 사용한 이유는 이 두 상태가 가장 처음에 호출될 것이기에 anim이 없는 상황에 적용될 수 있어서 그렇게 하였다.
    public void PlayIdle()
    {
        if (anim == null)
            InitializeAnimator();

        anim.runtimeAnimatorController = animators[0];
    }

    public void PlayMove()
    {
        if (anim == null)
            InitializeAnimator();

        anim.runtimeAnimatorController = animators[1];
    }

    public void PlayAttack()
    {
        anim.runtimeAnimatorController = animators[2];
    }

    public void PlayHit()
    {
        anim.runtimeAnimatorController = animators[3];
    }
}

using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    // 규칙 : 아래의 배열에는 열거형 State 순서대로 값을 저장해 놓도록 하여야 한다.(추후 딕셔너리를 사용하는 방식으로 바꿀 순 있지만 우선 이렇게 하도록 한다.)
    // 만약 애니메이터가 하나라면 동작 쪽에서 알아서 애니메이션을 딱 한 번만 재생시키도록 설정한다.
    public RuntimeAnimatorController[] animators;
    Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void PlayIdle()
    {
        if (anim == null)
            return;

        anim.runtimeAnimatorController = animators[0];
    }

    public void PlayMove()
    {
        if (anim == null)
            return;

        anim.runtimeAnimatorController = animators[1];
    }
}

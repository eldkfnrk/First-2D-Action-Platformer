using UnityEngine;

public class EnemyAction : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Run,
        Fall,
        Dash,
        DashAttack,
        Attack,
        Slide,
        Hurt,
        Croush,
        Jump,
        Death,
    }

    EnemyState state;

    private void Awake()
    {
        state = EnemyState.Idle;
    }

    // Update is called once per frame
    void Update()
    {
        // 보스전이 시작하기 전까지는 Idle
        // 보스전 시작 후 각 동작 수행
    }
}

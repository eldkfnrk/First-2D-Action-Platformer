using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum State
    {
        Idle,
        Move,
    }

    public State enemyState;
    protected EnemyFSM fsm;
    protected Rigidbody2D rigid;
    protected SpriteRenderer sprtieR;
    public EnemyRuntimeData variableData;
    public EnemyData constantData;
    public EnemyAnimation enemyAnimation;
    public float actionTimer;

    // 모든 적이 공통적으로 갖는 동작 - 이동, 타격, 피격, 사망

    // 이동
    public void Move()
    {
        rigid.linearVelocityX = constantData.moveSpeed * variableData.sightDirection;
        if (variableData.frontCheck.collider != null)
            ChangeDirection();
    }

    // 바라보는 방향 전환
    public void ChangeDirection()
    {
        variableData.sightDirection *= -1f;
        sprtieR.flipX = !sprtieR.flipX;
    }
}

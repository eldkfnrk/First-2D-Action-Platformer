using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum State
    {
        Idle,
        Move,
        Attack,
        Hit,
        Death,
    }

    public State enemyState;
    protected EnemyFSM fsm;
    protected Rigidbody2D rigid;
    protected SpriteRenderer sprtieR;
    public EnemyRuntimeData variableData;
    public EnemyData constantData;
    public EnemyAnimation enemyAnimation;
    public float actionTimer;

    public void WallFloorCheck()
    {
        variableData.floorCheckOrigin.x = transform.position.x + 0.2f * variableData.sightDirection;
        variableData.floorCheckOrigin.y = transform.position.y;
        variableData.frontCheck = Physics2D.Raycast(transform.position, Vector2.right * variableData.sightDirection, constantData.frontCheckDistance, constantData.groundLayer);
        variableData.floorCheck = Physics2D.Raycast(variableData.floorCheckOrigin, Vector2.down, constantData.floorCheckDistance, constantData.groundLayer);
    }

    // 플레이어 탐지
    public void DetectPlayer()
    {

    }

    // 모든 적이 공통적으로 갖는 동작 - 이동, 타격, 피격, 사망

    // 이동
    public void EnemyMove()
    {
        rigid.linearVelocityX = constantData.moveSpeed * variableData.sightDirection;
        if (variableData.frontCheck.collider != null || variableData.floorCheck.collider == null)
            ChangeDirection();
    }

    // 바라보는 방향 전환
    public void ChangeDirection()
    {
        variableData.sightDirection *= -1f;
        sprtieR.flipX = !sprtieR.flipX;
    }

    // 피격
    public void EnemyHit()
    {

    }

    // 사망
    public void EnemyDeath()
    {

    }
}

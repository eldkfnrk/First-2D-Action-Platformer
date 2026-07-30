using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    // 모든 적의 공통점
    // 적과 충돌 시 플레이어는 반드시 뒤로 넉백이 일어나며 데미지를 입는다.

    public enum State
    {
        Idle,
        Move,
        Chase,
        GoBack,
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

    public virtual void WallFloorCheck()
    {
        variableData.floorCheckOrigin.x = transform.position.x + 0.2f * variableData.sightDirection;
        variableData.floorCheckOrigin.y = transform.position.y;
        variableData.frontCheck = Physics2D.Raycast(transform.position, Vector2.right * variableData.sightDirection, constantData.frontCheckDistance, constantData.groundLayer);
        variableData.floorCheck = Physics2D.Raycast(variableData.floorCheckOrigin, Vector2.down, constantData.floorCheckDistance, constantData.groundLayer);
    }

    // 플레이어 탐지
    public bool DetectPlayer()
    {
        variableData.detectPlayerBoxPos.x = transform.position.x + variableData.sightDirection * constantData.detectPlayerBoxOffset.x;
        variableData.detectPlayerBoxPos.y = transform.position.y + constantData.detectPlayerBoxOffset.y;
        variableData.detectPlayer = Physics2D.BoxCast(variableData.detectPlayerBoxPos, constantData.detectPlayerBoxSize, 0f, Vector2.zero, 0f, constantData.playerLayer);
        return variableData.detectPlayer.collider != null;
    }

    // 바라보는 방향 전환
    public void ChangeDirection()
    {
        variableData.sightDirection *= -1f;
        sprtieR.flipX = !sprtieR.flipX;
    }

    protected void GetKnockBackDir()
    {
        variableData.knockbackDir = (transform.position - GameManager.instance.player.transform.position).normalized;
    }

    // 모든 적이 공통적으로 갖는 동작 - 이동, 멈춤, 타격, 피격, 사망

    // 일반 이동
    public void EnemyMove()
    {
        rigid.linearVelocityX = constantData.moveSpeed * variableData.sightDirection;
        if (variableData.frontCheck.collider != null || variableData.floorCheck.collider == null)
            ChangeDirection();
    }

    // 추적 이동
    public void EnemyChaseMove()
    {
        variableData.playerEnemyXDistance = GameManager.instance.player.transform.position.x - transform.position.x;
        variableData.moveDir = variableData.playerEnemyXDistance / Mathf.Abs(variableData.playerEnemyXDistance);
        variableData.playerEnemyXDistance = Mathf.Abs(variableData.playerEnemyXDistance);
        rigid.linearVelocityX = variableData.moveDir * constantData.moveSpeed;
        if (variableData.moveDir != variableData.sightDirection)
            ChangeDirection();
    }

    // 멈춤
    public void EnemyStop()
    {
        rigid.linearVelocityX = 0f;
    }

    // 피격
    public void EnemyHit()
    {
        StartCoroutine(HitRoutine());
    }

    // 피격 행동이 다른 적이 있을 수 있기에 가상 함수로 선언
    protected virtual IEnumerator HitRoutine()
    {
        // 피격 시 넉백 발생
        GetKnockBackDir();
        rigid.AddForce(variableData.knockbackDir * constantData.knockbackPower, ForceMode2D.Impulse);
        enemyAnimation.PlayIdle();  // 대부분의 적이 피격 애니메이션이 없어서 Idle 애니메이션 사용(피격이 있는 적은 따로 override로 수정)
        yield return new WaitForSeconds(0.25f);
        variableData.isHit = false;
    }

    // 사망
    public void EnemyDeath()
    {

    }
}

using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Scriptable Objects/PlayerData")]
public class PlayerData : ScriptableObject
{
    // 변하지 않는 플레이어의 값들을 저장

    public float maxHp;
    public float maxStamina;

    // 이동 관련 데이터
    public float moveSpeed;  // 이동 속도
    public float jumpPower;  // 점프 파워
    public float hitWallPower;  // 벽 점프 시 벽 반대로 튀는 파워
    public float fallSpeed;  // 떨어지는 속도
    public float rollSpeed;  // 구르는 속도

    // 레이어 마스크
    public LayerMask groundLayer;  // 땅에 해당하는 레이어
    public LayerMask enemyLayer;  // 적에 해당하는 레이어

    public float attackDurationTime;
    public float knockbackDurationTime;
    public float rollDurationTime;
    public float cantInputDurationTime;

    public WaitForSeconds attackDuration;
    public WaitForSeconds knockbackDuration;
    public WaitForSeconds rollDuration;
    public WaitForSeconds cantInputDuration;

    public float groundCheckDistance;  // 땅을 체크할 거리
    public Vector2 wallCheckBoxSize;  // 벽 탐지 범위
    public float wallCheckDistance;  // 벽을 체크할 거리
    public float lowWallCheckOffset;  // 위 쪽에서 벽을 체크할 boxcast의 y축 오프셋(x축 이동은 없을테니 y축 값만 보유하도록 설정)

    public float knockbackXPower;  // x축 넉백 파워
    public float knockbackYPower;  // y축 넉백 파워

    public float blockKnockbackPower;

    public Vector2 attackBoxSize;  // 공격 범위
    public float blockBoxXPos;
}

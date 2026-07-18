using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Scriptable Objects/PlayerData")]
public class PlayerData : ScriptableObject
{
    // 변하지 않는 플레이어의 값들을 저장

    // 이동 관련 데이터
    public float moveSpeed;  // 이동 속도
    public float jumpPower;  // 점프 파워
    public float hitWallPower;  // 벽 점프 시 벽 반대로 튀는 파워
    public float fallSpeed;  // 떨어지는 속도

    // 레이어 마스크
    public LayerMask groundLayer;  // 땅에 해당하는 레이어
    public float groundCheckDistance;  // 땅을 체크할 거리
    public float wallCheckDistance;  // 벽을 체크할 거리
}

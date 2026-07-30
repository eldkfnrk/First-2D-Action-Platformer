using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public float moveSpeed;

    public LayerMask groundLayer;
    public LayerMask playerLayer;

    public Vector2 detectPlayerBoxOffset;  // 플레이어를 탐지하는 박스의 중앙 위치를 지정하는 변수
    public Vector2 detectPlayerBoxSize;  // 플레이어를 탐지하는 박스의 크기를 저장하는 변수

    public float maxDistance;  // 플레이어와 적 간 최대 거리(이 거리를 넘으면 더 이상 추적하지 않는다.)

    public float knockbackPower;

    public float frontCheckDistance;
    public float floorCheckDistance;
}

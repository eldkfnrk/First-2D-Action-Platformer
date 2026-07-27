using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public float moveSpeed;

    public LayerMask groundLayer;
    public LayerMask playerLayer;

    public Vector2 detectRange;

    public float moveMaxRange;  // 적이 스폰된 위치에서 최대로 떨어질 수 있는 거리
    public float maxDistance;  // 플레이어와 적 간 최대 거리(이 거리를 넘으면 더 이상 추적하지 않는다.)

    public float frontCheckDistance;
    public float floorCheckDistance;
}

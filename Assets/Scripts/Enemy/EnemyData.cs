using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public float moveSpeed;

    public LayerMask groundLayer;

    public float frontCheckDistance;
    public float floorCheckDistance;
}

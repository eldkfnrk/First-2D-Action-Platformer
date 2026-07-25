using UnityEngine;

public class EnemyRuntimeData : MonoBehaviour
{
    public float sightDirection;

    public Vector3 spawnLoc;

    public RaycastHit2D frontCheck;
    public RaycastHit2D floorCheck;

    public Vector2 floorCheckOrigin;  // 떨어지는 것을 방지하기 위해 바닥 점검을 바라보는 방향으로 조금 더 전진시키도록 하기 위한 변수

    public bool isHit;
}

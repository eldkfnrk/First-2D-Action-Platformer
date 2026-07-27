using UnityEngine;

public class EnemyRuntimeData : MonoBehaviour
{
    public float sightDirection;

    public Vector3 spawnLoc;
    public bool esacpeRange;

    public RaycastHit2D frontCheck;
    public RaycastHit2D floorCheck;
    public RaycastHit2D detectPlayer;

    public Vector2 floorCheckOrigin;  // 떨어지는 것을 방지하기 위해 바닥 점검을 바라보는 방향으로 조금 더 전진시키도록 하기 위한 변수

    public float moveDir;
    public float playerEnemyXDistance;  // 플레이어와 적 캐릭터 간 X축 거리(플레이어가 적 캐릭터로부터 X축으로 일정 거리 벌어지면 더 이상 추적하는 것을 그만두기 위한 값)
    public bool goBack;

    public bool isHit;
}

using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    private Vector2 respawnPoint;

    // 콜라이더 크기는 맵 중 빈 공간을 채울 정도의 크기로 설정
    // 점프 맵의 경우 장애물 위에서부터 시작해서 바닥을 지나쳐 천장까지 가도록 설정
    // 각 콜라이더의 offset은 오브젝트의 위치에 맞춰서 설정
    // 오브젝트의 위치가 리스폰 지점이 될 예정이니 오브젝트에 맞게 콜라이더 크기 및 offset 설정만 하면 된다.

    private void Awake()
    {
        respawnPoint = transform.position;
    }

    public Vector2 RespawnPoint
    {
        get
        {
            return respawnPoint;
        }
    }
}

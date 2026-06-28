using UnityEngine;

public class Door : MonoBehaviour
{
    public enum DoorState
    {
        Close,
        Open,
        Lock,
    }

    public string nextSceneName;  // 문들은 보통 다음 씬으로 넘어가는 역할을 하기 때문에 해당 문이 상호 작용 시 이동할 씬의 이름을 저장

    public DoorState state;

    public Sprite OpenDoor;
    public Sprite CloseDoor;

    SpriteRenderer spriteR;

    private void Awake()
    {
        spriteR = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 열림 버튼 클릭 UI 표시
            // 상호 작용 가능 여부 가능으로 변환
        }
    }

    public void OpenDoorFunc()
    {
        spriteR.sprite = OpenDoor;
        // 문 열릴 때 사운드 추가
        // 다음 씬으로 이동
    }

    public void CloseDoorFunc()
    {
        spriteR.sprite = CloseDoor;
        // 문 닫힐 때 사운드 추가
    }
}

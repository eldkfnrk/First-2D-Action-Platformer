using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // 구역을 저장하는 열거형
    public enum Area
    {
        // 씬 순서와 구역을 일치시키면 현재 플레이어가 있는 구역을 저장할 수 있을 것으로 기대
        NormalStage,
        BossStage,
    }

    public Area area;

    public static GameManager instance;
    public GameObject player;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }

            int curSceneNum = SceneManager.GetActiveScene().buildIndex;

        switch (curSceneNum)
        {
            case 0:
                area = Area.NormalStage;
                break;
            case 1:
                area = Area.BossStage;
                break;
        }
    }

    // 이번에 보스전에 몰두하여 FSM을 반드시 구현해야 한다라는 생각에 매몰되었고 그로 인해 구현은 안 되고 시간만 소비하는 날도 있었고 높은 난이도에 시작 저항감이 상승하기만 하였다.
    // 그래서 순서를 바꿔서 여러 장애물과 나를 탐지하고 따라오는 적을 만들어보는 과정을 먼저 하면서 2d 메트로배니아라는 장르의 게임을 제대로 만들어 보는 시간을 가져보려 한다.
    // 그러기 위해 우선 게임 디자인에 대해서 더 잘 알아야 하고 게임을 하다가 이게 여기에 왜 등장해야 하는지 등을 알아보기 위해 유명 메트로배니아 게임인 할로우 나이트와 오리 시리즈를 플레이 해보면서
    // 레벨 디자인, 전투 디자인, 플레이어에게 기술을 알려주는 여러 게임적 디자인을 배워보는 시간을 함께 가져가보면 좋겠다 생각하였고 이를 바탕으로 하나씩 나의 게임에도 살을 붙여나가보면 좋을 거 같다.
    // 레벨 디자인은 전체적으로 씬을 분할해 볼 예정이고 언제 왜 이걸 습득해야 하는지 등을 고민해보는 시간도 함께 가져 볼 예정이다.

    // 만들어 볼 순서
    // 씬 간 이동 - 순찰하는 적이 있는 씬(발각 시 달려드는 적) - 전투 씬(미니 보스) - 체크 포인트 - 능력 획득 - 아이템 획득 - 보스 씬

    void Update()
    {
        
    }
}

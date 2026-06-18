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

    void Update()
    {
        
    }
}

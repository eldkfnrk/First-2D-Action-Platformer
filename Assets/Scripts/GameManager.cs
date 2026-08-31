using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // 구역을 저장하는 열거형
    public enum Area
    {
        // 씬 순서와 구역을 일치시키면 현재 플레이어가 있는 구역을 저장할 수 있을 것으로 기대
        TestScene,
        CentralArea,
    }

    public SpawnSystem spawnSystem;
    SpawnPoint.PointID playerSpawnPointId;

    public Area area;
    public string curSceneName;

    public static GameManager instance;
    public GameObject player;
    public GameObject playerPrefab;
    public PlayerStatus playerStatus;
    public GameObject mainCamera;
    public GameObject mainCameraPrefab;
    public GameObject playerCamera;

    // 플레이어 사망을 알리는 이벤트
    public event System.Action playerDeathEvent;

    public void NotifyPlayerDeath()
    {
        playerDeathEvent?.Invoke();  // 이 Invoke 함수는 호출 시 이벤트에 등록된 모든 함수들을 실행한다는 의미의 함수이다.
    }

    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        mainCamera = Instantiate(mainCameraPrefab);
        DontDestroyOnLoad(mainCamera);
        FindPlayerCamera();

        Vector3 playerPos = Vector3.zero;
        GameObject existPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existPlayer != null)
        {
            playerPos = existPlayer.transform.position;
            Destroy(existPlayer);
        }
        player = Instantiate(playerPrefab);
        player.transform.position = playerPos;
        DontDestroyOnLoad(player);

        curSceneName = SceneManager.GetActiveScene().name;
        area = (Area)Enum.Parse(typeof(Area), curSceneName);
        // 저장 데이터가 있다면 해당 위치로 생성하고 지금 당장은 원래 플레이어가 있던 위치에 생성되도록 설정
        playerSpawnPointId = SpawnPoint.PointID.None;
        SceneManager.sceneLoaded += OnSceneLoad;
    }

    public void AttackEnemies(Collider2D[] attackedEnemies, Vector2 attackBoxPos)
    {
        EnemyRuntimeData enemyVariableData;

        // 인자로 받은 적들에게 자신의 피격 사실을 전달
        foreach(Collider2D attackedEnemy in attackedEnemies)
        {
            if (attackedEnemy == null)
                break;
            enemyVariableData = attackedEnemy.gameObject.GetComponent<EnemyRuntimeData>();
            enemyVariableData.isHit = true;
            enemyVariableData.knockbackDir = ((Vector2)enemyVariableData.transform.position - attackBoxPos).normalized;
        }
    }

    public void SceneChange(SpawnPoint startPoint)
    {
        playerSpawnPointId = startPoint.targetId;
        string targetPointId = playerSpawnPointId.ToString();
        int index = targetPointId.IndexOf('_');

        string nextSceneName = index == -1 ? targetPointId : targetPointId.Substring(0, index);

        // LoadScene - 이 함수가 반환되면 기존 씬은 메모리에서 삭제되고 이동하고자 하는 씬의 Awake, OnEnable, Start이 완료된다.
        // sceneLoaded 이벤트는 Awake-OnEnable이 끝나고 Start 하기 전에 호출된다.
        // 고민해 볼 해결 방안
        // 1. 씬 전환을 알리는 bool 변수를 하나 두어서 이를 통해 관리한다.
        // 2. 입력 값을 받고 나서 씬 전환 중일 땐 호출 불가 씬 종료 후에는 씬 전환 완료를 알리도록 하는 방법을 통해 관리한다.
        SceneManager.LoadScene(nextSceneName);
        // 페이드 인 효과 주기(코루틴 활용)
    }

    void CreateSpawnSystem()
    {
        // 이 오브젝트는 원래 씬 내에 없기 때문에 씬 전환 시 자동 삭제가 되기 때문에 매 씬 전환마다 생성해주어야 한다.
        GameObject spawnSystemObj = new GameObject();
        spawnSystemObj.name = "SpawnSystem";
        spawnSystemObj.transform.position = Vector3.zero;
        spawnSystem = spawnSystemObj.AddComponent<SpawnSystem>();

        spawnSystem.FindEnemySpawnPointsCollecter();

        // 이동한 씬에 있는 모든 스폰 포인트를 스폰 시스템에 저장해 두는 작업
        SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        foreach(SpawnPoint spawnPoint in spawnPoints)
        {
            spawnSystem.SaveSpawnPoint(spawnPoint);
        }
    }

    void PlayerTransformShift()
    {
        if (playerSpawnPointId == SpawnPoint.PointID.None)
            return;

        GameObject[] playerObjs = GameObject.FindGameObjectsWithTag("Player");
        if (playerObjs.Length > 1)
        {
            foreach (GameObject playerObj in playerObjs)
            {
                if (player != playerObj)
                {
                    Destroy(playerObj);
                    break;
                }
            }
        }
        player.transform.position = spawnSystem.TakeSpawnPoint(playerSpawnPointId);
    }

    void FindPlayerCamera()
    {
        playerCamera = GameObject.FindGameObjectWithTag("PlayerCamera");
    }

    void CameraTargetPlayer()
    {
        CinemachineCamera cinemachine = playerCamera.GetComponent<CinemachineCamera>();
        cinemachine.Follow = player.transform;
        cinemachine.LookAt = player.transform;
    }

    void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        curSceneName = scene.name;
        area = (Area)Enum.Parse(typeof(Area), curSceneName);
        CreateSpawnSystem();
        PlayerTransformShift();
        FindPlayerCamera();
        CameraTargetPlayer();
        spawnSystem.EnemyRespawn();
        Debug.Log("씬 변환 완료. 현재 씬 : " + curSceneName);
    }

    // 현재 아이디어
    // 게임 시작 전 메인 페이지에 해당하는 씬이 존재하도록 설정
    // 게임 시작 시 게임 매니저를 생성함과 동시에 DontDestroyOnLoad로 설정
    // 게임 매니저가 플레이어와 메인 카메라와 같은 DontDestroyOnLoad를 생성 및 설정
    // 게임은 데이터를 저장하는데 현재 씬과 플레이어의 위치, 플레이어 데이터, 게임 진행에 관련된 데이터 등을 저장
    // 게임 매니저가 저장된 데이터를 적용 및 오브젝트 생성, 위치 선정 등을 수행

    // 현재 목표(이 포폴에 추가했으면 하는 요소들)
    // 간단한 3타입의 적 생성
    // 플레이어의 자연스러운 상태 전환
    // 1개의 보스 생성
    // 대화 시스템
    // 간단한 컷씬 1개
    // 저장과 불러오기 기능
    // 숙련도 시스템
    // 상점 거래

    // 이 포폴로 얻고자 하는 것
    // FSM, Tree 등과 같은 자료구조와 알고리즘 적용 연습
    // JSON이나 플레이어 프립과 같은 저장 시스템과 불러오기 시스템 습득
    // 대화 시스템을 통한 데이터 연결 관련 기술 습득
    // 간단한 컷씬을 통해 카메라와 애니메이션 관련 기술 습득
}

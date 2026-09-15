using System;
using System.Collections;
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

    // 플레이어가 장애물 충돌 시 동작
    // 할로우 나이트를 인용하여 플레이어가 바닥 장애물의 충돌하면 가까운 위치로 리스폰 되도록 설정
    // 이때 리스폰 위치 설정은 빈 오브젝트의 IsTrigger를 활성화 시킨 콜라이더를 두고 이를 체크 포인트로 설정하여 플레이어가 이 체크 포인트와 충돌하면 해당 체크 포인트가 저장하고 있는 리스폰 좌표를 저장(runtime 데이터로 리스폰 위치를 저장)
    // 초기 리스폰 위치는 플레이어가 생성된 위치이고 체크 포인트는 맵을 어떻게 통과하든 충돌이 되도록 좌우로는 얇고 상하로는 맵을 가득 차도록 배치시켜야 문제가 발생하지 않는다.
    // 체크 포인트를 지나면 저장되는 리스폰 지점의 좌표는 각 체크 포인트에 저장된 좌표를 플레이어가 충돌 시 불러오는 것으로 설정할 예정

    public void RespawnPlayer(Vector2 respawnPoint)
    {
        player.transform.position = respawnPoint;
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
        cinemachine.Lens.OrthographicSize = 12f;  // 시야각을 넓히는 방법은 시네머신의 Lens 중 OrthographicSize 값을 키우는 것이다.(씬에서는 FOV라 되어 있는데 플레이 모드는 OrthographicSize라 되어 있어서 알아내느라 시간이 걸렸다. 왜 이렇게 되는지 이해가 되지 않는다.)
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
}

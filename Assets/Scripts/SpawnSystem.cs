using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class SpawnSystem : MonoBehaviour
{
    // 게임 매니저 - 씬 이동 동작을 수행
    // 씬 이동 후 플레이어 배치 로직 수행
    // 각 포인트마다 id와 씬을 이동했을 때 해당 씬에 어느 포인트로 갈 건지 저장
    // 각 포인트는 씬 로드 시 스폰 시스템에 자신의 정보를 전달
    // 이는 이 클래스 내에서 저장 관련 동작을 수행할 함수 필요

    // 1. 씬에 있는 다른 씬 이동 포인트(플레이어 상호작용 포인트)
    // 2. 씬에 있는 적 스폰 위치에 해당하는 스폰 포인트(이름에 맞게 적을 스폰)

    // 보스전이나 일반적인 씬을 제외한 모든 씬에는 일반 몬스터들의 스폰 위치를 표현하기 위한 적 스폰 포인트 모음 오브젝트를 둘 예정이기에 2번 방법을 사용할 수 있다.
    // 2번 스폰 포인트는 SpawnPoint 스크립트가 아닌 이름을 사용하여 구현

    public SerializedDictionary<SpawnPoint.PointID, SpawnPoint> playerSpawnPoints;
    public GameObject enemySpawnPointCollecter;

    private void Awake()
    {
        playerSpawnPoints = new SerializedDictionary<SpawnPoint.PointID, SpawnPoint>();
    }

    public void FindEnemySpawnPointsCollecter()
    {
        enemySpawnPointCollecter = GameObject.Find("EnemySpawnPoints");
        //Debug.Log(enemySpawnPointCollecter);
    }

    public void EnemyRespawn()
    {
        if (enemySpawnPointCollecter == null)
            return;

        Transform[] enemySpawnPoints = enemySpawnPointCollecter.GetComponentsInChildren<Transform>();
        GameObject enemyObject;
        for (int i = 1; i < enemySpawnPoints.Length; i++)
        {
            string objName = enemySpawnPoints[i].name;
            int index = objName.IndexOf('_');

            string enemyType = index == -1 ? objName : objName.Substring(0, index);
            string enemyPrefabPath = string.Format("Assets/Prefabs/{0}.prefab", enemyType);

            // 프리팹 호출에 에러가 존재 해결 필요 
            // 프리팹 경로 문제인지 애초에 리소스를 불러오는 것에서 생긴 문제인지 파악 불가
            // 일단 Resources.Load를 통해 오브젝트를 못 불러오는 것을 확인 -> 문제 이유 파악 실패(Resources 폴더 밑에 있어야 가능한데 해당 폴더를 안 만들어서인 것으로 추정)
            // AssetDatabase.LoadAssetAtPath를 이용하여 해결
            // 호출할 에셋의 경로를 통해 해당 에셋을 가져오는 함수로 가져오는 에셋의 타입에 맞춘 형 변환이 필요하며 해당 에셋의 타입이 무엇인지도 인자로 전달하여야 한다.
            enemyObject = (GameObject)AssetDatabase.LoadAssetAtPath(enemyPrefabPath, typeof(GameObject));
            Instantiate(enemyObject, enemySpawnPoints[i].position, Quaternion.identity);
        }
    }

    // 스폰 포인트를 저장하는 함수
    public void SaveSpawnPoint(SpawnPoint point)
    {
        playerSpawnPoints.Add(point.id, point);
    }

    public Vector3 TakeSpawnPoint(SpawnPoint.PointID pointId)
    {
        return playerSpawnPoints[pointId].transform.position;
    }
}

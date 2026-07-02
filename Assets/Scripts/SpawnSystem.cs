using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SpawnSystem : MonoBehaviour
{
    // 게임 매니저 - 씬 이동 동작을 수행
    // 씬 이동 후 플레이어 배치 로직 수행
    // 각 포인트마다 id와 씬을 이동했을 때 해당 씬에 어느 포인트로 갈 건지 저장
    // 각 포인트는 씬 로드 시 스폰 시스템에 자신의 정보를 전달
    // 이는 이 클래스 내에서 저장 관련 동작을 수행할 함수 필요

    public SerializedDictionary<SpawnPoint.PointID, SpawnPoint> spawnPoints;

    private void Awake()
    {
        spawnPoints = new SerializedDictionary<SpawnPoint.PointID, SpawnPoint>();
    }

    // 스폰 포인트를 저장하는 함수
    public void SaveSpawnPoint(SpawnPoint point)
    {
        spawnPoints.Add(point.id, point);
    }

    public Vector3 TakeSpawnPoint(SpawnPoint.PointID pointId)
    {
        return spawnPoints[pointId].transform.position;
    }
}

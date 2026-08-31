using System.Collections;
using UnityEngine;

public class EnemyB : Enemy
{
    // 일정 범위 내에서 움직이다가 플레이어를 탐지하면 플레이어를 향해 달려드는 몬스터
    // "이동 - 멈춤 - 이동" 혹은 "이동 - 방향 전환 - 이동" 같이 정해진 범위 내에서 이동과 정지, 방향 전환 등을 수행
    // 그러다 플레이어를 탐지하면 플레이어를 향해 돌진
    // 이때 일정 거리 이상으로 떨어지면 더 이상 따라오지 않고 다시 원래 있던 곳으로 돌아가서 동일한 행동 수행
    // 탐지 범위는 몬스터의 앞으로 일정 거리의 범위, 뒤로는 가까이 오면 탐지할 수 있게 앞보다는 적은 범위
    // 따라가지 않는 거리는 플레이어와 몬스터 사이의 x축 거리를 사용할 것이고 이 거리는 몬스터의 탐지 범위보다 더 길게 설정할 예정

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.red;
        //Gizmos.DrawWireCube(variableData.detectPlayerBoxPos, variableData.detectPlayerBoxSize);
        Gizmos.color = Color.darkGray;
        Gizmos.DrawRay(transform.position, Vector2.right * variableData.sightDirection * constantData.frontCheckDistance);
        Gizmos.DrawRay(variableData.floorCheckOrigin, Vector2.down * constantData.floorCheckDistance);
    }
}

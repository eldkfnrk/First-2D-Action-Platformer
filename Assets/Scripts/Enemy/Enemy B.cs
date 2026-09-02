using System.Collections;
using UnityEngine;

public class EnemyB : Enemy
{
    // 플레이어 탐지 방법 설정 
    // A는 플레이어 탐지 X
    // B, C 모두 자신만의 탐지 영역 보유
    // B는 지면에서 가끔 움직이기도 하기 때문에 적 오브젝트를 따라다니도록 설정
    // C는 공중에서 가만히 있다가 플레이어 탐지 시 탐지 범위를 넓게 변경하고 오브젝트를 따라다니도록 설정
    // C는 공중에서 영역 침범 확인은 overlapbox로 하고 이동 시에는 boxcast로 진행

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(variableData.detectPlayerBoxPos, constantData.detectPlayerBoxSize);
        Gizmos.color = Color.darkGray;
        Gizmos.DrawRay(transform.position, Vector2.right * variableData.sightDirection * constantData.frontCheckDistance);
        Gizmos.DrawRay(variableData.floorCheckOrigin, Vector2.down * constantData.floorCheckDistance);
    }
}

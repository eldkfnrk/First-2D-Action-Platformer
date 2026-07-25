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
    private void Awake()
    {
        fsm = GetComponent<EnemyFSM>();
        rigid = GetComponent<Rigidbody2D>();
        sprtieR = GetComponent<SpriteRenderer>();
        variableData = GetComponent<EnemyRuntimeData>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        variableData.spawnLoc = transform.position;

        variableData.sightDirection = sprtieR.flipX ? 1f : -1f;
    }

    private void Start()
    {
        fsm.ChangeState(State.Move);
    }

    private void FixedUpdate()
    {
        WallFloorCheck();
    }
}
//    public enum ActionState
//    {
//        Idle,  // 쫓지 않는 상태일 때 기본 동작 중 가만히 있는 상태
//        Move,  // 쫓지 않는 상태일 때 기본 동작 중 움직이는 상태
//        Chase,  // 플레이어를 쫓는 상태
//        GoBack,  // 원래 있던 곳으로 돌아가는 상태(이 상태에서 플레이어를 다시 탐지하면 Chase 상태로 전환)
//    }

//    public ActionState state;

//    public float moveSpeed;
//    public float frontCheckDistance;
//    public float floorCheckDistance;
//    public LayerMask floorLayer;
//    public LayerMask playerLayer;
//    float direction;
//    RaycastHit2D frontCheck;
//    RaycastHit2D floorCheck;

//    public float offsetYValue;
//    Vector2 detectBoxCenter;
//    public Vector2 detectBoxSize;
//    RaycastHit2D detectPlayer;

//    public float actionTime;  // 행동을 한 시간을 저장
//    public float changActionTime;  // 행동을 전환하는 시간(actionTime이 이 변수의 저장 값보다 같거나 커지면 행동을 전환)
//    public float farawayTime;  // Chase 상태에서 몬스터가 더 이상 플레이어를 향해 이동 불가한 위치에 있을 때 이 시간이 지나면 GoBack 상태로 전환
//    public float goBackTime;  // farawayTime이 이 변수의 값보다 커지면 GoBack 상태로 전환하기 위한 변수

//    public Vector3 originPosition;  // 몬스터가 원래 있던 좌표를 저장하는 변수(몬스터가 리스폰된 위치를 저장)
//    public float moveRange;  // 몬스터가 기본 상태일 때 이동 가능한 거리(originPosition에서 이 변수의 값만큼 +-의 범위 내에서만 이동 가능)

//    Vector2 playerDirection;  // 몬스터가 바라보는 플레이어의 방향
//    public float maxDistance;  // 몬스터가 플레이어를 쫓는 최대 거리(이 거리 이상으로 멀어지면 다시 돌아가도록 설정)

//    bool canForward;  // 전진이 가능한지를 저장

//    bool playerCollision;

//    // 넉백을 위한 딜레이 시간
//    public float knockbackTime;
//    WaitForSeconds knockbackDelay;

//    public RuntimeAnimatorController[] enemyAnimController;  // 0번 인덱스는 Idle 애니메이터를 1번 인덱스는 Move 애니메이터를 갖는 것으로 설정(추후에 알아보기 쉽도록 설정할 방법 모색)
//    Animator animator;
//    Rigidbody2D rigid;
//    SpriteRenderer spriteR;

//    private void Awake()
//    {
//        animator = GetComponent<Animator>();
//        rigid = GetComponent<Rigidbody2D>();
//        spriteR = GetComponent<SpriteRenderer>();

//        knockbackDelay = new WaitForSeconds(knockbackTime);

//        originPosition = transform.position;
//        canForward = true;
        
//        state = Random.Range(0, 2) == 0 ? ActionState.Idle : ActionState.Move;  // 0이면 Idle로 1이면 Move로 시작
//        animator.runtimeAnimatorController = enemyAnimController[(int)state];

//        detectBoxCenter.y = transform.position.y + offsetYValue;

//        direction = Random.Range(0, 1) == 0 ? -1f : 1f;
//        spriteR.flipX = direction == 1f ? true : false;
//    }

//    private void FixedUpdate()
//    {
//        detectBoxCenter.x = transform.position.x + direction * 2f;

//        // Chase 상태에서 앞이 벽이거나 낭떠러지일 경우 -> 상태는 Chase로 두고 가만히 있다가 일정 시간이 지나면 GoBack 상태가 되도록 설정
//        frontCheck = Physics2D.Raycast(transform.position, Vector2.right * direction, frontCheckDistance, floorLayer);
//        floorCheck = Physics2D.Raycast(transform.position, Vector2.down, floorCheckDistance, floorLayer);
//        detectPlayer = Physics2D.BoxCast(detectBoxCenter, detectBoxSize, 0f, Vector2.right, 0f, playerLayer);

//        if (frontCheck.collider != null || floorCheck.collider == null)
//            canForward = false;

//        switch (state)
//        {
//            case ActionState.Idle:
//                rigid.linearVelocityX = 0f;
//                ChangeAnimController(enemyAnimController[0]);
//                actionTime += Time.fixedDeltaTime;
//                ChangeChaseState();
//                break;
//            case ActionState.Move:
//                ChangeAnimController(enemyAnimController[1]);
//                if(transform.position.x > originPosition.x + moveRange || transform.position.x < originPosition.x - moveRange)
//                {
//                    direction *= -1f;
//                    spriteR.flipX = !spriteR.flipX;
//                }

//                rigid.linearVelocityX = moveSpeed * direction;
//                ChangeChaseState();
//                break;
//            case ActionState.Chase:
//                ChangeAnimController(enemyAnimController[1]);
//                playerDirection = GameManager.instance.player.transform.position - transform.position;

//                // Mathf.Sign 함수는 Vector2의 x 혹은 y의 +-여부를 파악(+면 1f를 -면 -1f를 반환)
//                if (Mathf.Sign(playerDirection.x) != direction)
//                    spriteR.flipX = !spriteR.flipX;

//                direction = playerDirection.x > 0f ? 1f : -1f;

//                // 플레이어와 몬스터의 x축 거리가 maxDistance보다 클 경우 멀리 떨어진 것으로 판단하여 Chase 상태를 종료하고 원래 있던 자리로 돌아가도록 한다.
//                // Mathf.Abs 함수는 float의 절대 값을 반환하는 함수
//                if (Mathf.Abs(playerDirection.x) > maxDistance)
//                {
//                    CantChaseState();
//                }
//                // 플레이어가 점프 최대 높이보다 높게 올라간 경우도 멀리 떨어진 것으로 간주(플레이어 최대 점프 높이와 몬스터의 기본 높이 차이 -> 약 5.5 -> 너무 차이가 적으면 오류가 생길 수 있으니 더 큰 값을 사용)
//                else if (Mathf.Abs(GameManager.instance.player.transform.position.y - transform.position.y) > 6f || !canForward)  
//                {
//                    CantChaseState();
//                }
//                else
//                {
//                    if (playerCollision)
//                        rigid.linearVelocityX = 0f;
//                    else
//                        rigid.linearVelocityX = moveSpeed * direction;
//                    farawayTime = 0f;
//                }

//                if(farawayTime > goBackTime)
//                {
//                    state = ActionState.GoBack;
//                    farawayTime = 0f;
//                    spriteR.flipX = !spriteR.flipX;
//                    canForward = true;
//                }
//                break;
//            case ActionState.GoBack:
//                ChangeAnimController(enemyAnimController[1]);
//                direction = originPosition.x - transform.position.x > 0f ? 1f : -1f;
//                spriteR.flipX = originPosition.x - transform.position.x < 0f ? false : true;

//                rigid.linearVelocityX = moveSpeed * direction;

//                // Mathf.Round 함수는 반올림 함수로 인자 값을 소수점이 없을 때까지 반올림 한 값을 반환한다.
//                float originPosX = Mathf.Round(originPosition.x * 10f) / 10f;  // 소수점 1의 자리 수까지 반올림한 값을 획득하는 수식(소수점이 없는 부분까지 반올림을 하니까 소수점 1의 자리 수를 올려놓고 다시 나누어서 값을 획득하는 원리이다.)
//                float transPosX = Mathf.Round(transform.position.x * 10f) / 10f;

//                if (Mathf.Approximately(originPosX, transPosX))
//                {
//                    state = ActionState.Idle;
//                    actionTime = 0f;
//                }

//                ChangeChaseState();  // 돌아가다가도 플레이어를 탐지하면 다시 플레이어를 쫓는 상태로 전환되도록 수정
//                break;
//        }

//        if (actionTime > changActionTime)
//        {
//            int nextAction = Random.Range(0, 11);

//            if (nextAction < 5)
//                state = ActionState.Idle;
//            else
//                state = ActionState.Move;

//            actionTime = 0f;
//        }
//    }

//    private void OnCollisionEnter2D(Collision2D collision)
//    {
//        if (collision.collider.CompareTag("Player"))
//            StartCoroutine(PlayerCollisionRoutine());
//    }

//    IEnumerator PlayerCollisionRoutine()
//    {
//        playerCollision = true;
//        yield return new WaitForSeconds(0.4f);
//        playerCollision = false;
//    }

//    void CantChaseState()
//    {
//        rigid.linearVelocityX = 0f;
//        ChangeAnimController(enemyAnimController[0]);
//        farawayTime += Time.fixedDeltaTime;
//    }

//    void ChangeAnimController(RuntimeAnimatorController target)
//    {
//        if(animator.runtimeAnimatorController != target)
//            animator.runtimeAnimatorController = target;
//    }

//    void ChangeChaseState()
//    {
//        if (detectPlayer.collider != null)
//        {
//            state = ActionState.Chase;
//            actionTime = 0f;
//        }
//    }

//    // 임시 피격 함수
//    public void Hit()
//    {
//        Debug.Log(gameObject.name + " hit");
//    }

//    private void OnDrawGizmos()
//    {
//        Gizmos.color = Color.green;
//        Gizmos.DrawWireCube(detectBoxCenter, detectBoxSize);
//    }
//}

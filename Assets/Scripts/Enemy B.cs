using UnityEngine;

public class EnemyB : MonoBehaviour
{
    // 일정 범위 내에서 움직이다가 플레이어를 탐지하면 플레이어를 향해 달려드는 몬스터
    // "이동 - 멈춤 - 이동" 혹은 "이동 - 방향 전환 - 이동" 같이 정해진 범위 내에서 이동과 정지, 방향 전환 등을 수행
    // 그러다 플레이어를 탐지하면 플레이어를 향해 돌진
    // 이때 일정 거리 이상으로 떨어지면 더 이상 따라오지 않고 다시 원래 있던 곳으로 돌아가서 동일한 행동 수행
    // 탐지 범위는 몬스터의 앞으로 일정 거리의 범위, 뒤로는 가까이 오면 탐지할 수 있게 앞보다는 적은 범위
    // 따라가지 않는 거리는 플레이어와 몬스터 사이의 x축 거리를 사용할 것이고 이 거리는 몬스터의 탐지 범위보다 더 길게 설정할 예정

    public enum ActionState
    {
        Idle,  // 쫓지 않는 상태일 때 기본 동작 중 가만히 있는 상태
        Move,  // 쫓지 않는 상태일 때 기본 동작 중 움직이는 상태
        Chase,  // 플레이어를 쫓는 상태
        GoBack,  // 원래 있던 곳으로 돌아가는 상태(이 상태에서 플레이어를 다시 탐지하면 Chase 상태로 전환)
    }

    public ActionState state;

    public float moveSpeed;
    public float frontCheckDistance;
    public float floorCheckDistance;
    public LayerMask floorLayer;
    public LayerMask playerLayer;
    float direction;
    RaycastHit2D frontCheck;
    RaycastHit2D floorCheck;

    Vector2 detectBoxCenter;
    public Vector2 detectBoxSize;
    RaycastHit2D detectPlayer;

    public float actionTime;  // 행동을 한 시간을 저장
    public float changActionTime;  // 행동을 전환하는 시간(actionTime이 이 변수의 저장 값보다 같거나 커지면 행동을 전환)
    public float farawayTime;  // Chase 상태에서 몬스터가 더 이상 플레이어를 향해 이동 불가한 위치에 있을 때 이 시간이 지나면 GoBack 상태로 전환
    public float goBackTime;  // farawayTime이 이 변수의 값보다 커지면 GoBack 상태로 전환하기 위한 변수

    Vector3 originPosition;  // 몬스터가 원래 있던 좌표를 저장하는 변수(몬스터가 리스폰된 위치를 저장)
    public float moveRange;  // 몬스터가 기본 상태일 때 이동 가능한 거리(originPosition에서 이 변수의 값만큼 +-의 범위 내에서만 이동 가능)

    Vector2 playerDirection;  // 몬스터가 바라보는 플레이어의 방향
    public float maxDistance;  // 몬스터가 플레이어를 쫓는 최대 거리(이 거리 이상으로 멀어지면 다시 돌아가도록 설정)

    bool canForward;  // 전진이 가능한지를 저장

    // 넉백을 위한 딜레이 시간
    public float knockbackTime;
    WaitForSeconds knockbackDelay;

    public RuntimeAnimatorController[] enemyAnimController;  // 0번 인덱스는 Idle 애니메이터를 1번 인덱스는 Move 애니메이터를 갖는 것으로 설정(추후에 알아보기 쉽도록 설정할 방법 모색)
    Animator animator;
    Rigidbody2D rigid;
    SpriteRenderer spriteR;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();
        spriteR = GetComponent<SpriteRenderer>();

        knockbackDelay = new WaitForSeconds(knockbackTime);

        originPosition = transform.position;
        canForward = true;
        
        state = Random.Range(0, 2) == 0 ? ActionState.Idle : ActionState.Move;  // 0이면 Idle로 1이면 Move로 시작

        direction = Random.Range(0, 1) == 0 ? -1f : 1f;
        spriteR.flipX = direction == 1f ? true : false;
    }

    private void FixedUpdate()
    {
        detectBoxCenter.x = transform.position.x + direction * 2f;
        detectBoxCenter.y = transform.position.y;

        // Chase 상태에서 앞이 벽이거나 낭떠러지일 경우 -> 상태는 Chase로 두고 가만히 있다가 일정 시간이 지나면 GoBack 상태가 되도록 설정
        frontCheck = Physics2D.Raycast(transform.position, Vector2.right * direction, frontCheckDistance, floorLayer);
        floorCheck = Physics2D.Raycast(transform.position, Vector2.down, floorCheckDistance, floorLayer);
        detectPlayer = Physics2D.BoxCast(detectBoxCenter, detectBoxSize, 0f, Vector2.right, 0f, playerLayer);

        if(detectPlayer.collider != null)
        {
            Debug.Log("감지 성공");
        }

        if (frontCheck.collider != null || floorCheck.collider == null)
            canForward = false;

        switch (state)
        {
            case ActionState.Idle:
                rigid.linearVelocityX = 0f;
                animator.runtimeAnimatorController = enemyAnimController[0];
                actionTime += Time.fixedDeltaTime;
                break;
            case ActionState.Move:
                animator.runtimeAnimatorController = enemyAnimController[1];
                if(transform.position.x > originPosition.x + moveRange || transform.position.x < originPosition.x - moveRange)
                {
                    direction *= -1f;
                    spriteR.flipX = !spriteR.flipX;
                }
                rigid.linearVelocityX = moveSpeed * direction;
                actionTime += Time.fixedDeltaTime;
                break;
            case ActionState.Chase:
                playerDirection = detectPlayer.collider.transform.position - transform.position;
                if(playerDirection.magnitude > maxDistance)
                {
                    state = ActionState.GoBack;
                    spriteR.flipX = !spriteR.flipX;
                    break;
                }

                direction = playerDirection.x > 0f ? 1f : -1f;

                if (!canForward)
                {
                    farawayTime += Time.fixedDeltaTime;
                    rigid.linearVelocityX = 0f;
                }
                else
                {
                    farawayTime = 0f;
                    rigid.linearVelocityX = moveSpeed * direction;
                }

                if (farawayTime > goBackTime)
                {
                    state = ActionState.GoBack;
                    spriteR.flipX = !spriteR.flipX;
                }
                break;
            case ActionState.GoBack:
                direction = originPosition.x - transform.position.x > 0f ? 1f : -1f;
                rigid.linearVelocityX = moveSpeed * direction;

                if (Mathf.Approximately(transform.position.x, originPosition.x))
                {
                    state = ActionState.Idle;
                    actionTime = 0f;
                }
                break;
        }

        if (actionTime > changActionTime)
        {
            int nextAction = Random.Range(0, 11);

            if (nextAction < 5)
                state = ActionState.Idle;
            else
                state = ActionState.Move;

            actionTime = 0f;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(detectBoxCenter, detectBoxSize);
    }
}

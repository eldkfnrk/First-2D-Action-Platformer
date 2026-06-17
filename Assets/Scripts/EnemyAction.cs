using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class EnemyAction : MonoBehaviour
{
    public enum EnemyActionState
    {
        Idle,
        Run,
        Fall,
        Dash,
        DashAttack,
        Attack,
        Slide,
        Hurt,
        Croush,
        Jump,
        Death,
    }

    EnemyActionState actionState;

    public enum EnemyState
    {
        Phase1,
        Phase2,
        None,
    }

    EnemyState enemyState;

    // 플레이어 감지
    RaycastHit2D playerDetect;
    public LayerMask playerLayer;
    public Vector2 detectRange;
    public float detectDistance;

    // 바닥 및 벽 감지
    RaycastHit2D groundCheck;
    RaycastHit2D wallCheck;
    public LayerMask groundLayer;
    public float groundCheckDistance;
    public float wallCheckDistance;

    // 이동
    public float direction;
    public float moveSpeed;
    public float dashSpeed;
    public float slideSpeed;

    // 점프
    public float jumpPower;
    public float jumpCheckTimer;

    // 상태
    public bool isAttack;
    public bool isDash;
    public bool canDash;
    public bool isHurt;
    public bool isJump;

    Rigidbody2D rigid;
    SpriteRenderer spriteR;

    private void Awake()
    {
        actionState = EnemyActionState.Idle;
        enemyState = EnemyState.None;
        rigid = GetComponent<Rigidbody2D>();
        spriteR = GetComponent<SpriteRenderer>();
    }

    // 플레이어가 맵에 들어가면 넓은 공간이 나오도록 레벨 디자인
    // 보스 캐릭터 앞 일정 지점에 도달하면 보스전 시작(투명 오브젝트를 트리거로 활용)
    // 대쉬나 슬라이드 등의 동작을 하다가 공격 혹은 대쉬 공격을 하는 패턴 수립
    // 일정 체력 이하로 떨어지면 패턴 변경
    // 보스전 중에는 맵 크기를 제한

    // 범위 내에 플레이어가 있을 때 행동
    // phase1
    // 플레이어를 향해 일반 공격
    // 대쉬와 점프를 통한 회피
    // phase2
    // phase1의 행동에 대쉬 공격과 슬라이드 공격을 추가
    // 슬라이드 공격은 약간의 데미지 혹은 방어를 무력화하면서 뒤로 튕겨나가도록 설정

    // 범위 내에 플레이어가 없을 때 행동
    // 플레이어를 향해 이동
    // phase1 때는 달리기만 phase2 때는 대쉬와 점프까지 수행

    private void Update()
    {
        playerDetect = Physics2D.BoxCast(transform.position, detectRange, 0f, Vector2.zero, detectDistance, playerLayer);
        direction = (playerDetect.collider.gameObject.transform.position.x - transform.position.x) < 0f ? -1f : 1f;  // 플레이어가 캐릭터 기준 왼쪽에 있다면 -1f, 오른쪽에 있다면 1f
        switch (enemyState)
        {
            case EnemyState.Phase1:
                PhaseOnePattern();
                break;
            case EnemyState.Phase2:
                PhaseTwoPattern();
                break;
        }
    }

    private void LateUpdate()
    {
        if (isJump)
            return;

        if (direction > 0f)
            spriteR.flipX = false;
        else if (direction < 0f)
            spriteR.flipX = true;
    }

    private void FixedUpdate()
    {
        switch (actionState)
        {
            case EnemyActionState.Idle:
                jumpCheckTimer = 0f;
                break;
            case EnemyActionState.Run:
                rigid.linearVelocityX = moveSpeed * direction;
                break;
            case EnemyActionState.Fall:
                break;
            case EnemyActionState.Dash:
                break;
            case EnemyActionState.DashAttack:
                break;
            case EnemyActionState.Attack:
                break;
            case EnemyActionState.Slide:
                break;
            case EnemyActionState.Hurt:
                break;
            case EnemyActionState.Croush:
                break;
            case EnemyActionState.Jump:
                float moveDirection = spriteR.flipX ? -1f : 1f;  // 점프 동안은 좌우 움직임을 한 곳으로 통일하기 위하여 따로 이동 방향을 얻도록 한다.
                rigid.linearVelocityX = moveSpeed * moveDirection;
                if (!isJump)
                {
                    rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
                    isJump = true;
                }
                jumpCheckTimer += Time.fixedDeltaTime;
                // 0.25초 동안 점프 후 바닥을 확인하지 않도록 제한
                if (jumpCheckTimer < 0.25f) 
                    return;

                if (rigid.linearVelocityY<0f)
                {
                    jumpCheckTimer = 0f;
                    actionState = EnemyActionState.Fall;
                }

                if (groundCheck.collider != null)
                {
                    actionState = EnemyActionState.Idle;
                }
                break;
            case EnemyActionState.Death:
                break;
        }
    }

    // 페이즈1 패턴
    void PhaseOnePattern()
    {
        float actionValue = Random.value;
        // 범위 내에 플레이어 있는 경우
        if (playerDetect.collider != null)
        {
            // 행동 확률
            // 공격 60%
            // 대쉬 30%
            // 점프 10%
            if (actionValue < 0.1f)  // 10% - 점프
            {
                actionState = EnemyActionState.Jump;
            } 
            else if (actionValue < 0.4f) // 30% - 대쉬
            {
                actionState = EnemyActionState.Dash;
            }
            else // 60% - 공격
            {
                actionState = EnemyActionState.Attack;
            }
        }
        else  // 범위 내에 플레이어 없는 경우
        {
            // 행동 확률
            // 플레이어를 향한 이동 80%
            // 대쉬 20%
            if (actionValue < 0.2f)
            {
                actionState = EnemyActionState.Dash;
            }
            else
            {
                actionState = EnemyActionState.Run;
            }
        }
    }

    // 페이즈2 패턴
    void PhaseTwoPattern()
    {

    }

    // 피격 시 호출
    public void Hit()
    {
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, detectRange);
    }
}

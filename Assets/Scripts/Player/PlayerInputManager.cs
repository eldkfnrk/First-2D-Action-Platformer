using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class PlayerInputManager : MonoBehaviour
{
    PlayerStateMachine player;
    PlayerRuntimeData variableData;
    Vector2 inputMoveValue;

    private void Awake()
    {
        player = GetComponent<PlayerStateMachine>();
        variableData = GetComponent<PlayerRuntimeData>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        inputMoveValue = context.ReadValue<Vector2>();
        variableData.downKeyPressed = inputMoveValue.y < 0f;

        // y키를 같이 누르게 되면 x의 값이 작아지게 되는데 그러면 이동 속도에 영향을 끼치게 되므로 값을 일정하게 유지할 수 있도록 설정
        if (inputMoveValue.x > 0f)
            variableData.moveDirection = 1f;
        if (inputMoveValue.x < 0f)
            variableData.moveDirection = -1f;
        if (inputMoveValue.x == 0f)
            variableData.moveDirection = 0f;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started && (player.CurrentState.inputControl & InputControl.Jump) != 0)
            player.Jump();
    }

    public void OnRoll(InputAction.CallbackContext context)
    {
        if (context.started && (player.CurrentState.inputControl & InputControl.Roll) != 0)
        {
            if (variableData.atkRoutine)
                variableData.rollKeyDown = true;
            else
                player.Roll();
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if(context.started && (player.CurrentState.inputControl & InputControl.Attack) != 0 && variableData.atkKeyDownCount < 3)
        {
            ++variableData.atkKeyDownCount;

            if(variableData.atkCount == 0)
            {
                ++variableData.atkCount;
                player.Attack(variableData.atkCount);
            }
        }
    }

    public void OnBlock(InputAction.CallbackContext context)
    {
        if (context.started && (player.CurrentState.inputControl & InputControl.Block) != 0)
            player.Block();
        
        if (context.canceled)
            variableData.isBlock = false;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if(context.started && player.variableData.canInteractive)
        {
            GameManager.instance.SceneChange(variableData.doorSpawnPoint);
        }
    }

    public void OnDeath(InputAction.CallbackContext context)
    {
        if (context.started && variableData.isDead)
            variableData.isRevival = true;
    }
}

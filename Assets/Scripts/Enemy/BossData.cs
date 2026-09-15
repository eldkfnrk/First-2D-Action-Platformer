using UnityEngine;

[CreateAssetMenu(fileName = "BossData", menuName = "Scriptable Objects/BossData")] 
public class BossData : ScriptableObject
{
    public float moveSpeed;
    public float dashSpeed;
    public float slideSpeed;
}

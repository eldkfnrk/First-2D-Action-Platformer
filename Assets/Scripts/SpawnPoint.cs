using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public enum PointID
    {
        TestScene_Door,
        CentralArea_Door1,
        None,
    }

    public PointID id;
    public PointID targetId;
}

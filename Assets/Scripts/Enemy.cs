using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float maxHp;
    public float curHp;

    private void Awake()
    {
        curHp = maxHp;
    }

    void Update()
    {
        if (curHp < 1f)
        {
            curHp = 1f;
        }
    }
}

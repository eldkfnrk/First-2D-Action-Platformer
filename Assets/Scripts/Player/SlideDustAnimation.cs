using UnityEngine;

public class SlideDustAnimation : MonoBehaviour
{
    GameObject slideDustVFX;

    Vector3 slideDustPos;

    private void Awake()
    {
        slideDustVFX = this.gameObject;
        slideDustPos.x = 0.25f;
        slideDustPos.y = 0.7f;
    }

    public void SlideDustReposition()
    {
        slideDustVFX.transform.localPosition = slideDustPos;
    }

    public void SlideDustDestroy()
    {
        slideDustVFX.SetActive(false);
    }
}

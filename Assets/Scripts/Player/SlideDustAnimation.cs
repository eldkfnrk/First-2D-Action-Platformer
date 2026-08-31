using UnityEngine;

public class SlideDustAnimation : MonoBehaviour
{
    GameObject slideDustVFX;

    Vector3 slideDustPos;

    private void Awake()
    {
        slideDustVFX = this.gameObject;
    }

    private void OnEnable()
    {
        float sight = GetComponentInParent<PlayerRuntimeData>().sightDirection;
        transform.localRotation = sight == 1 ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
        slideDustPos.x = sight * 0.25f;
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

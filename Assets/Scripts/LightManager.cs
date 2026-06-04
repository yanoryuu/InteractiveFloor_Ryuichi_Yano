using DG.Tweening;
using UnityEngine;

public class LightManager : MonoBehaviour
{
    [SerializeField] private Transform _directionalLightTransform;

    public void StartRotate()
    {
        _directionalLightTransform.DORotate(new Vector3(0,360,0), 30f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }
}

using DG.Tweening;
using UnityEngine;

/// <summary>
/// 餌のライトを DOTween で明滅（点滅）させる。
/// Intensity を MinIntensity〜MaxIntensity の間で Yoyo ループさせる。
/// 餌Prefab内のLightと同じGameObject、または子のLightにアタッチして使う。
/// </summary>
[RequireComponent(typeof(Light))]
public class FoodLightBlink : MonoBehaviour
{
    [Tooltip("明滅させるLight。未指定なら同じGameObjectのLightを使う。")]
    [SerializeField] private Light _light;

    [Tooltip("明滅の最小・最大Intensity（幅 5〜20）。")]
    [SerializeField] private float _minIntensity = 5f;
    [SerializeField] private float _maxIntensity = 20f;

    [Tooltip("最小→最大の片道にかかる時間 [秒]。")]
    [SerializeField] private float _duration = 0.6f;

    [Tooltip("明滅の補間カーブ。")]
    [SerializeField] private Ease _ease = Ease.InOutSine;

    private Tween _tween;

    private void Awake()
    {
        if (_light == null) _light = GetComponent<Light>();
    }

    private void OnEnable()
    {
        if (_light == null) return;

        _light.intensity = _minIntensity;
        _tween = _light.DOIntensity(_maxIntensity, _duration)
            .SetEase(_ease)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDisable()
    {
        _tween?.Kill();
        _tween = null;
    }
}

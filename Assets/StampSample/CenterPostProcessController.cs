using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using InteractiveFloor;

/// <summary>
/// 作品の起動トリガー。
/// フィールドの中心に鑑賞者が来たら、PostProcessing(URP Volume)の
/// Vignette の Intensity を下げ、ColorAdjustments の Post Exposure を 0 に戻す
/// アニメーションを DOTween で再生する。
/// アニメーションと同時に、指定したゲームオブジェクトを Renderer マテリアルの
/// アルファでフェードアウトさせる。
/// アニメーション完了後、フェード対象を非表示・開始前パーティクルを非表示にし、
/// 完了後パーティクルを表示してから <see cref="OnActivationComplete"/> を通知し、
/// 後続のスタンプ生成フローへ繋ぐ。
///
/// フロー:
///   [開始前]       beforeParticle を表示
///     │ 人が中心に来る
///     ▼
///   [アニメーション] PostProcessing 補間 ＋（同時に）fadeTarget をフェードアウト
///     │ 完了
///     ▼
///   [完了後]       fadeTarget/beforeParticle を非表示 → afterParticle を表示 → スタンプ生成開始
///
/// StampSample 内で完結するシステム。
/// </summary>
public class CenterPostProcessController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("鑑賞者の位置を取得する SensorObjectManager")]
    [SerializeField] private SensorObjectManager _sensorObjectManager;

    [Tooltip("Vignette と ColorAdjustments を持つ URP の Volume")]
    [SerializeField] private Volume _volume;

    [Header("Field Center (XZ)")]
    [Tooltip("フィールドの中心。未指定ならこのオブジェクトの位置を使用する。")]
    [SerializeField] private Transform _fieldCenter;

    [Tooltip("中心とみなす半径（ワールド空間）")]
    [SerializeField] private float _centerRadius = 1.0f;

    [Header("Particles")]
    [Tooltip("開始前に表示しておくパーティクル（完了後に非表示にする）")]
    [SerializeField] private GameObject _beforeParticle;

    [Tooltip("アニメーション完了後に表示するパーティクル")]
    [SerializeField] private GameObject _afterParticle;

    [Header("Fade Out (アニメーションと同時)")]
    [Tooltip("アニメーション中にフェードアウトさせるゲームオブジェクト（Renderer のマテリアルアルファを操作）")]
    [SerializeField] private GameObject _fadeTarget;

    [Header("Target Values (アニメーション後の値)")]
    [Tooltip("下げたあとの Vignette Intensity")]
    [SerializeField] private float _targetVignetteIntensity = 0.0f;

    [Tooltip("戻したあとの Post Exposure（0 に戻す）")]
    [SerializeField] private float _targetPostExposure = 0.0f;

    [Header("Tween")]
    [Tooltip("アニメーションにかかる時間 [秒]")]
    [SerializeField] private float _duration = 1.0f;

    [Tooltip("補間カーブ")]
    [SerializeField] private Ease _ease = Ease.InOutSine;

    /// <summary>
    /// 中心に人が来て、PostProcessing のアニメーションが完了したときに呼ばれる。
    /// （スタンプ生成の開始フックとして使用する）
    /// </summary>
    public event Action OnActivationComplete;

    /// <summary>
    /// 起動シーケンスが開始済みか（このフローは一度きり）。
    /// </summary>
    public bool IsActivated => _isActivated;

    // マテリアルのアルファを探す際の候補プロパティ（URP / Built-in / Particle 系）
    private static readonly string[] ColorProperties = { "_BaseColor", "_Color", "_TintColor" };

    private Vignette _vignette;
    private ColorAdjustments _colorAdjustments;

    private bool _isActivated;
    private Sequence _sequence;

    private void Awake()
    {
        if (_fieldCenter == null) _fieldCenter = transform;

        if (_volume != null && _volume.profile != null)
        {
            _volume.profile.TryGet(out _vignette);
            _volume.profile.TryGet(out _colorAdjustments);
        }
    }

    private void Start()
    {
        // 初期表示状態：開始前パーティクルを表示、完了後パーティクルは非表示
        if (_beforeParticle != null) _beforeParticle.SetActive(true);
        if (_afterParticle != null) _afterParticle.SetActive(false);
    }

    private void Update()
    {
        // 起動済みなら以降の監視は不要（一度きりのフロー）
        if (_isActivated) return;

        if (IsAnyViewerAtCenter())
            Activate();
    }

    /// <summary>
    /// 起動シーケンス。PostProcessing アニメーションとフェードアウトを同時に再生し、
    /// 完了後にパーティクルを切り替えて通知する。
    /// </summary>
    private void Activate()
    {
        _isActivated = true;

        _sequence = DOTween.Sequence();

        // PostProcessing アニメーション
        if (_vignette != null)
        {
            _sequence.Join(DOTween.To(
                    () => _vignette.intensity.value,
                    v => _vignette.intensity.value = v,
                    _targetVignetteIntensity, _duration)
                .SetEase(_ease));
        }

        if (_colorAdjustments != null)
        {
            _sequence.Join(DOTween.To(
                    () => _colorAdjustments.postExposure.value,
                    v => _colorAdjustments.postExposure.value = v,
                    _targetPostExposure, _duration)
                .SetEase(_ease));
        }

        // 同時にフェードアウト
        var fade = BuildFadeOutTween(_fadeTarget);
        if (fade != null) _sequence.Join(fade);

        // 完了後：パーティクルを切り替えて後続フローへ通知
        _sequence.OnComplete(() =>
        {
            if (_fadeTarget != null) _fadeTarget.SetActive(false);
            if (_beforeParticle != null) _beforeParticle.SetActive(false);
            PlayParticle(_afterParticle);
            OnActivationComplete?.Invoke();
        });
    }

    /// <summary>
    /// 対象オブジェクト配下の全 Renderer のマテリアルアルファを 0 へフェードする Tween を構築する。
    /// </summary>
    private Tween BuildFadeOutTween(GameObject target)
    {
        if (target == null) return null;

        var renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return null;

        var fade = DOTween.Sequence();
        foreach (var r in renderers)
        {
            // インスタンス化されたマテリアルに対してフェードをかける
            foreach (var mat in r.materials)
            {
                var prop = GetColorProperty(mat);
                if (prop == null) continue;
                fade.Join(mat.DOFade(0.0f, prop, _duration).SetEase(_ease));
            }
        }

        // フェード対象に色プロパティが無ければ Tween 不要
        return fade.Duration() > 0f ? fade : null;
    }

    /// <summary>
    /// マテリアルが持つアルファ操作用の色プロパティ名を返す（無ければ null）。
    /// </summary>
    private static string GetColorProperty(Material mat)
    {
        foreach (var p in ColorProperties)
        {
            if (mat.HasProperty(p)) return p;
        }
        return null;
    }

    /// <summary>
    /// パーティクル（GameObject）を表示して再生する。
    /// </summary>
    private static void PlayParticle(GameObject go)
    {
        if (go == null) return;

        go.SetActive(true);
        var ps = go.GetComponent<ParticleSystem>();
        if (ps != null) ps.Play(true);
    }

    /// <summary>
    /// いずれかの鑑賞者が中心の半径内にいるか
    /// </summary>
    private bool IsAnyViewerAtCenter()
    {
        if (_sensorObjectManager == null || _sensorObjectManager.SensorList == null)
            return false;

        // SensorObject の positionMedian は (x, y) = ワールドの (X, Z)
        var center = new Vector2(_fieldCenter.position.x, _fieldCenter.position.z);

        foreach (var sensorObject in _sensorObjectManager.SensorList)
        {
            var pos = new Vector2(sensorObject.positionMedian.x, sensorObject.positionMedian.y);
            if (Vector2.Distance(pos, center) < _centerRadius)
                return true;
        }

        return false;
    }

    private void OnDisable()
    {
        _sequence?.Kill();
        _sequence = null;
    }

    private void OnDrawGizmosSelected()
    {
        var c = _fieldCenter != null ? _fieldCenter : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(c.position, _centerRadius);
    }
}

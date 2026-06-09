using UnityEngine;

/// <summary>
/// スタンプ生成・判定の管理。
///
/// フロー: 人が中心に来る ＝＞ PostProcessing アニメーション ＝＞ スタンプ生成開始
/// <see cref="CenterPostProcessController"/> のアニメーション完了通知を受けてから
/// スタンプの生成・判定を開始する。
/// </summary>
public class StampManager : MonoBehaviour
{
    [Tooltip("起動トリガー。中心入場 → PostProcessing アニメーション完了でスタンプ生成を開始する。")]
    [SerializeField] private CenterPostProcessController centerPostProcessController;
    [SerializeField] private StampHitDetecter stampHitDetecter;
    [SerializeField] private StampGenerator stampGenerator;

    // 起動シーケンスが完了し、スタンプ生成を開始してよいか
    private bool _stampStarted;

    private void OnEnable()
    {
        if (centerPostProcessController != null)
            centerPostProcessController.OnActivationComplete += StartStamping;
    }

    private void OnDisable()
    {
        if (centerPostProcessController != null)
            centerPostProcessController.OnActivationComplete -= StartStamping;
    }

    private void StartStamping()
    {
        _stampStarted = true;
    }

    private void Update()
    {
        // PostProcessing アニメーションが終わるまではスタンプを生成しない
        if (!_stampStarted) return;

        stampGenerator.Generate();
        stampHitDetecter.HitDetect(stampGenerator.activeStamps);
    }
}

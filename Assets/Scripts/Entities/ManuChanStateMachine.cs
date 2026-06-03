using UnityEngine;

/// <summary>
/// ManuChan の状態を管理し、Animator にパラメータを渡すステートマシン。
///
/// AnimatorController に以下のパラメータを設定してください:
///   float  Speed        : 移動速度(0=停止, >0=歩行/逃走)
///   bool   IsFleeing    : 逃げモードか
///   int    GrowthStage  : 成長段階 (0=幼年 / 1=青年 / 2=成人)
///   trigger GrowUp      : 成長段階が上がった瞬間に発火
///   trigger Contact     : 他のマヌちゃんと接触した瞬間に発火
/// </summary>
public class ManuChanStateMachine : MonoBehaviour
{
    // Animator パラメータ名のハッシュキャッシュ
    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashIsFleeing = Animator.StringToHash("IsFleeing");
    private static readonly int HashGrowthStage = Animator.StringToHash("GrowthStage");
    private static readonly int HashGrowUp = Animator.StringToHash("GrowUp");
    private static readonly int HashContact = Animator.StringToHash("Contact");

    private Animator _animator;

    public ManuChanState CurrentState { get; private set; } = ManuChanState.Idle;

    /// <summary>
    /// モデルが切り替わったときに呼ぶ。アクティブな子のAnimatorを再取得する。
    /// </summary>
    public void RefreshAnimator()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Moveから毎フレーム呼ぶ。Speed と IsFleeing を更新して状態を決定する。
    /// </summary>
    public void UpdateMovement(float speed, bool isFleeing)
    {
        var next = isFleeing ? ManuChanState.Flee
                 : speed > 0.05f ? ManuChanState.Walk
                 : ManuChanState.Idle;

        CurrentState = next;

        if (_animator == null) return;
        _animator.SetFloat(HashSpeed, speed);
        _animator.SetBool(HashIsFleeing, isFleeing);
    }

    /// <summary>
    /// 成長段階が変わったときに呼ぶ。GrowUp トリガーを発火する。
    /// </summary>
    public void TriggerGrowUp(int stage)
    {
        if (_animator == null) return;
        _animator.SetInteger(HashGrowthStage, stage);
        _animator.SetTrigger(HashGrowUp);
    }

    /// <summary>
    /// 別のマヌちゃんと接触した瞬間に呼ぶ。
    /// </summary>
    public void TriggerContact()
    {
        if (_animator == null) return;
        _animator.SetTrigger(HashContact);
    }
}

public enum ManuChanState
{
    Idle,
    Walk,
    Flee,
}

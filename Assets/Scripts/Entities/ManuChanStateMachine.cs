using UnityEngine;

/// <summary>
/// ManuChan の状態を管理し、Animator にパラメータを渡すステートマシン。
///
/// AnimatorController に以下のパラメータを設定してください:
///   float   Speed        : 移動速度(0=停止, >0=歩行/逃走)
///   bool    IsFleeing    : 逃げモードか
///   int     GrowthStage  : 成長段階 (0=幼年 / 1=青年 / 2=成人)
///   trigger GrowUp       : 成長段階が上がった瞬間に発火
///   trigger Contact      : 他のマヌちゃんと接触した瞬間に発火
///
/// パラメータが未設定のAnimatorControllerでも警告なく動作します。
/// </summary>
public class ManuChanStateMachine : MonoBehaviour
{
    private static readonly int HashSpeed       = Animator.StringToHash("Speed");
    private static readonly int HashIsFleeing   = Animator.StringToHash("IsFleeing");
    private static readonly int HashGrowthStage = Animator.StringToHash("GrowthStage");
    private static readonly int HashGrowUp      = Animator.StringToHash("GrowUp");
    private static readonly int HashContact     = Animator.StringToHash("Contact");

    private Animator _animator;

    // RefreshAnimator 時にキャッシュ。毎フレームのパラメータ存在チェックを避ける。
    private bool _hasSpeed;
    private bool _hasIsFleeing;
    private bool _hasGrowthStage;
    private bool _hasGrowUp;
    private bool _hasContact;

    public ManuChanState CurrentState { get; private set; } = ManuChanState.Idle;

    /// <summary>
    /// モデルが切り替わったときに呼ぶ。アクティブな子の Animator を再取得し、
    /// 保持するパラメータセットをキャッシュする。
    /// </summary>
    public void RefreshAnimator()
    {
        _animator = GetComponentInChildren<Animator>();

        _hasSpeed = _hasIsFleeing = _hasGrowthStage = _hasGrowUp = _hasContact = false;
        if (_animator == null || _animator.runtimeAnimatorController == null) return;

        foreach (var p in _animator.parameters)
        {
            var h = p.nameHash;
            if (h == HashSpeed)       _hasSpeed       = true;
            else if (h == HashIsFleeing)   _hasIsFleeing   = true;
            else if (h == HashGrowthStage) _hasGrowthStage = true;
            else if (h == HashGrowUp)      _hasGrowUp      = true;
            else if (h == HashContact)     _hasContact     = true;
        }
    }

    /// <summary>Move から毎フレーム呼ぶ。Speed と IsFleeing を更新して状態を決定する。</summary>
    public void UpdateMovement(float speed, bool isFleeing)
    {
        CurrentState = isFleeing       ? ManuChanState.Flee
                     : speed > 0.05f  ? ManuChanState.Walk
                     : ManuChanState.Idle;

        if (_animator == null) return;
        if (_hasSpeed)     _animator.SetFloat(HashSpeed, speed);
        if (_hasIsFleeing) _animator.SetBool(HashIsFleeing, isFleeing);
    }

    /// <summary>成長段階が変わったときに呼ぶ。GrowUp トリガーを発火する。</summary>
    public void TriggerGrowUp(int stage)
    {
        if (_animator == null) return;
        if (_hasGrowthStage) _animator.SetInteger(HashGrowthStage, stage);
        if (_hasGrowUp)      _animator.SetTrigger(HashGrowUp);
    }

    /// <summary>別のマヌちゃんと接触した瞬間に呼ぶ。</summary>
    public void TriggerContact()
    {
        if (_animator == null) return;
        if (_hasContact) _animator.SetTrigger(HashContact);
    }
}

public enum ManuChanState
{
    Idle,
    Walk,
    Flee,
}

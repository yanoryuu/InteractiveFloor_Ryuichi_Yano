using System.Collections;
using InteractiveFloor.Detect;
using UnityEngine;

public class ManuChan : Entity
{
    [SerializeField] private GameObject[] _stageModels;
    [SerializeField] private ParticleSystem[] _growUpParticles;

    // GrowUp ステート名のハッシュ（AnimatorController のステート名と一致させること）
    private static readonly int HashGrowUpState = Animator.StringToHash("GrowUp");

    private float _currentYaw;
    private bool _hasYaw;
    private int _lastGrowthStage = -1;

    private ManuChanStateMachine _stateMachine;
    private ManuChanStateMachine StateMachine
        => _stateMachine != null ? _stateMachine : (_stateMachine = GetComponent<ManuChanStateMachine>());

    // SetActive(true) のタイミングで確実に正しいモデルを表示する
    private void OnEnable()
    {
        IsLeaving = false;
        IsAdult = false;
        _lastGrowthStage = -1; // リスポーン時にリセットしてスポーン扱いにする
        GrowUpEntity(GrowthStage.Value);
    }

    // FlockController が毎フレーム設定する
    public int ViewerCount { get; set; } = 0;

    /// <summary>成人後にエリア外へ退場中かどうか。FlockController が制御・参照する。</summary>
    public bool IsLeaving { get; private set; }

    /// <summary>最大成長（成人, stage>=2）に達したか。FlockController が退場判定に使う。</summary>
    public bool IsAdult { get; private set; }

    /// <summary>
    /// 退場状態を設定する。FlockController が「Viewerにくっついている間は退場しない、
    /// Viewerが消えたら退場する」という制御に使う。
    /// </summary>
    public void SetLeaving(bool leaving) => IsLeaving = leaving;

    public override void Move(Vector3 target)
    {
        // FlockController が target を方向込みで計算済みなので方向反転は行わない
        var isFleeing = ViewerCount >= InteractiveFloorConsts.ManuChanFleeViewerCountThreshold;

        var toTarget = target - transform.position;
        toTarget.y = 0f;

        var moveDirection = toTarget;
        var prevPosition = transform.position;

        if (moveDirection.sqrMagnitude > Mathf.Epsilon)
        {
            var targetYaw = Quaternion.LookRotation(moveDirection.normalized, Vector3.up).eulerAngles.y;
            if (!_hasYaw) { _currentYaw = transform.eulerAngles.y; _hasYaw = true; }

            var isRotatingSlowdown = !isFleeing &&
                Mathf.Abs(Mathf.DeltaAngle(_currentYaw, targetYaw)) > InteractiveFloorConsts.ManuChanRotatingMoveSlowdownAngleThreshold;

            _currentYaw = Mathf.MoveTowardsAngle(_currentYaw, targetYaw, InteractiveFloorConsts.EntityRotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.AngleAxis(_currentYaw, Vector3.up) * Quaternion.AngleAxis(-35f, Vector3.right);

            var moveSpeed = InteractiveFloorConsts.EntityMoveSpeed;
            if (isRotatingSlowdown) moveSpeed *= InteractiveFloorConsts.ManuChanRotatingMoveSpeedMultiplier;

            var keepDist = InteractiveFloorConsts.ManuChanChaseKeepDistance;
            var shouldMove = isFleeing || toTarget.magnitude > keepDist;
            if (shouldMove)
            {
                var destination = isFleeing
                    ? transform.position + moveDirection.normalized * 100f
                    : target - toTarget.normalized * keepDist;

                transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
                GrowthRate.Value = Mathf.Clamp01(GrowthRate.Value + InteractiveFloorConsts.ManuChanWalkingGrowthRate * Time.deltaTime);
            }
        }

        Position = transform.position;

        // 実際の移動量から Speed を算出してステートマシンに渡す
        var actualSpeed = Vector3.Distance(transform.position, prevPosition) / Time.deltaTime;
        StateMachine?.UpdateMovement(actualSpeed, isFleeing);
    }

    public override void GrowUpEntity(int stage)
    {
        if (_stageModels == null || _stageModels.Length == 0) return;

        var resolved = Mathf.Min(stage, _stageModels.Length - 1);
        while (resolved > 0 && _stageModels[resolved] == null)
            resolved--;
        if (_stageModels[resolved] == null) return;

        var isGrowing = _lastGrowthStage >= 0 && stage > _lastGrowthStage;
        _lastGrowthStage = stage;

        if (isGrowing)
        {
            // 成長時: 現在のモデルでアニメーション再生し、終了後にモデルを切り替える
            StateMachine?.RefreshAnimator();
            StateMachine?.TriggerGrowUp(stage);
            StartCoroutine(SwitchModelAfterGrowUp(resolved, stage));
        }
        else
        {
            // スポーン/リスポーン時: 即座にモデルを切り替える
            ApplyStageModel(resolved);
            StateMachine?.RefreshAnimator();
            StateMachine?.ApplyGrowthStage(stage);
            if (stage >= 2) IsAdult = true;
        }
    }

    private void ApplyStageModel(int resolved)
    {
        for (var i = 0; i < _stageModels.Length; i++)
        {
            if (_stageModels[i] != null)
                _stageModels[i].SetActive(i == resolved);
        }
    }

    private IEnumerator SwitchModelAfterGrowUp(int resolvedStage, int stage)
    {
        // トリガーが Animator に届くのを1フレーム待つ
        yield return null;

        var animator = StateMachine?.CurrentAnimator;
        if (animator != null)
        {
            // GrowUp ステートへの遷移が始まるまで待つ（最大1秒のタイムアウト）
            for (var t = 0f; t < 1f; t += Time.deltaTime)
            {
                if (IsInOrTransitioningToGrowUp(animator)) break;
                yield return null;
            }

            // GrowUp ステートと、そこへの/からの遷移が完全に終わるまで待つ
            while (animator != null && IsInOrTransitioningToGrowUp(animator))
                yield return null;
        }

        // モデルを新しいステージに切り替えて Animator を更新
        ApplyStageModel(resolvedStage);
        StateMachine?.RefreshAnimator();
        StateMachine?.ApplyGrowthStage(stage);

        // 変身の瞬間にパーティクルを再生
        if (_growUpParticles != null)
        {
            foreach (var ps in _growUpParticles)
            {
                if (ps == null) continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play();
            }
        }

        // 成人フラグは必ずモデル切替後に立てる（アニメーション中は退場判定しない）
        if (stage >= 2) IsAdult = true;
    }

    private bool IsInOrTransitioningToGrowUp(Animator animator)
    {
        if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash == HashGrowUpState)
            return true;
        return animator.IsInTransition(0) &&
               animator.GetNextAnimatorStateInfo(0).shortNameHash == HashGrowUpState;
    }

    public override void OnContactEnter(Entity other)
    {
        if (other == null || other == this) return;
        if (other.Type != EntityType.Manuchan) return;

        GrowthRate.Value = Mathf.Clamp01(GrowthRate.Value + InteractiveFloorConsts.ManuChanGrowthRateIncrease);
        StateMachine?.TriggerContact();
    }

    public override void OnContactStay(Entity other)
    {
        if (other == null || other == this) return;
        if (other.Type != EntityType.Manuchan) return;

        GrowthRate.Value = Mathf.Clamp01(GrowthRate.Value + InteractiveFloorConsts.ManuChanContactStayGrowthRate * Time.deltaTime);
    }
}

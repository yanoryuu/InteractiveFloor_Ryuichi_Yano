using InteractiveFloor.Detect;
using UnityEngine;

public class ManuChan : Entity
{
    [SerializeField] private GameObject[] _stageModels;

    private float _currentYaw;
    private bool _hasYaw;

    private ManuChanStateMachine _stateMachine;
    private ManuChanStateMachine StateMachine
        => _stateMachine != null ? _stateMachine : (_stateMachine = GetComponent<ManuChanStateMachine>());

    // SetActive(true) のタイミングで確実に正しいモデルを表示する
    private void OnEnable()
    {
        GrowUpEntity(GrowthStage.Value);
    }

    public override void Move(Vector3 target)
    {
        var isFleeing = EntityManager.Instance != null &&
                        EntityManager.Instance.ActiveEntityCount >= InteractiveFloorConsts.ManuChanFleeViewerCountThreshold;

        var toTarget = target - transform.position;
        toTarget.y = 0f;

        var moveDirection = isFleeing ? -toTarget : toTarget;
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

            var shouldMove = isFleeing || toTarget.magnitude > InteractiveFloorConsts.EntityViewerStopDistance;
            if (shouldMove)
            {
                var destination = isFleeing
                    ? transform.position + moveDirection.normalized * 100f
                    : target - toTarget.normalized * InteractiveFloorConsts.EntityViewerStopDistance;

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

        // 指定ステージにモデルがない場合は直近下のステージにフォールバック
        // （中間モデルが未設定でも消えない）
        var resolved = Mathf.Min(stage, _stageModels.Length - 1);
        while (resolved > 0 && _stageModels[resolved] == null)
            resolved--;

        if (_stageModels[resolved] == null) return;

        for (var i = 0; i < _stageModels.Length; i++)
        {
            if (_stageModels[i] != null)
                _stageModels[i].SetActive(i == resolved);
        }

        StateMachine?.RefreshAnimator();
        StateMachine?.TriggerGrowUp(stage);
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

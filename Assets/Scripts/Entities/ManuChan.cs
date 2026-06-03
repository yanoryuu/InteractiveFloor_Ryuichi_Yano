using InteractiveFloor.Detect;
using UnityEngine;

public class ManuChan : Entity
{
    private float _currentYaw;
    private bool _hasYaw;

    public override void Move(Vector3 target)
    {
        var direction = target - transform.position;
        direction.y = 0f;
        var isRotating = false;

        if (direction.sqrMagnitude > Mathf.Epsilon)
        {
            var targetYaw = Quaternion.LookRotation(direction.normalized, Vector3.up).eulerAngles.y;

            if (!_hasYaw)
            {
                _currentYaw = transform.eulerAngles.y;
                _hasYaw = true;
            }

            isRotating = Mathf.Abs(Mathf.DeltaAngle(_currentYaw, targetYaw))
                > InteractiveFloorConsts.ManuChanRotatingMoveSlowdownAngleThreshold;

            _currentYaw = Mathf.MoveTowardsAngle(
                _currentYaw,
                targetYaw,
                InteractiveFloorConsts.EntityRotationSpeed * Time.deltaTime
            );

            var yawRotation = Quaternion.AngleAxis(_currentYaw, Vector3.up);
            var tiltRotation = Quaternion.AngleAxis(-40f, Vector3.right);
            transform.rotation = yawRotation * tiltRotation;
        }

        var moveSpeed = InteractiveFloorConsts.EntityMoveSpeed;
        if (isRotating)
        {
            moveSpeed *= InteractiveFloorConsts.ManuChanRotatingMoveSpeedMultiplier;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            moveSpeed * Time.deltaTime
        );
        Position = transform.position;
    }
    
    public override void GrowUpEntity(int stage){
        switch (stage)
        {
            case 0:
                Debug.Log("ManuChan has grown to Youth stage!");
                break;
            case 1:
                Debug.Log("ManuChan has grown to Adult stage!");
                break;
            case 2:
                Debug.Log("ManuChan has reached maximum growth stage!");
                break;
            default:                
                Debug.Log("ManuChan is in Baby stage.");
                break;
        }
    }
    
    public override void OnContactEnter(Entity other)
    {
        if (other == null || other == this)
        {
            return;
        }

        if (other.Type != EntityType.Manuchan)
        {
            return;
        }

        GrowthRate.Value = Mathf.Clamp01(
            GrowthRate.Value + InteractiveFloorConsts.ManuChanGrowthRateIncrease
        );
    }
}

using System.Collections.Generic;
using InteractiveFloor.Detect;
using UnityEngine;

/// <summary>
/// Boidsアルゴリズムによる群体シミュレーション。
/// 人(Viewer)がいない場合は分離・整列・結合の3ルールで群れを形成しながら自由に探索する。
/// 人が近づくと追いかけ、一定数以上の人がいる場合は逃げる。
/// </summary>
public class ManuChanFlockController : MonoBehaviour
{
    [SerializeField] private EntityManager _entityManager;

    private class FlockAgent
    {
        public ManuChan Entity;
        public Vector3 Velocity;
        public float WanderYaw;
    }

    private readonly List<FlockAgent> _agents = new();

    private void Awake()
    {
        if (_entityManager == null)
            _entityManager = FindObjectOfType<EntityManager>();
    }

    private void Start()
    {
        SpawnFlock();
    }

    private void SpawnFlock()
    {
        var hw = InteractiveFloorConsts.ManuChanMapHalfWidth * 0.8f;
        var hd = InteractiveFloorConsts.ManuChanMapHalfDepth * 0.8f;
        for (var i = 0; i < InteractiveFloorConsts.ManuChanMaxCount; i++)
        {
            var pos = new Vector3(
                Random.Range(-hw, hw),
                0f,
                Random.Range(-hd, hd));

            var entity = _entityManager.GetEntity<ManuChan>(EntityType.Manuchan, pos, 100);
            if (entity == null) continue;

            var yaw = Random.Range(0f, 360f);
            _agents.Add(new FlockAgent
            {
                Entity = entity,
                Velocity = YawToDir(yaw) * InteractiveFloorConsts.EntityMoveSpeed,
                WanderYaw = yaw
            });
        }
    }

    public void UpdateFlock(IReadOnlyList<Vector3> viewerPositions)
    {
        var viewerCount = viewerPositions.Count;
        var isGlobalFlee = viewerCount >= InteractiveFloorConsts.ManuChanFleeViewerCountThreshold;

        var fleeCentroid = Vector3.zero;
        if (isGlobalFlee && viewerCount > 0)
        {
            foreach (var vp in viewerPositions)
                fleeCentroid += vp;
            fleeCentroid /= viewerCount;
        }

        foreach (var agent in _agents)
        {
            if (agent.Entity == null || !agent.Entity.IsActive) continue;

            agent.Entity.ViewerCount = viewerCount;

            Vector3 desired;

            if (isGlobalFlee)
            {
                var fleeDir = agent.Entity.Position - fleeCentroid;
                fleeDir.y = 0f;
                desired = fleeDir.sqrMagnitude > Mathf.Epsilon
                    ? fleeDir.normalized * InteractiveFloorConsts.EntityMoveSpeed
                    : YawToDir(agent.WanderYaw) * InteractiveFloorConsts.EntityMoveSpeed;
                agent.WanderYaw = Mathf.Atan2(desired.x, desired.z) * Mathf.Rad2Deg;
            }
            else if (TryGetNearestViewer(agent.Entity.Position, viewerPositions, out var nearestViewer))
            {
                // 追いかけ: プレイヤー実座標を直接渡す。
                // Move() が ManuChanChaseKeepDistance だけ手前で停止するので重ならない。
                var chaseDir = nearestViewer - agent.Entity.Position;
                chaseDir.y = 0f;
                if (chaseDir.sqrMagnitude > Mathf.Epsilon)
                {
                    agent.Velocity = Vector3.Lerp(
                        agent.Velocity,
                        chaseDir.normalized * InteractiveFloorConsts.EntityMoveSpeed,
                        5f * Time.deltaTime);
                    agent.WanderYaw = Mathf.Atan2(chaseDir.x, chaseDir.z) * Mathf.Rad2Deg;
                }
                agent.Entity.Move(nearestViewer);
                continue;
            }
            else
            {
                desired = ComputeBoidsVelocity(agent);
            }

            desired += ComputeBoundaryForce(agent.Entity.Position);
            desired = Vector3.ClampMagnitude(desired, InteractiveFloorConsts.EntityMoveSpeed);

            agent.Velocity = Vector3.Lerp(agent.Velocity, desired, 5f * Time.deltaTime);

            if (agent.Velocity.sqrMagnitude < 0.001f)
                agent.Velocity = YawToDir(agent.WanderYaw) * 0.1f;

            agent.Entity.Move(agent.Entity.Position + agent.Velocity.normalized * 10f);
        }
    }

    private Vector3 ComputeBoidsVelocity(FlockAgent agent)
    {
        var sep = Vector3.zero;
        var ali = Vector3.zero;
        var cohPos = Vector3.zero;
        var sepCount = 0;
        var neighborCount = 0;

        foreach (var other in _agents)
        {
            if (ReferenceEquals(other, agent) || other.Entity == null || !other.Entity.IsActive)
                continue;

            var offset = agent.Entity.Position - other.Entity.Position;
            offset.y = 0f;
            var dist = offset.magnitude;

            if (dist < InteractiveFloorConsts.ManuChanSeparationRadius && dist > 0.001f)
            {
                sep += offset / (dist * dist);
                sepCount++;
            }

            if (dist < InteractiveFloorConsts.ManuChanFlockPerceptionRadius)
            {
                ali += other.Velocity;
                cohPos += other.Entity.Position;
                neighborCount++;
            }
        }

        if (neighborCount == 0)
        {
            // 仲間が視野内にいない場合はふらふらと徘徊
            agent.WanderYaw += Random.Range(
                -InteractiveFloorConsts.ManuChanWanderAngleRate,
                InteractiveFloorConsts.ManuChanWanderAngleRate) * Time.deltaTime;
            return YawToDir(agent.WanderYaw);
        }

        // 群れモードから徘徊へ遷移した際の方向を滑らかにするため現在速度方向を保持
        if (agent.Velocity.sqrMagnitude > 0.001f)
            agent.WanderYaw = Mathf.Atan2(agent.Velocity.x, agent.Velocity.z) * Mathf.Rad2Deg;

        var result = agent.Velocity;

        // 分離: 近すぎる仲間から離れる
        if (sepCount > 0)
            result += sep.normalized * InteractiveFloorConsts.ManuChanSeparationWeight;

        // 整列: 周囲の仲間の平均移動方向に揃える
        var avgVel = ali / neighborCount;
        if (avgVel.sqrMagnitude > Mathf.Epsilon)
            result += avgVel.normalized * InteractiveFloorConsts.ManuChanAlignmentWeight;

        // 結合: 周囲の仲間の重心方向へ向かう
        var cohDir = cohPos / neighborCount - agent.Entity.Position;
        cohDir.y = 0f;
        if (cohDir.sqrMagnitude > Mathf.Epsilon)
            result += cohDir.normalized * InteractiveFloorConsts.ManuChanCohesionWeight;

        return result;
    }

    private bool TryGetNearestViewer(Vector3 position, IReadOnlyList<Vector3> viewerPositions, out Vector3 nearest)
    {
        nearest = Vector3.zero;
        var minDist = InteractiveFloorConsts.ManuChanPlayerDetectRadius;
        var found = false;

        foreach (var vp in viewerPositions)
        {
            var d = Vector3.Distance(position, vp);
            if (d >= minDist) continue;
            minDist = d;
            nearest = vp;
            found = true;
        }

        return found;
    }

    private static Vector3 ComputeBoundaryForce(Vector3 position)
    {
        var force = Vector3.zero;
        var m = InteractiveFloorConsts.ManuChanBoundaryMargin;
        var w = InteractiveFloorConsts.ManuChanBoundaryWeight;
        var hw = InteractiveFloorConsts.ManuChanMapHalfWidth;
        var hd = InteractiveFloorConsts.ManuChanMapHalfDepth;

        var dxLeft  = position.x - (-hw);
        var dxRight = hw - position.x;
        var dzBack  = position.z - (-hd);
        var dzFront = hd - position.z;

        if (dxLeft  < m) force.x += w * (1f - dxLeft  / m);
        if (dxRight < m) force.x -= w * (1f - dxRight / m);
        if (dzBack  < m) force.z += w * (1f - dzBack  / m);
        if (dzFront < m) force.z -= w * (1f - dzFront / m);

        return force;
    }

    private static Vector3 YawToDir(float yawDeg)
    {
        var rad = yawDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }
}

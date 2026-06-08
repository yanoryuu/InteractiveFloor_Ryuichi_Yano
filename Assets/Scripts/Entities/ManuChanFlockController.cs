using System.Collections.Generic;
using InteractiveFloor.Detect;
using UnityEngine;

/// <summary>
/// Boidsアルゴリズムによる群体シミュレーション。
///
/// パフォーマンス設計:
///   - Boids計算(O(N²)) は ManuChanBoidsUpdateInterval フレームに1回だけ実行
///   - 距離比較は sqrMagnitude を使い sqrt を排除
///   - 逃げ/追いかけの判定は毎フレーム実行（応答性を保つため）
///   - 境界反発は毎フレーム適用
/// </summary>
public class ManuChanFlockController : MonoBehaviour
{
    [SerializeField] private EntityManager _entityManager;

    [Header("生存エリア")]
    [Tooltip("生存エリアの中心座標 (XZ平面)")]
    [SerializeField] private Vector3 _areaCenter = Vector3.zero;
    [Tooltip("生存エリアの半幅 (X方向, Z方向)")]
    [SerializeField] private Vector2 _areaHalfExtents = new Vector2(
        InteractiveFloorConsts.ManuChanMapHalfWidth,
        InteractiveFloorConsts.ManuChanMapHalfDepth);

    [Header("餌(エサ)")]
    [Tooltip("マップ上に出現する餌のPrefab。Viewerが回収すると、くっついているマヌちゃんが次の段階へ進化する。")]
    [SerializeField] private GameObject _foodPrefab;
    [Tooltip("Viewerが餌を回収した瞬間に再生するパーティクル（円形に輪が広がる演出など）。回収位置に生成され、再生後に自動破棄される。")]
    [SerializeField] private ParticleSystem _foodPickupEffect;

    private class FlockAgent
    {
        public ManuChan Entity;
        public Vector3 Velocity;
        public Vector3 DesiredVelocity;
        public float WanderYaw;
        public bool IsChasing;
        public Vector3 ChasedTarget;
    }

    private readonly List<FlockAgent> _agents = new();
    private readonly List<GameObject> _foods = new();
    private int _frameIndex;
    private float _boidsDtAccum;

    private void Awake()
    {
        if (_entityManager == null)
            _entityManager = FindAnyObjectByType<EntityManager>();
    }

    private void Start()
    {
        SpawnFlock();
    }

    private void SpawnFlock()
    {
        var hw = _areaHalfExtents.x * 0.8f;
        var hd = _areaHalfExtents.y * 0.8f;
        for (var i = 0; i < InteractiveFloorConsts.ManuChanMaxCount; i++)
        {
            var pos = new Vector3(
                _areaCenter.x + Random.Range(-hw, hw),
                _areaCenter.y,
                _areaCenter.z + Random.Range(-hd, hd));

            var entity = _entityManager.GetEntity<ManuChan>(EntityType.Manuchan, pos, 100);
            if (entity == null) continue;

            var yaw = Random.Range(0f, 360f);
            var initVel = YawToDir(yaw) * InteractiveFloorConsts.EntityMoveSpeed;
            _agents.Add(new FlockAgent
            {
                Entity = entity,
                Velocity = initVel,
                DesiredVelocity = initVel,
                WanderYaw = yaw
            });
        }
    }

    public void UpdateFlock(IReadOnlyList<Vector3> viewerPositions)
    {
        UpdateFood(viewerPositions);

        var viewerCount = viewerPositions.Count;
        var isGlobalFlee = viewerCount >= InteractiveFloorConsts.ManuChanFleeViewerCountThreshold;

        var fleeCentroid = Vector3.zero;
        if (isGlobalFlee && viewerCount > 0)
        {
            foreach (var vp in viewerPositions) fleeCentroid += vp;
            fleeCentroid /= viewerCount;
        }

        var sep = InteractiveFloorConsts.ManuChanSeparationRadius;
        var perc = InteractiveFloorConsts.ManuChanFlockPerceptionRadius;
        var det = InteractiveFloorConsts.ManuChanPlayerDetectRadius;
        var sepRadSq    = sep  * sep;
        var percRadSq   = perc * perc;
        var detectRadSq = det  * det;
        var speed       = InteractiveFloorConsts.EntityMoveSpeed;

        _boidsDtAccum += Time.deltaTime;
        var recalcBoids = (++_frameIndex % InteractiveFloorConsts.ManuChanBoidsUpdateInterval == 0);
        var boidsDt = 0f;
        if (recalcBoids) { boidsDt = _boidsDtAccum; _boidsDtAccum = 0f; }

        foreach (var agent in _agents)
        {
            if (agent.Entity == null || !agent.Entity.IsActive) continue;

            var pos = agent.Entity.Position;

            // 成人個体の退場判定:
            //   Viewerにくっついている（検出半径内にViewerがいる）間は退場しない。
            //   Viewerが消えたら退場を開始する。Viewerが戻ってくれば再び追従する。
            //   全体逃げモード中はViewerが大勢いるので退場せず、逃げ挙動に従う。
            if (agent.Entity.IsAdult)
            {
                var hasNearbyViewer = isGlobalFlee
                    || TryGetNearestViewer(pos, viewerPositions, detectRadSq, out _);
                agent.Entity.SetLeaving(!hasNearbyViewer);
            }

            // 退場中の個体は専用の処理にまかせる
            if (agent.Entity.IsLeaving)
            {
                UpdateLeavingAgent(agent);
                continue;
            }

            agent.Entity.ViewerCount = viewerCount;

            // ---- 行動決定 ----
            if (isGlobalFlee)
            {
                agent.IsChasing = false;
                var fleeDir = pos - fleeCentroid;
                fleeDir.y = 0f;
                agent.DesiredVelocity = fleeDir.sqrMagnitude > Mathf.Epsilon
                    ? fleeDir.normalized * speed
                    : YawToDir(agent.WanderYaw) * speed;
                if (agent.Velocity.sqrMagnitude > 0.001f)
                    agent.WanderYaw = Mathf.Atan2(agent.Velocity.x, agent.Velocity.z) * Mathf.Rad2Deg;
            }
            else if (TryGetNearestViewer(pos, viewerPositions, detectRadSq, out var nearest))
            {
                agent.IsChasing = true;
                agent.ChasedTarget = nearest;
                var chaseDir = nearest - pos;
                chaseDir.y = 0f;
                if (chaseDir.sqrMagnitude > Mathf.Epsilon)
                {
                    agent.DesiredVelocity = chaseDir.normalized * speed;
                    agent.WanderYaw = Mathf.Atan2(chaseDir.x, chaseDir.z) * Mathf.Rad2Deg;
                }
            }
            else
            {
                agent.IsChasing = false;
                if (recalcBoids)
                    agent.DesiredVelocity = ComputeBoidsVelocity(agent, boidsDt, sepRadSq, percRadSq);
            }

            // ---- 移動 (毎フレーム) ----
            var velocityTarget = agent.DesiredVelocity + ComputeBoundaryForce(pos);
            velocityTarget = Vector3.ClampMagnitude(velocityTarget, speed);

            if (agent.IsChasing)
            {
                var chaseDir = agent.ChasedTarget - pos;
                chaseDir.y = 0f;
                if (chaseDir.sqrMagnitude > Mathf.Epsilon)
                    agent.Velocity = Vector3.Lerp(agent.Velocity, chaseDir.normalized * speed, 5f * Time.deltaTime);
                agent.Entity.Move(agent.ChasedTarget);
            }
            else
            {
                agent.Velocity = Vector3.Lerp(agent.Velocity, velocityTarget, 5f * Time.deltaTime);
                if (agent.Velocity.sqrMagnitude < 0.001f)
                    agent.Velocity = YawToDir(agent.WanderYaw) * 0.1f;
                agent.Entity.Move(pos + agent.Velocity.normalized * 10f);
            }
        }

        ResolveOverlaps();
    }

    /// <summary>
    /// 成人して退場中の個体を動かす。境界反発を無視してエリア外まで歩かせ、
    /// 十分に出たタイミングで Die() を呼ぶ。
    /// </summary>
    private void UpdateLeavingAgent(FlockAgent agent)
    {
        agent.Entity.ViewerCount = 0; // 逃げフラグを立てない

        var pos = agent.Entity.Position;

        // 現在の速度方向を優先。止まっていたらエリア中心から離れる方向へ
        var exitDir = agent.Velocity;
        exitDir.y = 0f;
        if (exitDir.sqrMagnitude < 0.01f)
        {
            exitDir = pos - _areaCenter;
            exitDir.y = 0f;
        }
        exitDir = exitDir.sqrMagnitude > Mathf.Epsilon ? exitDir.normalized : Vector3.forward;

        var speed = InteractiveFloorConsts.EntityMoveSpeed;
        agent.Velocity = Vector3.Lerp(agent.Velocity, exitDir * speed, 3f * Time.deltaTime);
        agent.Entity.Move(pos + exitDir * 10f);

        // エリアを1m超えたら消滅 → 新しい個体をリスポーン
        var local = pos - _areaCenter;
        if (Mathf.Abs(local.x) > _areaHalfExtents.x + 1f ||
            Mathf.Abs(local.z) > _areaHalfExtents.y + 1f)
        {
            agent.Entity.Die();
            RespawnAgent(agent);
        }
    }

    /// <summary>
    /// Die() 直後に同じ FlockAgent スロットへ新しい個体を割り当てる。
    /// プールに戻ったばかりなので GetEntity は必ず成功する。
    /// </summary>
    private void RespawnAgent(FlockAgent agent)
    {
        var hw = _areaHalfExtents.x * 0.8f;
        var hd = _areaHalfExtents.y * 0.8f;
        var pos = new Vector3(
            _areaCenter.x + Random.Range(-hw, hw),
            _areaCenter.y,
            _areaCenter.z + Random.Range(-hd, hd));

        var entity = _entityManager.GetEntity<ManuChan>(EntityType.Manuchan, pos, 100);
        if (entity == null) return;

        var yaw = Random.Range(0f, 360f);
        agent.Entity = entity;
        agent.Velocity = YawToDir(yaw) * InteractiveFloorConsts.EntityMoveSpeed;
        agent.DesiredVelocity = agent.Velocity;
        agent.WanderYaw = yaw;
        agent.IsChasing = false;
    }

    // ---- 餌(エサ) ----

    /// <summary>
    /// 餌をマップ上にランダム出現させ、Viewerによる回収判定を行う。
    /// 出現範囲・出現確率は InteractiveFloorConsts（定数エディタ）で調整できる。
    /// </summary>
    private void UpdateFood(IReadOnlyList<Vector3> viewerPositions)
    {
        // ---- 出現 ----
        // 出現確率は「1秒あたり」の確率として扱い、フレームレート非依存にする。
        if (_foodPrefab != null
            && _foods.Count < InteractiveFloorConsts.ManuChanFoodMaxCount
            && Random.value < InteractiveFloorConsts.ManuChanFoodSpawnProbability * Time.deltaTime)
        {
            SpawnFood();
        }

        // ---- 回収判定 ----
        if (viewerPositions.Count == 0) return;

        var pickupSq = InteractiveFloorConsts.ManuChanFoodPickupDistance
                     * InteractiveFloorConsts.ManuChanFoodPickupDistance;

        for (var i = _foods.Count - 1; i >= 0; i--)
        {
            var food = _foods[i];
            if (food == null)
            {
                _foods.RemoveAt(i);
                continue;
            }

            var foodPos = food.transform.position;
            foreach (var vp in viewerPositions)
            {
                var offset = vp - foodPos;
                offset.y = 0f;
                if (offset.sqrMagnitude > pickupSq) continue;

                // Viewerが餌を回収 → 演出を再生し、くっついているマヌちゃんを一気に進化させる
                PlayPickupEffect(foodPos);
                EvolveAttachedManuChan(vp);
                Destroy(food);
                _foods.RemoveAt(i);
                break;
            }
        }
    }

    /// <summary>出現範囲内のランダムな位置に餌を1つ生成する。</summary>
    private void SpawnFood()
    {
        var hw = InteractiveFloorConsts.ManuChanFoodSpawnHalfWidth;
        var hd = InteractiveFloorConsts.ManuChanFoodSpawnHalfDepth;
        var pos = new Vector3(
            _areaCenter.x + Random.Range(-hw, hw),
            _areaCenter.y,
            _areaCenter.z + Random.Range(-hd, hd));

        var food = Instantiate(_foodPrefab, pos, Quaternion.identity, transform);
        _foods.Add(food);
    }

    /// <summary>
    /// 餌の回収位置にパーティクルを生成して再生する。再生時間が経過したら自動破棄する。
    /// </summary>
    private void PlayPickupEffect(Vector3 position)
    {
        if (_foodPickupEffect == null) return;

        var effect = Instantiate(_foodPickupEffect, position, Quaternion.identity, transform);
        effect.Play();

        // パーティクル本体＋子要素の寿命を考慮して破棄するまでの時間を求める
        var main = effect.main;
        var lifetime = main.duration + main.startLifetime.constantMax;
        Destroy(effect.gameObject, lifetime);
    }

    /// <summary>
    /// 指定Viewerの近く（ManuChanFoodAttachDistance以内）にいるマヌちゃんを
    /// 一気に次の成長段階へ進化させる。退場中の個体は対象外。
    /// </summary>
    private void EvolveAttachedManuChan(Vector3 viewerPos)
    {
        var attachSq = InteractiveFloorConsts.ManuChanFoodAttachDistance
                     * InteractiveFloorConsts.ManuChanFoodAttachDistance;

        foreach (var agent in _agents)
        {
            if (agent.Entity == null || !agent.Entity.IsActive || agent.Entity.IsLeaving) continue;

            var offset = agent.Entity.Position - viewerPos;
            offset.y = 0f;
            if (offset.sqrMagnitude <= attachSq)
                agent.Entity.AdvanceGrowthStage();
        }
    }

    /// <summary>
    /// 位置ベースのハード分離パス。
    /// Boids のソフト分離では防ぎきれない重なりをフレームごとに強制解消する。
    /// 2回反復することで密集時も確実に収束する。
    /// </summary>
    private void ResolveOverlaps()
    {
        var minDist = InteractiveFloorConsts.ManuChanMinEntityDistance;
        var minDistSq = minDist * minDist;

        for (var iter = 0; iter < 2; iter++)
        {
            for (var i = 0; i < _agents.Count; i++)
            {
                var a = _agents[i];
                if (a.Entity == null || !a.Entity.IsActive) continue;
                for (var j = i + 1; j < _agents.Count; j++)
                {
                    var b = _agents[j];
                    if (b.Entity == null || !b.Entity.IsActive) continue;
                    // 両方フロック中はBoids分離に任せてハード分離をスキップ
                    if (!a.IsChasing && !b.IsChasing) continue;

                    var offset = a.Entity.Position - b.Entity.Position;
                    offset.y = 0f;
                    var distSq = offset.sqrMagnitude;

                    if (distSq >= minDistSq || distSq < 0.0001f) continue;

                    // 重なり量を半分ずつ両者に分配して押し退ける
                    var dist = Mathf.Sqrt(distSq);
                    var half = (minDist - dist) * 0.5f;
                    var dir = offset / dist;
                    var push = new Vector3(dir.x * half, 0f, dir.z * half);

                    a.Entity.transform.position += push;
                    b.Entity.transform.position -= push;
                    a.Entity.Position = a.Entity.transform.position;
                    b.Entity.Position = b.Entity.transform.position;
                }
            }
        }
    }

    private Vector3 ComputeBoidsVelocity(FlockAgent agent, float dt, float sepRadSq, float percRadSq)
    {
        var sep = Vector3.zero;
        var ali = Vector3.zero;
        var cohPos = Vector3.zero;
        var sepCount = 0;
        var neighborCount = 0;
        var myPos = agent.Entity.Position;

        foreach (var other in _agents)
        {
            if (ReferenceEquals(other, agent) || other.Entity == null || !other.Entity.IsActive)
                continue;

            var offset = myPos - other.Entity.Position;
            offset.y = 0f;
            var distSq = offset.sqrMagnitude;

            if (distSq < sepRadSq && distSq > 0.000001f)
            {
                sep += offset / distSq;
                sepCount++;
            }

            if (distSq < percRadSq)
            {
                ali += other.Velocity;
                cohPos += other.Entity.Position;
                neighborCount++;
            }
        }

        if (neighborCount == 0)
        {
            agent.WanderYaw += Random.Range(
                -InteractiveFloorConsts.ManuChanWanderAngleRate,
                InteractiveFloorConsts.ManuChanWanderAngleRate) * dt;
            return YawToDir(agent.WanderYaw);
        }

        if (agent.Velocity.sqrMagnitude > 0.001f)
            agent.WanderYaw = Mathf.Atan2(agent.Velocity.x, agent.Velocity.z) * Mathf.Rad2Deg;

        var result = agent.Velocity;

        if (sepCount > 0)
            result += sep.normalized * InteractiveFloorConsts.ManuChanSeparationWeight;

        var avgVel = ali / neighborCount;
        if (avgVel.sqrMagnitude > Mathf.Epsilon)
            result += avgVel.normalized * InteractiveFloorConsts.ManuChanAlignmentWeight;

        var cohDir = cohPos / neighborCount - myPos;
        cohDir.y = 0f;
        if (cohDir.sqrMagnitude > Mathf.Epsilon)
            result += cohDir.normalized * InteractiveFloorConsts.ManuChanCohesionWeight;

        return result;
    }

    /// <summary>エリア境界に近づくと中心方向へ反発力を加える。</summary>
    private Vector3 ComputeBoundaryForce(Vector3 position)
    {
        var force = Vector3.zero;
        var m  = InteractiveFloorConsts.ManuChanBoundaryMargin;
        var w  = InteractiveFloorConsts.ManuChanBoundaryWeight;
        var hw = _areaHalfExtents.x;
        var hd = _areaHalfExtents.y;
        var cx = _areaCenter.x;
        var cz = _areaCenter.z;

        var dxLeft  = position.x - (cx - hw);
        var dxRight = (cx + hw) - position.x;
        var dzBack  = position.z - (cz - hd);
        var dzFront = (cz + hd) - position.z;

        if (dxLeft  < m) force.x += w * (1f - dxLeft  / m);
        if (dxRight < m) force.x -= w * (1f - dxRight / m);
        if (dzBack  < m) force.z += w * (1f - dzBack  / m);
        if (dzFront < m) force.z -= w * (1f - dzFront / m);

        return force;
    }

    private static bool TryGetNearestViewer(
        Vector3 pos, IReadOnlyList<Vector3> viewerPositions, float detectRadSq, out Vector3 nearest)
    {
        nearest = Vector3.zero;
        var minSq = detectRadSq;
        var found = false;

        foreach (var vp in viewerPositions)
        {
            var offset = vp - pos;
            offset.y = 0f;
            var dSq = offset.sqrMagnitude;
            if (dSq >= minSq) continue;
            minSq = dSq;
            nearest = vp;
            found = true;
        }

        return found;
    }

    // ---- Gizmos ----

    private void OnDrawGizmos()
    {
        DrawAreaGizmo(false);
    }

    private void OnDrawGizmosSelected()
    {
        DrawAreaGizmo(true);
    }

    private void DrawAreaGizmo(bool selected)
    {
        var hw = _areaHalfExtents.x;
        var hd = _areaHalfExtents.y;
        var m  = InteractiveFloorConsts.ManuChanBoundaryMargin;
        var center = _areaCenter;

        // 生存エリア全体（緑）
        Gizmos.color = selected ? new Color(0.2f, 1f, 0.3f, 1f) : new Color(0.2f, 1f, 0.3f, 0.5f);
        DrawFlatRect(center, hw, hd);

        // 境界反発が始まる内側ライン（黄）
        if (hw > m && hd > m)
        {
            Gizmos.color = selected ? new Color(1f, 0.85f, 0f, 1f) : new Color(1f, 0.85f, 0f, 0.4f);
            DrawFlatRect(center, hw - m, hd - m);
        }

        // 選択時は半透明フィル
        if (selected)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.06f);
            Gizmos.DrawCube(center, new Vector3(hw * 2f, 0.02f, hd * 2f));
        }

        // 餌の出現範囲（マゼンタ）
        var fhw = InteractiveFloorConsts.ManuChanFoodSpawnHalfWidth;
        var fhd = InteractiveFloorConsts.ManuChanFoodSpawnHalfDepth;
        Gizmos.color = selected ? new Color(1f, 0.2f, 0.8f, 1f) : new Color(1f, 0.2f, 0.8f, 0.4f);
        DrawFlatRect(center, fhw, fhd);
    }

    private static void DrawFlatRect(Vector3 center, float hw, float hd)
    {
        var y = center.y;
        var x0 = center.x - hw;
        var x1 = center.x + hw;
        var z0 = center.z - hd;
        var z1 = center.z + hd;

        Gizmos.DrawLine(new Vector3(x0, y, z0), new Vector3(x1, y, z0));
        Gizmos.DrawLine(new Vector3(x1, y, z0), new Vector3(x1, y, z1));
        Gizmos.DrawLine(new Vector3(x1, y, z1), new Vector3(x0, y, z1));
        Gizmos.DrawLine(new Vector3(x0, y, z1), new Vector3(x0, y, z0));
    }

    private static Vector3 YawToDir(float yawDeg)
    {
        var rad = yawDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }
}

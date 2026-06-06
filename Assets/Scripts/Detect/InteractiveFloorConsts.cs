using UnityEngine;

namespace InteractiveFloor.Detect
{
    public class InteractiveFloorConsts
    {
        public const float MatchingThreshold = 0.2f;
        public const float EntityMoveSpeed = 0.5f;
        public const float EntityRotationSpeed = 280f;
        public const float ManuChanRotatingMoveSlowdownAngleThreshold = 15f;
        public const float ManuChanRotatingMoveSpeedMultiplier = 0.35f;
        public const float EntityViewerStopDistance = 1f;
        public const float EntityContactDistance = 0.5f;
        public const float ManuChanGrowthRateIncrease = 0.05f;
        public const float ManuChanMatchingDistance = 0.5f;
        public const float ManuChanWalkingGrowthRate = 0.02f;
        public const float ManuChanContactStayGrowthRate = 0.05f;
        public const int ManuChanFleeViewerCountThreshold = 4;

        // マップ範囲
        public const float ManuChanMapHalfWidth = 5f;
        public const float ManuChanMapHalfDepth = 3.5f;

        // 群れ(Boids)システム
        public const int ManuChanMaxCount = 20;
        public const float ManuChanFlockPerceptionRadius = 1f;
        public const float ManuChanSeparationRadius = 1f;
        public const float ManuChanSeparationWeight = 1.5f;
        public const float ManuChanAlignmentWeight = 1f;
        public const float ManuChanCohesionWeight = 0.8f;
        public const float ManuChanBoundaryWeight = 2f;
        public const float ManuChanBoundaryMargin = 0.8f;
        public const float ManuChanWanderAngleRate = 60f;
        public const float ManuChanPlayerDetectRadius = 1f;
        public const float ManuChanChaseKeepDistance = 0.3f;
        public const int ManuChanBoidsUpdateInterval = 3;
        public const float ManuChanMinEntityDistance = 0.2f;
    }
}

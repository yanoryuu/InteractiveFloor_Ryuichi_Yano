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
        public const float EntityViewerStopDistance = 0.3f;
        public const float EntityContactDistance = 0.5f;
        public const float ManuChanGrowthRateIncrease = 0.05f;
        public const float ManuChanMatchingDistance = 0.5f;
        public const float ManuChanWalkingGrowthRate = 0.1f;    // 歩行中の成長率/秒
        public const float ManuChanContactStayGrowthRate = 0.1f; // 接触継続中の成長率/秒
        public const int ManuChanFleeViewerCountThreshold = 4;    // これ以上の人数で逃げる
    }
}

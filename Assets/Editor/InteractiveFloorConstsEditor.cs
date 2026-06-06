using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace InteractiveFloor.Editor
{
    public class InteractiveFloorConstsEditor : EditorWindow
    {
        private const string ConstsFilePath = "Assets/Scripts/Detect/InteractiveFloorConsts.cs";

        // 検出設定
        private float matchingThreshold;

        // エンティティ移動
        private float entityMoveSpeed;
        private float entityRotationSpeed;

        // マヌちゃん挙動
        private float manuChanRotatingMoveSlowdownAngleThreshold;
        private float manuChanRotatingMoveSpeedMultiplier;
        private float manuChanGrowthRateIncrease;
        private float manuChanMatchingDistance;
        private float manuChanWalkingGrowthRate;
        private float manuChanContactStayGrowthRate;

        // 人数制限・距離
        private int manuChanFleeViewerCountThreshold;
        private float entityViewerStopDistance;
        private float entityContactDistance;

        // マップ範囲
        private float manuChanMapHalfWidth;
        private float manuChanMapHalfDepth;

        // 群れ(Boids)システム
        private int manuChanMaxCount;
        private float manuChanFlockPerceptionRadius;
        private float manuChanSeparationRadius;
        private float manuChanSeparationWeight;
        private float manuChanAlignmentWeight;
        private float manuChanCohesionWeight;
        private float manuChanBoundaryWeight;
        private float manuChanBoundaryMargin;
        private float manuChanWanderAngleRate;
        private float manuChanPlayerDetectRadius;
        private float manuChanChaseKeepDistance;
        private int manuChanBoidsUpdateInterval;
        private float manuChanMinEntityDistance;

        private Vector2 _scrollPos;

        [MenuItem("InteractiveFloor/定数エディタ")]
        public static void Open()
        {
            var window = GetWindow<InteractiveFloorConstsEditor>("定数エディタ");
            window.minSize = new Vector2(360, 400);
            window.LoadValues();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("InteractiveFloor 定数設定", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("検出設定", EditorStyles.miniBoldLabel);
            matchingThreshold = EditorGUILayout.FloatField("マッチング閾値", matchingThreshold);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("エンティティ移動", EditorStyles.miniBoldLabel);
            entityMoveSpeed = EditorGUILayout.FloatField("移動速度", entityMoveSpeed);
            entityRotationSpeed = EditorGUILayout.FloatField("回転速度", entityRotationSpeed);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("マヌちゃん挙動", EditorStyles.miniBoldLabel);
            manuChanRotatingMoveSlowdownAngleThreshold = EditorGUILayout.FloatField("減速角度閾値", manuChanRotatingMoveSlowdownAngleThreshold);
            manuChanRotatingMoveSpeedMultiplier = EditorGUILayout.FloatField("回転移動速度倍率", manuChanRotatingMoveSpeedMultiplier);
            manuChanGrowthRateIncrease = EditorGUILayout.FloatField("接触時成長量(一回)", manuChanGrowthRateIncrease);
            manuChanMatchingDistance = EditorGUILayout.FloatField("マッチング距離", manuChanMatchingDistance);
            manuChanWalkingGrowthRate = EditorGUILayout.FloatField("歩行中成長率/秒", manuChanWalkingGrowthRate);
            manuChanContactStayGrowthRate = EditorGUILayout.FloatField("接触継続成長率/秒", manuChanContactStayGrowthRate);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("人数制限・距離設定", EditorStyles.miniBoldLabel);
            manuChanFleeViewerCountThreshold = EditorGUILayout.IntField("逃げる人数閾値", manuChanFleeViewerCountThreshold);
            entityViewerStopDistance = EditorGUILayout.FloatField("Viewer停止距離", entityViewerStopDistance);
            entityContactDistance = EditorGUILayout.FloatField("接触判定距離", entityContactDistance);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("マップ範囲", EditorStyles.miniBoldLabel);
            manuChanMapHalfWidth = EditorGUILayout.FloatField("マップ半幅 X(m)", manuChanMapHalfWidth);
            manuChanMapHalfDepth = EditorGUILayout.FloatField("マップ半幅 Z(m)", manuChanMapHalfDepth);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("群れ(Boids)システム", EditorStyles.miniBoldLabel);
            manuChanMaxCount = EditorGUILayout.IntField("マップ内最大数", manuChanMaxCount);
            manuChanFlockPerceptionRadius = EditorGUILayout.FloatField("群れ認識半径", manuChanFlockPerceptionRadius);
            manuChanSeparationRadius = EditorGUILayout.FloatField("分離半径", manuChanSeparationRadius);
            manuChanSeparationWeight = EditorGUILayout.FloatField("分離の強さ", manuChanSeparationWeight);
            manuChanAlignmentWeight = EditorGUILayout.FloatField("整列の強さ", manuChanAlignmentWeight);
            manuChanCohesionWeight = EditorGUILayout.FloatField("結合の強さ", manuChanCohesionWeight);
            manuChanBoundaryWeight = EditorGUILayout.FloatField("境界反発の強さ", manuChanBoundaryWeight);
            manuChanBoundaryMargin = EditorGUILayout.FloatField("境界余白距離", manuChanBoundaryMargin);
            manuChanWanderAngleRate = EditorGUILayout.FloatField("徘徊角速度(度/秒)", manuChanWanderAngleRate);
            manuChanPlayerDetectRadius = EditorGUILayout.FloatField("プレイヤー検出半径", manuChanPlayerDetectRadius);
            manuChanChaseKeepDistance = EditorGUILayout.FloatField("プレイヤー停止距離", manuChanChaseKeepDistance);
            manuChanBoidsUpdateInterval = EditorGUILayout.IntField("Boids更新間隔(フレーム)", manuChanBoidsUpdateInterval);
            manuChanMinEntityDistance = EditorGUILayout.FloatField("エンティティ最小距離", manuChanMinEntityDistance);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("保存", GUILayout.Height(28)))
                    SaveValues();
                if (GUILayout.Button("リロード", GUILayout.Height(28)))
                    LoadValues();
            }
        }

        private void LoadValues()
        {
            var fullPath = Path.Combine(Application.dataPath, "../", ConstsFilePath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"定数ファイルが見つかりません: {fullPath}");
                return;
            }

            var text = File.ReadAllText(fullPath);

            matchingThreshold = ParseFloat(text, "MatchingThreshold");
            entityMoveSpeed = ParseFloat(text, "EntityMoveSpeed");
            entityRotationSpeed = ParseFloat(text, "EntityRotationSpeed");
            manuChanRotatingMoveSlowdownAngleThreshold = ParseFloat(text, "ManuChanRotatingMoveSlowdownAngleThreshold");
            manuChanRotatingMoveSpeedMultiplier = ParseFloat(text, "ManuChanRotatingMoveSpeedMultiplier");
            entityViewerStopDistance = ParseFloat(text, "EntityViewerStopDistance");
            entityContactDistance = ParseFloat(text, "EntityContactDistance");
            manuChanGrowthRateIncrease = ParseFloat(text, "ManuChanGrowthRateIncrease");
            manuChanMatchingDistance = ParseFloat(text, "ManuChanMatchingDistance");
            manuChanWalkingGrowthRate = ParseFloat(text, "ManuChanWalkingGrowthRate");
            manuChanContactStayGrowthRate = ParseFloat(text, "ManuChanContactStayGrowthRate");
            manuChanFleeViewerCountThreshold = ParseInt(text, "ManuChanFleeViewerCountThreshold");

            manuChanMapHalfWidth = ParseFloat(text, "ManuChanMapHalfWidth");
            manuChanMapHalfDepth = ParseFloat(text, "ManuChanMapHalfDepth");

            manuChanMaxCount = ParseInt(text, "ManuChanMaxCount");
            manuChanFlockPerceptionRadius = ParseFloat(text, "ManuChanFlockPerceptionRadius");
            manuChanSeparationRadius = ParseFloat(text, "ManuChanSeparationRadius");
            manuChanSeparationWeight = ParseFloat(text, "ManuChanSeparationWeight");
            manuChanAlignmentWeight = ParseFloat(text, "ManuChanAlignmentWeight");
            manuChanCohesionWeight = ParseFloat(text, "ManuChanCohesionWeight");
            manuChanBoundaryWeight = ParseFloat(text, "ManuChanBoundaryWeight");
            manuChanBoundaryMargin = ParseFloat(text, "ManuChanBoundaryMargin");
            manuChanWanderAngleRate = ParseFloat(text, "ManuChanWanderAngleRate");
            manuChanPlayerDetectRadius = ParseFloat(text, "ManuChanPlayerDetectRadius");
            manuChanChaseKeepDistance = ParseFloat(text, "ManuChanChaseKeepDistance");
            manuChanBoidsUpdateInterval = ParseInt(text, "ManuChanBoidsUpdateInterval");
            manuChanMinEntityDistance = ParseFloat(text, "ManuChanMinEntityDistance");

            Repaint();
        }

        private void SaveValues()
        {
            var fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../", ConstsFilePath));
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"定数ファイルが見つかりません: {fullPath}");
                return;
            }

            var text = File.ReadAllText(fullPath);

            text = ReplaceFloat(text, "MatchingThreshold", matchingThreshold);
            text = ReplaceFloat(text, "EntityMoveSpeed", entityMoveSpeed);
            text = ReplaceFloat(text, "EntityRotationSpeed", entityRotationSpeed);
            text = ReplaceFloat(text, "ManuChanRotatingMoveSlowdownAngleThreshold", manuChanRotatingMoveSlowdownAngleThreshold);
            text = ReplaceFloat(text, "ManuChanRotatingMoveSpeedMultiplier", manuChanRotatingMoveSpeedMultiplier);
            text = ReplaceFloat(text, "EntityViewerStopDistance", entityViewerStopDistance);
            text = ReplaceFloat(text, "EntityContactDistance", entityContactDistance);
            text = ReplaceFloat(text, "ManuChanGrowthRateIncrease", manuChanGrowthRateIncrease);
            text = ReplaceFloat(text, "ManuChanMatchingDistance", manuChanMatchingDistance);
            text = ReplaceFloat(text, "ManuChanWalkingGrowthRate", manuChanWalkingGrowthRate);
            text = ReplaceFloat(text, "ManuChanContactStayGrowthRate", manuChanContactStayGrowthRate);
            text = ReplaceInt(text, "ManuChanFleeViewerCountThreshold", manuChanFleeViewerCountThreshold);

            text = ReplaceFloat(text, "ManuChanMapHalfWidth", manuChanMapHalfWidth);
            text = ReplaceFloat(text, "ManuChanMapHalfDepth", manuChanMapHalfDepth);

            text = ReplaceInt(text, "ManuChanMaxCount", manuChanMaxCount);
            text = ReplaceFloat(text, "ManuChanFlockPerceptionRadius", manuChanFlockPerceptionRadius);
            text = ReplaceFloat(text, "ManuChanSeparationRadius", manuChanSeparationRadius);
            text = ReplaceFloat(text, "ManuChanSeparationWeight", manuChanSeparationWeight);
            text = ReplaceFloat(text, "ManuChanAlignmentWeight", manuChanAlignmentWeight);
            text = ReplaceFloat(text, "ManuChanCohesionWeight", manuChanCohesionWeight);
            text = ReplaceFloat(text, "ManuChanBoundaryWeight", manuChanBoundaryWeight);
            text = ReplaceFloat(text, "ManuChanBoundaryMargin", manuChanBoundaryMargin);
            text = ReplaceFloat(text, "ManuChanWanderAngleRate", manuChanWanderAngleRate);
            text = ReplaceFloat(text, "ManuChanPlayerDetectRadius", manuChanPlayerDetectRadius);
            text = ReplaceFloat(text, "ManuChanChaseKeepDistance", manuChanChaseKeepDistance);
            text = ReplaceInt(text, "ManuChanBoidsUpdateInterval", manuChanBoidsUpdateInterval);
            text = ReplaceFloat(text, "ManuChanMinEntityDistance", manuChanMinEntityDistance);

            File.WriteAllText(fullPath, text);
            AssetDatabase.ImportAsset(ConstsFilePath);
            Debug.Log("定数を保存しました。");
        }

        private static float ParseFloat(string source, string name)
        {
            var match = Regex.Match(source, $@"{name}\s*=\s*([0-9.eEfF+-]+)f");
            if (match.Success && float.TryParse(match.Groups[1].Value, out var value))
                return value;
            return 0f;
        }

        private static string ReplaceFloat(string source, string name, float value)
        {
            return Regex.Replace(
                source,
                $@"({name}\s*=\s*)[0-9.eEfF+-]+f",
                $"${{1}}{value.ToString("G")}f"
            );
        }

        private static int ParseInt(string source, string name)
        {
            var match = Regex.Match(source, $@"{name}\s*=\s*(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var value))
                return value;
            return 0;
        }

        private static string ReplaceInt(string source, string name, int value)
        {
            return Regex.Replace(source, $@"({name}\s*=\s*)\d+", $"${{1}}{value}");
        }
    }
}

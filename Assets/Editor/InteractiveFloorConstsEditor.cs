using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace InteractiveFloor.Editor
{
    public class InteractiveFloorConstsEditor : EditorWindow
    {
        private const string ConstsFilePath = "Assets/Scripts/Detect/InteractiveFloorConsts.cs";

        private float matchingThreshold;
        private float entityMoveSpeed;
        private float entityRotationSpeed;
        private float manuChanRotatingMoveSlowdownAngleThreshold;
        private float manuChanRotatingMoveSpeedMultiplier;
        private float entityViewerStopDistance;
        private float entityContactDistance;
        private float manuChanGrowthRateIncrease;
        private float manuChanMatchingDistance;
        private float manuChanWalkingGrowthRate;
        private float manuChanContactStayGrowthRate;
        private int manuChanFleeViewerCountThreshold;

        [MenuItem("InteractiveFloor/定数エディタ")]
        public static void Open()
        {
            var window = GetWindow<InteractiveFloorConstsEditor>("定数エディタ");
            window.minSize = new Vector2(360, 320);
            window.LoadValues();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("InteractiveFloor 定数設定", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

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
            EditorGUILayout.LabelField("人数制限", EditorStyles.miniBoldLabel);
            manuChanFleeViewerCountThreshold = EditorGUILayout.IntField("逃げる人数閾値", manuChanFleeViewerCountThreshold);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("距離設定", EditorStyles.miniBoldLabel);
            entityViewerStopDistance = EditorGUILayout.FloatField("Viewer停止距離", entityViewerStopDistance);
            entityContactDistance = EditorGUILayout.FloatField("接触判定距離", entityContactDistance);

            EditorGUILayout.Space(12);
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
            matchingThreshold = ParseConst(text, "MatchingThreshold");
            entityMoveSpeed = ParseConst(text, "EntityMoveSpeed");
            entityRotationSpeed = ParseConst(text, "EntityRotationSpeed");
            manuChanRotatingMoveSlowdownAngleThreshold = ParseConst(text, "ManuChanRotatingMoveSlowdownAngleThreshold");
            manuChanRotatingMoveSpeedMultiplier = ParseConst(text, "ManuChanRotatingMoveSpeedMultiplier");
            entityViewerStopDistance = ParseConst(text, "EntityViewerStopDistance");
            entityContactDistance = ParseConst(text, "EntityContactDistance");
            manuChanGrowthRateIncrease = ParseConst(text, "ManuChanGrowthRateIncrease");
            manuChanMatchingDistance = ParseConst(text, "ManuChanMatchingDistance");
            manuChanWalkingGrowthRate = ParseConst(text, "ManuChanWalkingGrowthRate");
            manuChanContactStayGrowthRate = ParseConst(text, "ManuChanContactStayGrowthRate");
            manuChanFleeViewerCountThreshold = ParseIntConst(text, "ManuChanFleeViewerCountThreshold");

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
            text = ReplaceConst(text, "MatchingThreshold", matchingThreshold);
            text = ReplaceConst(text, "EntityMoveSpeed", entityMoveSpeed);
            text = ReplaceConst(text, "EntityRotationSpeed", entityRotationSpeed);
            text = ReplaceConst(text, "ManuChanRotatingMoveSlowdownAngleThreshold", manuChanRotatingMoveSlowdownAngleThreshold);
            text = ReplaceConst(text, "ManuChanRotatingMoveSpeedMultiplier", manuChanRotatingMoveSpeedMultiplier);
            text = ReplaceConst(text, "EntityViewerStopDistance", entityViewerStopDistance);
            text = ReplaceConst(text, "EntityContactDistance", entityContactDistance);
            text = ReplaceConst(text, "ManuChanGrowthRateIncrease", manuChanGrowthRateIncrease);
            text = ReplaceConst(text, "ManuChanMatchingDistance", manuChanMatchingDistance);
            text = ReplaceConst(text, "ManuChanWalkingGrowthRate", manuChanWalkingGrowthRate);
            text = ReplaceConst(text, "ManuChanContactStayGrowthRate", manuChanContactStayGrowthRate);
            text = ReplaceIntConst(text, "ManuChanFleeViewerCountThreshold", manuChanFleeViewerCountThreshold);

            File.WriteAllText(fullPath, text);
            AssetDatabase.ImportAsset(ConstsFilePath);
            Debug.Log("定数を保存しました。");
        }

        private static float ParseConst(string source, string name)
        {
            var match = Regex.Match(source, $@"{name}\s*=\s*([0-9.eEfF+-]+)f");
            if (match.Success && float.TryParse(match.Groups[1].Value, out var value))
                return value;
            return 0f;
        }

        private static string ReplaceConst(string source, string name, float value)
        {
            return Regex.Replace(
                source,
                $@"({name}\s*=\s*)[0-9.eEfF+-]+f",
                $"${{1}}{value.ToString("G")}f"
            );
        }

        private static int ParseIntConst(string source, string name)
        {
            var match = Regex.Match(source, $@"{name}\s*=\s*(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var value))
                return value;
            return 0;
        }

        private static string ReplaceIntConst(string source, string name, int value)
        {
            return Regex.Replace(source, $@"({name}\s*=\s*)\d+", $"${{1}}{value}");
        }
    }
}

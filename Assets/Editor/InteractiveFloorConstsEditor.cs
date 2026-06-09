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

        // 餌(エサ)システム
        private float manuChanFoodSpawnHalfWidth;
        private float manuChanFoodSpawnHalfDepth;
        private float manuChanFoodSpawnProbability;
        private int manuChanFoodMaxCount;
        private float manuChanFoodPickupDistance;
        private float manuChanFoodAttachDistance;

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
            EditorGUILayout.HelpBox("ラベルにマウスを重ねると説明が表示されます。保存するとソースコードに書き込まれ、再コンパイルが走ります。", MessageType.Info);
            EditorGUILayout.Space(4);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("検出設定", EditorStyles.miniBoldLabel);
            matchingThreshold = EditorGUILayout.FloatField(
                new GUIContent("マッチング閾値", "センサー入力のノイズ除去閾値。小さいほど微細な動きも検出する（0〜1）。"),
                matchingThreshold);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("エンティティ移動", EditorStyles.miniBoldLabel);
            entityMoveSpeed = EditorGUILayout.FloatField(
                new GUIContent("移動速度", "全エンティティの移動速度 [m/s]。"),
                entityMoveSpeed);
            entityRotationSpeed = EditorGUILayout.FloatField(
                new GUIContent("回転速度", "全エンティティの回転速度 [度/秒]。大きいほど素早く方向転換する。"),
                entityRotationSpeed);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("マヌちゃん挙動", EditorStyles.miniBoldLabel);
            manuChanRotatingMoveSlowdownAngleThreshold = EditorGUILayout.FloatField(
                new GUIContent("減速角度閾値", "この角度以上を旋回するとき移動を遅くする閾値 [度]。逃げモード中は無効。"),
                manuChanRotatingMoveSlowdownAngleThreshold);
            manuChanRotatingMoveSpeedMultiplier = EditorGUILayout.FloatField(
                new GUIContent("回転移動速度倍率", "旋回中に適用される移動速度の倍率。1より小さいと旋回しながら遅くなる。"),
                manuChanRotatingMoveSpeedMultiplier);
            manuChanGrowthRateIncrease = EditorGUILayout.FloatField(
                new GUIContent("接触時成長量(一回)", "別のマヌちゃんに接触した瞬間に加算される成長量（0〜1）。"),
                manuChanGrowthRateIncrease);
            manuChanMatchingDistance = EditorGUILayout.FloatField(
                new GUIContent("マッチング距離", "マヌちゃん同士がマッチングと判定される距離 [m]。"),
                manuChanMatchingDistance);
            manuChanWalkingGrowthRate = EditorGUILayout.FloatField(
                new GUIContent("歩行中成長率/秒", "歩行中に1秒あたり加算される成長率。"),
                manuChanWalkingGrowthRate);
            manuChanContactStayGrowthRate = EditorGUILayout.FloatField(
                new GUIContent("接触継続成長率/秒", "別のマヌちゃんに接触し続けているとき、1秒あたり加算される成長率。"),
                manuChanContactStayGrowthRate);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("人数制限・距離設定", EditorStyles.miniBoldLabel);
            manuChanFleeViewerCountThreshold = EditorGUILayout.IntField(
                new GUIContent("逃げる人数閾値", "この人数以上のViewerが同時に検出されると、全マヌちゃんが逃げモードに入る。"),
                manuChanFleeViewerCountThreshold);
            entityViewerStopDistance = EditorGUILayout.FloatField(
                new GUIContent("Viewer停止距離", "ViewerがEntityにこの距離まで近づくと停止と見なす [m]。"),
                entityViewerStopDistance);
            entityContactDistance = EditorGUILayout.FloatField(
                new GUIContent("接触判定距離", "Entity同士がこの距離以下になると接触とみなす [m]。"),
                entityContactDistance);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("マップ範囲", EditorStyles.miniBoldLabel);
            manuChanMapHalfWidth = EditorGUILayout.FloatField(
                new GUIContent("マップ半幅 X(m)", "生存エリアのX方向の半幅 [m]。中心から左右この距離が有効エリアになる。"),
                manuChanMapHalfWidth);
            manuChanMapHalfDepth = EditorGUILayout.FloatField(
                new GUIContent("マップ半幅 Z(m)", "生存エリアのZ方向の半幅 [m]。中心から前後この距離が有効エリアになる。"),
                manuChanMapHalfDepth);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("餌(エサ)システム", EditorStyles.miniBoldLabel);
            manuChanFoodSpawnHalfWidth = EditorGUILayout.FloatField(
                new GUIContent("餌 出現半幅 X(m)", "餌が出現する範囲のX方向の半幅 [m]。エリア中心から左右この距離内に出現する。"),
                manuChanFoodSpawnHalfWidth);
            manuChanFoodSpawnHalfDepth = EditorGUILayout.FloatField(
                new GUIContent("餌 出現半幅 Z(m)", "餌が出現する範囲のZ方向の半幅 [m]。エリア中心から前後この距離内に出現する。"),
                manuChanFoodSpawnHalfDepth);
            manuChanFoodSpawnProbability = EditorGUILayout.FloatField(
                new GUIContent("餌 出現確率/秒", "1秒あたりに餌が1つ出現する確率（フレームレート非依存）。大きいほど頻繁に出現する。"),
                manuChanFoodSpawnProbability);
            manuChanFoodMaxCount = EditorGUILayout.IntField(
                new GUIContent("餌 最大数", "マップ上に同時に存在できる餌の最大数。"),
                manuChanFoodMaxCount);
            manuChanFoodPickupDistance = EditorGUILayout.FloatField(
                new GUIContent("餌 回収距離(m)", "Viewerが餌にこの距離まで近づくと回収する [m]。"),
                manuChanFoodPickupDistance);
            manuChanFoodAttachDistance = EditorGUILayout.FloatField(
                new GUIContent("餌 進化範囲(m)", "餌を回収したViewerのこの距離内にいるマヌちゃんが一気に次の段階へ進化する [m]。"),
                manuChanFoodAttachDistance);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("群れ(Boids)システム", EditorStyles.miniBoldLabel);
            manuChanMaxCount = EditorGUILayout.IntField(
                new GUIContent("マップ内最大数", "同時に存在できるマヌちゃんの最大数。起動時にこの数だけスポーンする。"),
                manuChanMaxCount);
            manuChanFlockPerceptionRadius = EditorGUILayout.FloatField(
                new GUIContent("群れ認識半径", "整列・結合の計算に使う近隣認識半径 [m]。この範囲の仲間を「群れ」と認識する。"),
                manuChanFlockPerceptionRadius);
            manuChanSeparationRadius = EditorGUILayout.FloatField(
                new GUIContent("分離半径", "この半径内の仲間から離れようとする分離半径 [m]。群れ認識半径より小さくすること。"),
                manuChanSeparationRadius);
            manuChanSeparationWeight = EditorGUILayout.FloatField(
                new GUIContent("分離の強さ", "分離力の重み。大きいほど密集を嫌い間隔を広げる。"),
                manuChanSeparationWeight);
            manuChanAlignmentWeight = EditorGUILayout.FloatField(
                new GUIContent("整列の強さ", "整列力の重み。大きいほど仲間と同じ方向を向いて動く。"),
                manuChanAlignmentWeight);
            manuChanCohesionWeight = EditorGUILayout.FloatField(
                new GUIContent("結合の強さ", "結合力の重み。大きいほど仲間の中心に引き寄せられ、群れがまとまる。"),
                manuChanCohesionWeight);
            manuChanBoundaryWeight = EditorGUILayout.FloatField(
                new GUIContent("境界反発の強さ", "エリア境界に近づいたとき中心方向へ押し戻す力の強さ。"),
                manuChanBoundaryWeight);
            manuChanBoundaryMargin = EditorGUILayout.FloatField(
                new GUIContent("境界余白距離", "境界からこの距離内に入ると反発力が働き始める [m]。"),
                manuChanBoundaryMargin);
            manuChanWanderAngleRate = EditorGUILayout.FloatField(
                new GUIContent("徘徊角速度(度/秒)", "近隣に仲間がいないとき、1秒あたりにランダムで変化する最大方向変化量 [度/秒]。"),
                manuChanWanderAngleRate);
            manuChanPlayerDetectRadius = EditorGUILayout.FloatField(
                new GUIContent("プレイヤー検出半径", "この半径内にViewerがいると追いかけモードに入る [m]。"),
                manuChanPlayerDetectRadius);
            manuChanChaseKeepDistance = EditorGUILayout.FloatField(
                new GUIContent("プレイヤー停止距離", "Viewerを追いかけるとき、この距離手前で止まる [m]。0に近いほどぴったりくっつく。"),
                manuChanChaseKeepDistance);
            manuChanBoidsUpdateInterval = EditorGUILayout.IntField(
                new GUIContent("Boids更新間隔(フレーム)", "BoidsのO(N²)計算をNフレームに1回に間引く。値が大きいほど軽いが反応が遅くなる。"),
                manuChanBoidsUpdateInterval);
            manuChanMinEntityDistance = EditorGUILayout.FloatField(
                new GUIContent("エンティティ最小距離", "ハード分離で保証するエンティティ間の最小距離 [m]。これより近づくと強制的に押し退けられる。"),
                manuChanMinEntityDistance);

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

            manuChanFoodSpawnHalfWidth = ParseFloat(text, "ManuChanFoodSpawnHalfWidth");
            manuChanFoodSpawnHalfDepth = ParseFloat(text, "ManuChanFoodSpawnHalfDepth");
            manuChanFoodSpawnProbability = ParseFloat(text, "ManuChanFoodSpawnProbability");
            manuChanFoodMaxCount = ParseInt(text, "ManuChanFoodMaxCount");
            manuChanFoodPickupDistance = ParseFloat(text, "ManuChanFoodPickupDistance");
            manuChanFoodAttachDistance = ParseFloat(text, "ManuChanFoodAttachDistance");

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

            text = ReplaceFloat(text, "ManuChanFoodSpawnHalfWidth", manuChanFoodSpawnHalfWidth);
            text = ReplaceFloat(text, "ManuChanFoodSpawnHalfDepth", manuChanFoodSpawnHalfDepth);
            text = ReplaceFloat(text, "ManuChanFoodSpawnProbability", manuChanFoodSpawnProbability);
            text = ReplaceInt(text, "ManuChanFoodMaxCount", manuChanFoodMaxCount);
            text = ReplaceFloat(text, "ManuChanFoodPickupDistance", manuChanFoodPickupDistance);
            text = ReplaceFloat(text, "ManuChanFoodAttachDistance", manuChanFoodAttachDistance);

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

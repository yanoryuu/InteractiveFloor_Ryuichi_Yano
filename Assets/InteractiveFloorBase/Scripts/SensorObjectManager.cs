using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Linq;
using InteractiveFloor.Detect;

namespace InteractiveFloor
{
    /// <summary>
    /// センサーオブジェクトを管理するスクリプト
    /// </summary>
    [System.Serializable]
    public class SensorObjectManager : MonoBehaviour
    {
        #region Classes
        /// <summary>
        /// センサーオブジェクト
        /// </summary>
        [System.Serializable]
        public class SensorObject
        {
            /// <summary>
            /// 更新がないときに、このセンサーオブジェクトを消去するまでの時間（秒）
            /// </summary>
            const float DURATION_WHEN_NO_UPDATE = 1.5f;
            /// <summary>
            /// 過去の値を保持しておく配列の数
            /// </summary>
            const int HISTORY_NUM = 8;

            /// <summary>
            /// このセンサーオブジェクトのID
            /// </summary>
            public int id;
            /// <summary>
            /// X座標の値（ワールド空間）
            /// </summary>
            public float x;
            /// <summary>
            /// Y座標の値（ワールド空間）
            /// </summary>
            public float y;
            /// <summary>
            /// 横幅（ワールド空間）
            /// </summary>
            public float width;
            /// <summary>
            /// 縦幅（ワールド空間）
            /// </summary>
            public float height;
            
            /// <summary>
            /// 半径（横幅と立幅の平均）
            /// </summary>
            public float radius => (this.width + this.height) * 0.5f;
            /// <summary>
            /// 位置（x, y）
            /// </summary>
            public float2 position => float2(this.x, this.y);

            /// <summary>
            /// 位置Xの中央値
            /// </summary>
            public float xMedian;
            /// <summary>
            /// 位置Yの中央値
            /// </summary>
            public float yMedian;
            /// <summary>
            /// 幅の中央値
            /// </summary>
            public float widthMedian;
            /// <summary>
            /// 高さの中央値
            /// </summary>
            public float heightMedian;
            /// <summary>
            ///位置（x, y)の中央値
            /// </summary>
            public float2 positionMedian => float2(this.xMedian, this.yMedian);
            /// <summary>
            /// ひとつ前のフレームの位置（x, y）の中央値
            /// </summary>
            public float2 previousPositionMedian { get; private set; }
            /// <summary>
            /// 同一人物として扱うためのラベル
            /// </summary>
            public string TrackingLabel { get; set; } = string.Empty;
            public ViewerType Type { get; set; }

            /// <summary>
            /// X座標上の位置の過去の値を保持する配列
            /// </summary>
            [SerializeField, ReadOnly]
            float[] _xHistory = new float[HISTORY_NUM];
            /// <summary>
            /// Y座標上の位置の過去の値を保持する配列
            /// </summary>
            [SerializeField, ReadOnly]
            float[] _yHistory = new float[HISTORY_NUM];
            /// <summary>
            /// 幅の過去の値を保持する配列
            /// </summary>
            [SerializeField, ReadOnly]
            float[] _widthHistory = new float[HISTORY_NUM];
            /// <summary>
            /// 高さの過去の値を保持する配列
            /// </summary>
            [SerializeField, ReadOnly]
            float[] _heightHistory = new float[HISTORY_NUM];

            /// <summary>
            /// 更新が最後にあった時から経過した時間（秒）
            /// </summary>        
            float _timerFromLastUpdate = 0.0f;
            /// <summary>
            /// 削除対象とするか
            /// </summary>
            bool _isDead = false;
            /// <summary>
            /// 削除対象とするか
            /// </summary>
            public bool isDead => this._isDead;

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="x">位置X</param>
            /// <param name="y">位置X</param>
            /// <param name="w">幅</param>
            /// <param name="h">高さ</param>
            public SensorObject(float x, float y, float w, float h)
            {
                this.x      = x;
                this.y      = y;
                this.width  = w;
                this.height = h;

                for (var i = 0; i < HISTORY_NUM; i++)
                {
                    this._xHistory[i] = x;
                    this._yHistory[i] = y;
                    this._widthHistory[i] = w;
                    this._heightHistory[i] = h;
                }

                CalculatePositionMedian();
                previousPositionMedian = positionMedian;
            }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="rect">Rect(位置X, 位置Y, 幅, 高さ)</param>
            public SensorObject(Rect rect)
            {
                this.x = rect.x;
                this.y = rect.y;
                this.width = rect.width;
                this.height = rect.height;

                for (var i = 0; i < HISTORY_NUM; i++)
                {
                    this._xHistory[i] = x;
                    this._yHistory[i] = y;
                    this._widthHistory[i] = width;
                    this._heightHistory[i] = height;
                }

                CalculatePositionMedian();
                previousPositionMedian = positionMedian;
            }

            /// <summary>
            /// 更新
            /// </summary>
            /// <param name="deltaTime">フレーム間の時間の差分</param>
            public void Update(float deltaTime)
            {
                if (_timerFromLastUpdate > DURATION_WHEN_NO_UPDATE)
                {
                    _isDead = true;
                    return;
                }

                previousPositionMedian = positionMedian;

                ShiftRight(_xHistory);
                ShiftRight(_yHistory);
                ShiftRight(_widthHistory);
                ShiftRight(_heightHistory);

                _xHistory[0] = this.x;
                _yHistory[0] = this.y;
                _widthHistory[0] = this.width;
                _heightHistory[0] = this.height;

                CalculatePositionMedian();

                _timerFromLastUpdate += deltaTime;
            }

            /// <summary>
            /// 現在のフレームの値としてセットする
            /// </summary>
            /// <param name="x">位置X</param>
            /// <param name="y">位置Y</param>
            /// <param name="w">幅</param>
            /// <param name="h">高さ</param>
            public void SetValueAsCurrentFrame(float x, float y, float w, float h)
            {
                this.x = x;
                this.y = y;
                this.width = w;
                this.height = h;

                _timerFromLastUpdate = 0.0f;
            }

            /// <summary>
            /// 現在のフレームの値としてセットする
            /// </summary>
            /// <param name="rect">Rect(位置X, 位置Y, 幅, 高さ)</param>
            public void SetValueAsCurrentFrame(Rect rect)
            {
                this.x = rect.x;
                this.y = rect.y;
                this.width = rect.width;
                this.height = rect.height;

                _timerFromLastUpdate = 0.0f;
            }

            /// <summary>
            /// 廃棄
            /// </summary>
            public void Dispose()
            {
                _xHistory = null;
                _yHistory = null;
                _widthHistory = null;
                _heightHistory = null;
            }

            /// <summary>
            /// 配列の中身を右にシフトする（[0]を[1]に、[1]を[2]に）
            /// </summary>
            /// <param name="arr"></param>
            private void ShiftRight(float[] arr)
            {
                //float lastElement = arr[arr.Length - 1];
                Array.Copy(arr, 0, arr, 1, arr.Length - 1);
                //arr[0] = lastElement;
            }

            //private void ShiftLeft(float[] arr)
            //{
            //    float firstElement = arr[0];
            //    Array.Copy(arr, 1, arr, 0, arr.Length - 1);
            //    arr[arr.Length - 1] = firstElement;
            //}

            /// <summary>
            /// 
            /// </summary>
            void CalculatePositionMedian()
            {
                xMedian = CalculateMedian(_xHistory);
                yMedian = CalculateMedian(_yHistory);
                widthMedian = CalculateMedian(_widthHistory);
                heightMedian = CalculateMedian(_heightHistory);
            }

            /// <summary>
            /// 中央値を計算
            /// </summary>
            /// <param name="arr"></param>
            /// <returns></returns>
            float CalculateMedian(float[] arr)
            {
                // 並びかえる
                var sorted = arr.OrderBy(a => a).ToArray();

                // 配列の要素数が奇数の場合、中央の値を取得
                if (sorted.Length % 2 != 0)
                {
                    int middleIndex = sorted.Length / 2;
                    return sorted[middleIndex];
                }
                // 配列が偶数の場合、中央の2つの値の平均を取得する
                else
                {
                    int middleIndex1 = sorted.Length / 2 - 1;
                    int middleIndex2 = sorted.Length / 2;
                    return (sorted[middleIndex1] + sorted[middleIndex2]) * 0.5f;
                }
            }
        }
        #endregion

        #region Parameters and Variables
        /// <summary>
        /// センサーから送られてくる値の範囲（X最小）
        /// </summary>
        [Header("Parameters")]
        public float SensorXMin = 0.0f;
        /// <summary>
        /// センサーから送られてくる値の範囲（X最大）
        /// </summary>
        public float SensorXMax = 1.0f;
        /// <summary>
        /// センサーから送られてくる値の範囲（Y最小）
        /// </summary>
        public float SensorYMin = 0.0f;
        /// <summary>
        /// センサーから送られてくる値の範囲（Y最大）
        /// </summary>
        public float SensorYMax = 1.0f;
        /// <summary>
        /// センサーの値をワールド空間に変換する際の値の範囲（X最小）
        /// </summary>
        [Space]
        public float AreaXMin = 0.0f;
        /// <summary>
        /// センサーの値をワールド空間に変換する際の値の範囲（X最大）
        /// </summary>
        public float AreaXMax = 1.0f;
        /// <summary>
        /// センサーの値をワールド空間に変換する際の値の範囲（Y最小）
        /// </summary>
        public float AreaZMin = 0.0f;
        /// <summary>
        /// センサーの値をワールド空間に変換する際の値の範囲（Y最大）
        /// </summary>
        public float AreaZMax = 1.0f;
        [Space]
        /// <summary>
        /// 違うセンサーオブジェクトであると判断する距離（ワールド空間）
        /// </summary>
        [Range(0.0f, 100.0f)]
        public float DistanceToJudgeAsDifferent = 0.1f;

        /// <summary>
        /// センサーオブジェクトを格納する動的配列
        /// </summary>
        [Header("Private Variables")]
        [SerializeField]
        List<SensorObject> _sensorObjectList = new List<SensorObject>();
        /// <summary>
        /// センサーオブジェクトを格納する動的配列
        /// </summary>
        public List<SensorObject> SensorList => _sensorObjectList;
        /// <summary>
        /// 作成されたセンサーオブジェクトの数
        /// </summary>
        int _sensorObjectCount = 0;
      
        [Header("Debug Draw")]       
        /// <summary>
        /// マーカーオブジェクトをデバッグのために表示するかどうか
        /// </summary>
        [SerializeField]
        bool _drawSensorMarkerForDebug = true;
        /// <summary>
        /// デバッグ矩形描画用のマテリアル
        /// </summary>
        [SerializeField]
        Material _debugRectMaterial = null;
        /// <summary>
        /// デバッグ矩形を描く位置（Y）
        /// </summary>
        [SerializeField]
        float _debugRectY = 0.02f;
        /// <summary>
        /// デバッグ描画用の矩形のメッシュ
        /// </summary>
        Mesh _debugQuadMesh = null;
        #endregion

        #region MonoBehaviour Functions

        void Awake()
        {
            CreateDebugQuadMesh();    
        }

        void Update()
        {
            var dt = Time.deltaTime;

            // 異常に長いフレームは追跡更新しない
            if (dt > 0.1f)
                return;

            // 全てのセンサーオブジェクトについて処理
            for (var i = _sensorObjectList.Count - 1; i >= 0; i--)
            //for (var i = 0; i < _sensorObjectList.Count; i++)
            {
                // 更新
                _sensorObjectList[i].Update(dt);

                // 指定時間更新されず、不必要である場合、
                if (_sensorObjectList[i].isDead)
                {
                    // 消去する
                    _sensorObjectList[i].Dispose();
                    _sensorObjectList.RemoveAt(i);
                }
            }
        }

        void LateUpdate()
        {
            DrawDebugRects();
        }

        void OnDestroy()
        {
            if (_debugQuadMesh != null)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(_debugQuadMesh);
                }
                else
                {
                    Destroy(_debugQuadMesh);
                }
                _debugQuadMesh = null;
            }
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// OSCで新たに送られてきたメッセージを処理し、センサーオブジェクトの生成、更新を行う。
        /// SensorOscMessageReceiver.cs から呼びだす。
        /// </summary>
        /// <param name="newOscMessageList"></param>
        public void SetValues(List<Rect> newOscMessageList)
        {
            // 新しくメッセージが送られてきていなければ処理を終了する。
            if (newOscMessageList == null || newOscMessageList.Count == 0)
                return;

            // この更新内ですでに使ったSensorObjectを記録
            HashSet<int> usedSensorIds = new HashSet<int>();

            // 新しく送られてきたメッセージ一つ一つについて処理を行う。
            foreach (var newOscMessage in newOscMessageList)
            {
                // センサー座標系（0.0～1.0）をワールド空間へ変換する
                var newOscMessageWS = ConvertWorldSpace(newOscMessage);

                // 幅または高さが不正なBlobは無視する
                if (newOscMessageWS.width <= 0.0f || newOscMessageWS.height <= 0.0f)
                    continue;

                // このBlobに最も近い既存センサーを探す
                SensorObject nearestSensor = null;
                float nearestDistance = float.MaxValue;

                var newPos = new float2(newOscMessageWS.x, newOscMessageWS.y);

                for (var i = 0; i < _sensorObjectList.Count; i++)
                {
                    var s = _sensorObjectList[i];

                    // この更新内で既に別Blobへ対応付け済みセンサーは除外する
                    if (usedSensorIds.Contains(s.id))
                        continue;

                    // 既存センサーと今回のBlobの中心位置の距離を求める
                    var dist = distance(s.positionMedian, newPos);

                    // 一定距離以内にあるセンサーのうち、最も近いものを候補にする
                    if (dist < DistanceToJudgeAsDifferent && dist < nearestDistance)
                    {
                        nearestDistance = dist;
                        nearestSensor = s;
                    }
                }

                if (nearestSensor != null)
                {
                    // 対応する既存センサーが見つかったので、このフレームの値で更新する
                    nearestSensor.SetValueAsCurrentFrame(newOscMessageWS);
                    // 同一更新内で再利用されないように、このセンサーIDを使用済みとして記録する
                    usedSensorIds.Add(nearestSensor.id);
                }
                else
                {
                    // 対応する既存センサーが見つからなかったので、新しいセンサーを作成する
                    var newSensorObject = new SensorObject(newOscMessageWS);
                    newSensorObject.id = _sensorObjectCount++;
                    _sensorObjectList.Add(newSensorObject);
                    
                    // 新規生成したセンサーも、この更新内では使用済みとして扱う
                    usedSensorIds.Add(newSensorObject.id);
                }
            }
            // 受信済みメッセージはこのタイミングで処理完了なのでクリアする
            newOscMessageList.Clear();            
        }
        #endregion

        #region Private Functions
        /// <summary>
        /// ワールド空間に変換
        /// </summary>
        /// <param name="r">Rect（位置X, 位置Y, 幅, 高さ）</param>
        /// <returns>ワールド空間でのRect（位置X, 位置Y, 幅, 高さ）</returns>
        Rect ConvertWorldSpace(Rect r)
        {
            return new Rect(
                remap(SensorXMin, SensorXMax, AreaXMin, AreaXMax, r.x),
                remap(SensorYMin, SensorYMax, AreaZMin, AreaZMax, r.y),
                0.5f * r.width * (abs(AreaXMax - AreaXMin) / abs(SensorXMax - SensorXMin)),
                0.5f * r.height * (abs(AreaZMax - AreaZMin) / abs(SensorYMax - SensorYMin))
            );
        }     
        
        /// <summary>
        /// デバッグ用の矩形メッシュを作成する
        /// </summary>
        void CreateDebugQuadMesh()
        {
            if (_debugQuadMesh != null)
            {
                return;
            }

            _debugQuadMesh = new Mesh();
            _debugQuadMesh.name = "SensorDebugQuadXZ";

            _debugQuadMesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, 0.0f, -0.5f),
                new Vector3(-0.5f, 0.0f,  0.5f),
                new Vector3( 0.5f, 0.0f,  0.5f),
                new Vector3( 0.5f, 0.0f, -0.5f)
            };

            _debugQuadMesh.triangles = new int[]
            {
                0, 1, 2, 0, 2, 3
            };

            _debugQuadMesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };

            _debugQuadMesh.RecalculateNormals();
            _debugQuadMesh.RecalculateBounds();
        }

        void DrawDebugRects()
        {
            if (!_drawSensorMarkerForDebug)
            {
                return;
            }

            if (_debugRectMaterial == null)
            {
                return;
            }

            for (var i = 0; i < _sensorObjectList.Count; i++)
            {
                var s = _sensorObjectList[i];

                var pos = new Vector3(s.xMedian, _debugRectY, s.yMedian);
                var scale = new Vector3(s.widthMedian, 1.0f, s.heightMedian);

                var matrix = Matrix4x4.TRS(
                    pos,
                    Quaternion.identity,
                    scale
                );

                Graphics.DrawMesh(
                    _debugQuadMesh,
                    matrix,
                    _debugRectMaterial,
                    gameObject.layer

                );
            }
        }
        #endregion
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.ComponentModel;
using Klak.Spout;

namespace InteractiveFloor
{
    /// <summary>
    /// インタラクティブフロア マネージャ
    /// </summary>
    [ExecuteAlways]
    public class InteractiveFloorManager : MonoBehaviour
    {
        #region Parameters and Variables
        /// <summary>
        /// カメラオブジェクトの参照
        /// </summary>
        [Header("References")]
        [SerializeField]
        Camera _cameraRef = null;
        /// <summary>
        /// ダミーカメラの参照
        /// </summary>
        [SerializeField]
        Camera _dummyCameraRef = null;
        /// <summary>
        /// 背景用の画像を表示する矩形オブジェクトの参照
        /// </summary>
        [SerializeField]
        Transform _backgroundImageQuadRef = null;
        /// <summary>
        /// センサーオブジェクト管理スクリプトの参照
        /// </summary>
        [SerializeField]
        SensorObjectManager _sensorObjectManagerRef = null;
        /// <summary>
        /// SpoutSenderの参照
        /// </summary>
        [SerializeField]
        SpoutSender _spoutSenderRef = null;

        /// <summary>
        /// ワールド空間でのエリアの範囲（X最小）
        /// </summary>
        [Header("Parameter")]
        public float AreaXMin = 0.0f;
        /// <summary>
        /// ワールド空間でのエリアの範囲（X最大）
        /// </summary>
        public float AreaXMax = 1.0f;
        /// <summary>
        /// ワールド空間でのエリアの範囲（Z最小）
        /// </summary>
        public float AreaZMin = 0.0f;
        /// <summary>
        /// ワールド空間でのエリアの範囲（Z最大）
        /// </summary>
        public float AreaZMax = 1.0f;
        /// <summary>
        /// ワールド空間でのエリアの範囲（Y最小）
        /// </summary>
        public float AreaYMin = -25.0f;
        /// <summary>
        /// ワールド空間でのエリアの範囲（Y最大）
        /// </summary>
        public float AreaYMax = 5.0f;
        /// <summary>
        /// 内部的に作成するRenderTextureの長辺の解像度
        /// </summary>
        [Range(64, 8192)]
        public int ResultRTResolutionLongSide = 2048;
        /// <summary>
        /// Spoutを有効にする
        /// </summary>
        public bool EnableSpout = false;
        /// <summary>
        /// レンダリング結果を格納するRenderTexture
        /// </summary>
        [Header("Private Variables")]
        [SerializeField, ReadOnly]
        RenderTexture _resultRT = null;
        /// <summary>
        /// レンダリング結果を格納するRenderTextureの幅（px）
        /// </summary>
        [SerializeField, ReadOnly]
        int _resultRTResolutionWidth = 1024;
        /// <summary>
        /// レンダリング結果を格闘するRenderTextureの高さ（px）
        /// </summary>
        [SerializeField, ReadOnly]
        int _resultRTResolutionHeight = 1024;
        /// <summary>
        /// レンダリング結果を格納するRenderTextureのアスペクト比
        /// </summary>
        [SerializeField, ReadOnly]
        float _aspectRatio = 1.0f;
        #endregion

        #region MonoBehaviour Functions
        void Update()
        {

            UpdateResultRenderTexture();

            UpdateCamera();

            UpdateSpoutSender();

            UpdateBackgroundImageQuad();

            UpdateSensorObjectManager();
        }

        void OnDrawGizmos()
        {
            DrawGizmsoAreaRect(AreaXMin, AreaXMax, AreaZMin, AreaZMax, 0.0f);
            DrawGizmosCameraFrustumBox(AreaXMin, AreaXMax, AreaZMin, AreaZMax, AreaYMin, AreaYMax);
        }

        void OnGUI()
        {
            if (EnableSpout)
            {
                if (_resultRT != null)
                {
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _resultRT);
                }
            }
        }
        #endregion

        #region Private Functions

        /// <summary>
        /// 結果を格納するRenderTextureの更新
        /// </summary>
        void UpdateResultRenderTexture()
        {
            // アスペクト比
            _aspectRatio = (abs(AreaXMax - AreaXMin) / abs(AreaZMax - AreaZMin));

            // 横長
            if (_aspectRatio > 1.0f)
            {
                _resultRTResolutionWidth = ResultRTResolutionLongSide;
                _resultRTResolutionHeight = Mathf.FloorToInt(ResultRTResolutionLongSide * (1.0f / _aspectRatio));
                // 縦長
            }
            else
            {
                _resultRTResolutionHeight = ResultRTResolutionLongSide;
                _resultRTResolutionWidth = Mathf.FloorToInt(ResultRTResolutionLongSide * _aspectRatio);
            }

            // レンダリング結果を格納をするRenderTextureを作成する
            // 幅、高さが前のフレームのものと変わっていれば、一度削除し、作成しなおす。
            if (_resultRT == null || (_resultRT.width != _resultRTResolutionWidth || _resultRT.height != _resultRTResolutionHeight))
            {
                if (_resultRT != null)
                {
                    _cameraRef.targetTexture = null;
                    RenderTextureHelper.DeleteRenderTexture(_resultRT);
                }
                var desc = new RenderTextureDescriptor(_resultRTResolutionWidth, _resultRTResolutionHeight);
                desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
                desc.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm_S8_UInt;

                _resultRT = new RenderTexture(desc);
                _resultRT.filterMode = FilterMode.Bilinear;
                _resultRT.wrapMode = TextureWrapMode.Clamp;
                _resultRT.Create();
            }
        }

        /// <summary>
        /// カメラを更新
        /// </summary>
        void UpdateCamera()
        {
            // カメラの設定項目を変更
            if (_cameraRef != null)
            {
                _cameraRef.transform.position = new Vector3(
                    (AreaXMin + AreaXMax) / 2,
                    AreaYMax,
                    (AreaZMin + AreaZMax) / 2
                );

                _cameraRef.transform.eulerAngles = new Vector3(90.0f, 0.0f, 0.0f);

                _cameraRef.orthographic = true;
                _cameraRef.orthographicSize = abs(AreaZMax - AreaZMin) / 2;

                _cameraRef.nearClipPlane = 0.0f;
                _cameraRef.farClipPlane = abs(AreaYMax - AreaYMin);

                if (EnableSpout)
                {
                    _cameraRef.targetTexture = _resultRT;
                }
                else
                {
                    _cameraRef.targetTexture = null;
                }
            }
        }
        
        /// <summary>
        /// SpoutSenderの更新
        /// </summary>
        void UpdateSpoutSender()
        {
            if (_spoutSenderRef != null)
            {
                if (EnableSpout)
                {
                    _spoutSenderRef.gameObject.SetActive(true);
                    _spoutSenderRef.enabled = true;
                    _spoutSenderRef.sourceTexture = _resultRT ?? null;
                }
                else
                {
                    _spoutSenderRef.gameObject.SetActive(false);
                    _spoutSenderRef.enabled = false;
                }
            }

            if (_dummyCameraRef != null)
            {
                if (EnableSpout)
                    _dummyCameraRef.gameObject.SetActive(true);
                else
                    _dummyCameraRef.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 背景画像を表示する矩形の更新
        /// </summary>
        void UpdateBackgroundImageQuad()
        {
            // 背景イメージを表示する矩形オブジェクトの位置調整
            if (_backgroundImageQuadRef != null)
            {
                _backgroundImageQuadRef.transform.position = new Vector3((AreaXMin + AreaXMax) / 2, AreaYMin + 0.01f, (AreaZMin + AreaZMax) / 2);
                _backgroundImageQuadRef.transform.eulerAngles = new Vector3(90.0f, 0.0f, 0.0f);
                _backgroundImageQuadRef.transform.localScale = new Vector3(abs(AreaXMax - AreaXMin), abs(AreaZMax - AreaZMin), 1.0f);
            }
        }

        /// <summary>
        /// SensorObjectManagerを更新
        /// </summary>
        void UpdateSensorObjectManager()
        {
            // センサーオブジェクト管理スクリプトにパラメータを代入
            if (_sensorObjectManagerRef != null)
            {
                _sensorObjectManagerRef.AreaXMin = AreaXMin;
                _sensorObjectManagerRef.AreaXMax = AreaXMax;
                _sensorObjectManagerRef.AreaZMin = AreaZMin;
                _sensorObjectManagerRef.AreaZMax = AreaZMax;
            }
        }

        /// <summary>
        /// エリアをGizmosとして描画する
        /// </summary>
        /// <param name="xmin"></param>
        /// <param name="xmax"></param>
        /// <param name="zmin"></param>
        /// <param name="zmax"></param>
        /// <param name="y"></param>
        void DrawGizmsoAreaRect(float xmin, float xmax, float zmin, float zmax, float y)
        {
            Gizmos.color = Color.cyan;

            var p = new Vector3[4]
            {
            new Vector3(xmin, y, zmin),
            new Vector3(xmin, y, zmax),
            new Vector3(xmax, y, zmax),
            new Vector3(xmax, y, zmin)
            };

            var j = 3;
            for (var i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(p[i], p[j]);
                j = i;
            }

            // テキストを描画
            GizmosHelper.DrawString("xmin:" + xmin.ToString("0.000"), (p[0] + p[1]) / 2, Color.red, Vector2.zero, 11);
            GizmosHelper.DrawString("xmax:" + xmax.ToString("0.000"), (p[2] + p[3]) / 2, Color.red, Vector2.zero, 11);
            GizmosHelper.DrawString("zmin:" + zmin.ToString("0.000"), (p[3] + p[0]) / 2, Color.blue, Vector2.zero, 11);
            GizmosHelper.DrawString("zmax:" + zmax.ToString("0.000"), (p[1] + p[2]) / 2, Color.blue, Vector2.zero, 11);
        }

        /// <summary>
        /// カメラのレンダリング範囲をGizmosで描画する
        /// </summary>
        /// <param name="xmin"></param>
        /// <param name="xmax"></param>
        /// <param name="zmin"></param>
        /// <param name="zmax"></param>
        /// <param name="ymin"></param>
        /// <param name="ymax"></param>
        void DrawGizmosCameraFrustumBox(float xmin, float xmax, float zmin, float zmax, float ymin, float ymax)
        {
            Gizmos.color = Color.green;

            var center = new Vector3((xmin + xmax) / 2, (ymin + ymax) / 2, (zmin + zmax) / 2);
            var size = new Vector3(abs(xmax - xmin), abs(ymax - ymin), abs(zmax - zmin));

            Gizmos.DrawWireCube(center, size);
        }
        #endregion
    }
}

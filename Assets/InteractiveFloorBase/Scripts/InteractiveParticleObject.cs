using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InteractiveFloor
{
    /// <summary>
    /// インタラクティブパーティクルオブジェクト
    /// ・センサー位置に移動させる
    /// ・死活管理
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class InteractiveParticleObject : MonoBehaviour
    {
        #region Parameters and Variables
        /// <summary>
        /// センサーオブジェクト
        /// </summary>
        [SerializeField, ReadOnly]
        SensorObjectManager.SensorObject _sensorObject = null;
        public SensorObjectManager.SensorObject sensorObject => this._sensorObject ?? null;

        /// <summary>
        /// センサーオブジェクトのセット
        /// </summary>
        /// <param name="sensor"></param>
        public void SetSensorObject(SensorObjectManager.SensorObject sensor)
        {
            this._sensorObject = sensor;
        }

        /// <summary>
        /// パーティクルを停止させ、削除する準備をするかどうか
        /// </summary>
        [SerializeField, ReadOnly]
        bool _isStopEmittingParticlesAndPrepareToDelete = false;
        public bool isStopEmittingParticlesAndPrepareToDelete => this._isStopEmittingParticlesAndPrepareToDelete;
        public bool SetStopEmittingParticlesAndPrepareToDelete(bool b) => this._isStopEmittingParticlesAndPrepareToDelete = b;

        /// <summary>
        /// 削除対象かどうか
        /// ・_isStopEmittingParticlesAndPrepareToDelete が true である
        /// ・すべてのパーティクルの発生が完全に停止している
        /// </summary>
        [SerializeField, ReadOnly]
        bool _isDead = false;
        /// <summary>
        /// 削除対象かどうか
        /// </summary>
        public bool isDead => this._isDead;
        #endregion

        #region MonoBehaviour Functions
        void Awake()
        {
        }

        void Update()
        {
            // 位置を更新
            if (this._sensorObject != null)
            {
                transform.position = new Vector3(
                    this._sensorObject.positionMedian.x,
                    0.0f,
                    this._sensorObject.positionMedian.y
                );
            }

            // 自分自身にアタッチされているパーティクル、
            // また、この階層にアタッチされたパーティクルの参照を取得
            var particleSystems = this.GetComponentsInChildren<ParticleSystem>();

            // パーティクルの発生を停止させる
            if (_isStopEmittingParticlesAndPrepareToDelete == true)
            {
                foreach (var ps in particleSystems)
                {
                    ps.Stop();
                }
            }

            // 現存するパーティクルの個数をカウント
            var particlesCount = 0;
            if (particleSystems != null)
            {
                foreach (var ps in particleSystems)
                {
                    particlesCount += ps.particleCount;
                }
            }

            // パーティクルの生成が停止しており、現存するパーティクルの数が 0 であれば、
            // このオブジェクトに死を与える
            if (_isStopEmittingParticlesAndPrepareToDelete == true && particlesCount == 0)
            {
                _isDead = true;
            }
        }
        #endregion
    }
}
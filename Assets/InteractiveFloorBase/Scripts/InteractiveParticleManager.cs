using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.Rendering;

namespace InteractiveFloor
{
    /// <summary>
    /// インタラクティブパーティクル管理スクリプト
    /// </summary>
    public class InteractiveParticleManager : MonoBehaviour
    {
        #region Enums
        /// <summary>
        /// パーティクルオブジェクトをインスタンス化する際、Prefabを1種類に限定するか、複数にするか
        /// </summary>
        public enum ParticleInstansingMode
        {
            /// <summary>
            /// 1種類
            /// </summary>
            [InspectorName("1種類だけ使用")]
            SinglePrefab,
            /// <summary>
            /// 複数種類Listで指定する
            /// </summary>
            [InspectorName("複数種類使用する")]
            MultiplePrefab
        };
        /// <summary>
        /// 複数種類のPrefabからインスタンスを作成する際、リストに登録されたPrefabをどのように選択するか
        /// </summary>
        public enum MultiplePrefabInstansingMode
        {
            /// <summary>
            /// リストにセットしたオブジェクトを順に生成する
            /// </summary>
            [InspectorName("リストにセットしたオブジェクトを順に生成する")]
            InOrder,
            /// <summary>
            /// リストにセットしたオブジェクトをランダムに生成する
            /// </summary>
            [InspectorName("リストにセットしたオブジェクトをランダムに生成する")]
            Random,
        };
        #endregion

        #region References and Variables
        /// <summary>
        /// センサーオブジェクト管理スクリプトの参照
        /// </summary>
        [Header("Script References")]
        [SerializeField]
        SensorObjectManager _sensorObjectManager = null;
        /// <summary>
        /// インタラクティブパーティクルオブジェクトのPrefab
        /// </summary>
        [Header("Prefabs")]
        [SerializeField, HelpBox("１つだけの場合、ここにParticleSystemのPrefabをセットします。", HelpBoxMessageType.Info)]
        InteractiveParticleObject _interactiveParticleObjectPrefab = null;
        /// <summary>
        /// インタラクティブパーティクルオブジェクトのPrefabのリスト
        /// </summary>
        [SerializeField, HelpBox("複数種類のパーティクルを使用したい場合、ここにParticleSystemのPrefabを１つずつセットします。リストにセットしていないものがあるとエラーが起きます。", HelpBoxMessageType.Info)]
        List<InteractiveParticleObject> _interactiveParticleObjectPrefabList = new List<InteractiveParticleObject>();

        /// <summary>
        /// パーティクルオブジェクトをインスタンス化する際、Prefabを1種類に限定するか、複数にするか
        /// </summary>
        [Header("Parameters")]
        [SerializeField]
        ParticleInstansingMode _particleInstancingMode = ParticleInstansingMode.SinglePrefab;
        /// <summary>
        /// 複数種類のPrefabからインスタンスを作成する際、リストに登録されたPrefabをどのように選択するか
        /// </summary>
        [SerializeField]
        MultiplePrefabInstansingMode _multiplePrefabInstansingMode = MultiplePrefabInstansingMode.InOrder;
        
        /// <summary>
        /// インタラクティブパーティクルオブジェクトのリスト
        /// </summary>
        List<InteractiveParticleObject> _interactiveParticleObjectList = new List<InteractiveParticleObject>();       
        /// <summary>
        /// 生成されたインタラクティブパーティクルオブジェクトの累積数
        /// </summary>
        int _generatedInteractiveParticleObjectCount = 0;
        /// <summary>
        /// 複数種類のパーティクルオブジェクトを生成する際に使用するListのインデックス
        /// </summary>
        [Header("Private Variables (for Debug)")]
        [SerializeField, ReadOnly]
        int _prefabListIndexIncremental = 0;
        #endregion

        #region MonoBehaviour Functions
        void Update()
        {
            // センサーオブジェクト管理スクリプトがセットされていれば
            if (_sensorObjectManager != null)
            {
                // センサーオブジェクトが0以上であれば処理を行う
                if (_sensorObjectManager.SensorList != null && _sensorObjectManager.SensorList.Count > 0)
                {
                    // センサーオブジェクト一つ一つに対して処理を行う
                    for (var i = 0; i < _sensorObjectManager.SensorList.Count; i++)
                    {
                        // （Linq処理に入れ替える？）
                        bool isFoundSameSensorObject = false;
                        for (var j = 0; j < _interactiveParticleObjectList.Count; j++)
                        {
                            if (_interactiveParticleObjectList[j].sensorObject == _sensorObjectManager.SensorList[i])
                            {
                                isFoundSameSensorObject = true;
                            }
                        }

                        if (!isFoundSameSensorObject)
                        {
                            // 新しいパーティクルオブジェクトのインスタンスを作成する
                            InstantiateNewParticleObject(i);
                        }
                    }
                }
            }

            // インタラクティブパーティクルオブジェクト一つ一つに対して処理
            for (var i = _interactiveParticleObjectList.Count - 1; i >= 0; i--)
            {
                // センサーオブジェクトリストの中に、割り当てたものが存在しているか
                // もし、センサーオブジェクトリスト側で既に消されているのであれば、削除対象とする
                if (!_sensorObjectManager.SensorList.Contains(_interactiveParticleObjectList[i].sensorObject))
                {
                    // 削除対象とする
                    _interactiveParticleObjectList[i].SetStopEmittingParticlesAndPrepareToDelete(true);
                }

                if (_interactiveParticleObjectList[i].isDead)
                {
                    if (Application.isEditor)
                        GameObject.DestroyImmediate(_interactiveParticleObjectList[i].gameObject);
                    else
                        GameObject.Destroy(_interactiveParticleObjectList[i].gameObject);

                    _interactiveParticleObjectList.RemoveAt(i);
                }
            }
        }
        #endregion

        #region Private Functions
        /// <summary>
        /// パーティクルオブジェクトのインスタンスを作成する
        /// </summary>
        void InstantiateNewParticleObject(int sensorObjectIndex)
        {
            // 新しいインタラクティブパーティクルオブジェクトの作成
            GameObject go = null;

            // 単一種類
            if (_particleInstancingMode == ParticleInstansingMode.SinglePrefab)
            {
                if (_interactiveParticleObjectPrefab == null)
                {
                    Debug.LogError("ParticleObjectPrefabがセットされていません。");
                }
                go = (GameObject)Instantiate(_interactiveParticleObjectPrefab.gameObject);
            }
            // 複数種類
            else if (_particleInstancingMode == ParticleInstansingMode.MultiplePrefab)
            {
                // インスタンスを作成するPrefabリストのインデックス
                var prefabListIndex = 0;
                // リストにセットしたPrefabを１つずつ使用
                if (_multiplePrefabInstansingMode == MultiplePrefabInstansingMode.InOrder)
                {
                    prefabListIndex = _prefabListIndexIncremental;
                    _prefabListIndexIncremental++; // 1増やす
                    if (_prefabListIndexIncremental >= _interactiveParticleObjectPrefabList.Count)
                    {
                        _prefabListIndexIncremental = 0;
                    }
                }
                // リストにセットしたPrefabをランダムに選択
                else if (_multiplePrefabInstansingMode == MultiplePrefabInstansingMode.Random)
                {
                    prefabListIndex = UnityEngine.Random.Range(0, _interactiveParticleObjectPrefabList.Count);
                }
                // Debug.Log("PrefabIndex : " + prefabListIndex.ToString("00"));
                // Nullチェック
                if (_interactiveParticleObjectPrefabList == null ||
                    _interactiveParticleObjectPrefabList.Count == 0 ||
                    _interactiveParticleObjectPrefabList[prefabListIndex].gameObject == null)
                {
                    Debug.LogError("InteractiveParticleObjectList が セットされていないかもしれないです。");
                }
                go = (GameObject)Instantiate(_interactiveParticleObjectPrefabList[prefabListIndex].gameObject);
            }

            go.name = "InteractiveParticle_" + _generatedInteractiveParticleObjectCount.ToString("000");
            go.transform.parent = transform;
            InteractiveParticleObject ipo = go.GetComponent<InteractiveParticleObject>();
            ipo.SetSensorObject(_sensorObjectManager.SensorList[sensorObjectIndex]);
            // 動的配列に追加
            _interactiveParticleObjectList.Add(go.GetComponent<InteractiveParticleObject>());
            // カウントアップ
            _generatedInteractiveParticleObjectCount++;
        }

        #endregion
    }
}
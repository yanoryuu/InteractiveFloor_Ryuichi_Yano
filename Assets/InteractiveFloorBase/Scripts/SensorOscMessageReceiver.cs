using OscJack;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InteractiveFloor
{
    /// <summary>
    /// センサーのOSCメッセージを受信する
    /// </summary>
    public class SensorOscMessageReceiver : MonoBehaviour
    {
        #region Parameters and Variables
        /// <summary>
        /// センサーオブジェクト管理スクリプトの参照
        /// </summary>
        [Header("Script References")]
        [SerializeField]
        SensorObjectManager _sensorObjectManagerRef = null;
        /// <summary>
        /// OSCポート番号
        /// </summary>
        [Header("Parameters")]
        public int OscPort = 8888;
        /// <summary>
        /// OSCアドレス
        /// </summary>
        public string OscAddress = "/point";
        /// <summary>
        /// 最大メッセージ個数
        /// </summary>
        public int MaxReceiveMessageCount = 128;
        /// <summary>
        /// 1フレーム間に受け取るメッセージの最大数
        /// </summary>
        public int MaxMessagesPerFrame = 16;
        /// <summary>
        /// 受信したメッセージの動的配列
        /// </summary>
        [Header("Private Variables")]
        List<Rect> _receivedMessageList = new List<Rect>();
        /// <summary>
        /// 受信したメッセージを格納するキュー
        /// </summary>
        Queue<Rect> _messageQueue = new Queue<Rect>();
        /// <summary>
        /// キューロックオブジェクト
        /// </summary>
        object _queueLock = new object();
        /// <summary>
        /// OSCサーバー
        /// </summary>
        OscServer _server = null;
        #endregion

        #region MonoBehaviour Functions
        void Start()
        {
            // OSCサーバーを作成
            _server = new OscServer(OscPort);
            // OSCメッセージが送られてきた時の処理を設定
            _server.MessageDispatcher.AddCallback(
                // アドレスを設定
                OscAddress,
                    (string address, OscDataHandle data) =>
                    {
                        lock (_queueLock)
                        {
                            if (_messageQueue.Count < MaxReceiveMessageCount)
                            {
                                _messageQueue.Enqueue(
                                    new Rect(
                                        data.GetElementAsFloat(0),
                                        data.GetElementAsFloat(1),
                                        data.GetElementAsFloat(2),
                                        data.GetElementAsFloat(3)
                                    )
                                );
                            }
                        }
                    }
            );
        }

        void Update()
        {
            // GCやEditor停止直後の古いデータは捨てる
            if (Time.deltaTime > 0.1f)
            {
                lock (_queueLock)
                {
                    _messageQueue.Clear();
                }

                _receivedMessageList.Clear();
                return;
            }

            _receivedMessageList.Clear();

            int count = 0;

            // キューを取り出し、動的配列に入れ替える
            lock (_queueLock)
            {
                while (_messageQueue.Count > 0 && count < MaxMessagesPerFrame)
                {
                    _receivedMessageList.Add(_messageQueue.Dequeue());
                    count++;
                }
            }
            // たまりすぎた古いメッセージは削除する
            if (_messageQueue.Count > MaxReceiveMessageCount / 2)
            {
                _messageQueue.Clear();
            }

            // センサーオブジェクト管理に値を移す
            if (_sensorObjectManagerRef != null)
            {
                _sensorObjectManagerRef.SetValues(_receivedMessageList);
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            lock (_queueLock)
            {
                _messageQueue.Clear();
            }

            _receivedMessageList.Clear();
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                lock (_queueLock)
                {
                    _messageQueue.Clear();
                }

                _receivedMessageList.Clear();
            }
        }

        void OnDestroy()
        {
            // OSCサーバーを削除
            _server?.Dispose();
            _server = null;

            // メッセージキューを削除
            lock (_queueLock)
            {
                _messageQueue.Clear();
            }

            // メッセージを格納する動的配列を削除
            _receivedMessageList.Clear();
        
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// メッセージキューを追加（外から呼び出す）
        /// </summary>
        /// <param name="rect">Rect(位置X, 位置Y, 幅, 高さ)</param>
        public void AddMessageQueue(Rect rect)
        {
            lock (_queueLock)
            {
                if (_messageQueue.Count < MaxReceiveMessageCount)
                {
                    _messageQueue.Enqueue(rect);
                }
            }
        }
        #endregion
    }
}

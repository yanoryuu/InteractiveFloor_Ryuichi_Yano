using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InteractiveFloor
{
    /// <summary>
    /// 
    /// </summary>
    public class SensorMouseSimulator : MonoBehaviour
    {
        /// <summary>
        /// カメラの参照
        /// </summary>
        [Header("Object References")]
        [SerializeField]
        Camera _cameraRef = null;
        /// <summary>
        /// センサーOSCメッセージ受信スクリプトの参照
        /// </summary>
        [Header("Script References")]
        [SerializeField]
        SensorOscMessageReceiver _sensorOscMessageReceiverRef = null;

        /// <summary>
        /// 左マウスボタンをクリックしているかどうか
        /// </summary>
        // [SerializeField, ReadOnly]
        bool _isPressedLeftMouseButton = false;

        void Update()
        {
            // 左マウスボタンを押したか
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                _isPressedLeftMouseButton = true;
            }
            // 左マウスボタンを離したか
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                _isPressedLeftMouseButton = false;
            }

            // 左マウスボタンを押しているかどうか
            if (_isPressedLeftMouseButton)
            {
                if (_sensorOscMessageReceiverRef != null)
                {
                    // OSCメッセージとして追加
                    Camera cam = _cameraRef != null ? _cameraRef : Camera.main;
                    var mousePosition = Mouse.current.position.ReadValue();
                    var mousePositionVS = cam.ScreenToViewportPoint(mousePosition);
                    _sensorOscMessageReceiverRef.AddMessageQueue(new Rect(mousePositionVS.x, mousePositionVS.y, 0.05f, 0.05f));
                }
            }
        }
    }
}

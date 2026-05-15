using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class FPSManager : MonoBehaviour
{

    [Range(0, 240)]
    [Header("Parameters")]
    public int TargetFPS = 60;

    public bool Enable = false;

    [SerializeField]
    Rect _debugRect = new Rect(0, 0, 120, 60);

    public int FontSize = 12;

    [Header("Private Variables")]
    [SerializeField, ReadOnly]
    int _frameCount;
    [SerializeField, ReadOnly]
    float _prevTime;
    [SerializeField, ReadOnly]
    float _fps;

    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFPS;

        _frameCount = 0;
        _prevTime = 0.0f;
    }

    void Update()
    {
        _frameCount++;
        float time = Time.realtimeSinceStartup - _prevTime;

        if (time >= 0.1f)
        {
            _fps = _frameCount / time;
            _frameCount = 0;
            _prevTime = Time.realtimeSinceStartup;
        }
    }

    void OnGUI()
    {
        if (Enable)
        {
            var fontSizeStore = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = FontSize;
            GUI.BeginGroup(_debugRect);
            {
                GUI.Label(new Rect(0, 0, _debugRect.width, _debugRect.height), "FPS : " + _fps.ToString("0.0000"));
            }
            GUI.EndGroup();
            GUI.skin.label.fontSize = fontSizeStore;
        }                
    }
}

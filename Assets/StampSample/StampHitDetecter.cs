using System.Collections.Generic;
using UnityEngine;
using InteractiveFloor;

public class StampHitDetecter : MonoBehaviour
{
    [SerializeField] private SensorObjectManager _sensorObjectManager;
    [SerializeField] private float _stampAreaRadius = 0.5f;

    public void HitDetect(List<StampArea> stampAreas)
    {
        if (_sensorObjectManager == null) return;

        foreach (var sensorObject in _sensorObjectManager.SensorList)
        {
            var sensorPos = new Vector2(sensorObject.positionMedian.x, sensorObject.positionMedian.y);
            foreach (var stamp in stampAreas)
            {
                if (Vector2.Distance(sensorPos, stamp.StampPosition) < _stampAreaRadius)
                    stamp.TryStamp();
            }
        }
    }
}

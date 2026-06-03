using System;
using UnityEngine;

public class StampManager : MonoBehaviour
{
    [SerializeField] private StampHitDetecter stampHitDetecter;
    [SerializeField] private StampGenerator stampGenerator;

    private void Update()
    {
        stampGenerator.Generate();
        stampHitDetecter.HitDetect(stampGenerator.activeStamps);
    }
}

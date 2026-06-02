using InteractiveFloor.Detect;
using UnityEngine;

public class ManuChan : Entity
{
    public override void Move(Vector3 target)
    {
        transform.position = Vector3.Lerp(
            transform.position,  
            target,     
            0.5f * Time.deltaTime
        );
        Position = transform.position;
    }
    
    public override void GrowUpEntity(int stage){
        switch (stage)
        {
            case 0:
                Debug.Log("ManuChan has grown to Youth stage!");
                break;
            case 1:
                Debug.Log("ManuChan has grown to Adult stage!");
                break;
            case 2:
                Debug.Log("ManuChan has reached maximum growth stage!");
                break;
            default:                
                Debug.Log("ManuChan is in Baby stage.");
                break;
        }
    }
    
    public override void OnContactEnter(Entity other)
    {
        if (other == null || other == this)
        {
            return;
        }

        if (other.Type != EntityType.Manuchan)
        {
            return;
        }

        GrowthRate.Value = Mathf.Clamp01(
            GrowthRate.Value + InteractiveFloorConsts.ManuChanGrowthRateIncrease
        );
    }
}

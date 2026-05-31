using InteractiveFloor.Detect;
using UnityEngine;

public class ManuChan : Entity
{
    public override void Move(Vector3 target)
    {
        transform.position = Vector3.Lerp(
            transform.position,  // 現在地
            target,     // 目標地
            0.5f * Time.deltaTime
        );
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
    
    public void MatchOtherManuChan(ManuChan other)
    {
        float distance = Vector3.Distance(this.Position, other.Position);
        if (distance < InteractiveFloorConsts.ManuChanMatchingDistance)
        {
            // 成長率を上げる
            this.ManuChanGrowthRate.Value += 0.05f;
            other.ManuChanGrowthRate.Value += 0.05f;
        }
    }
}

using UnityEngine;
using InteractiveFloor.Detect;

public class People : Entity
{
    
    // 移動
    public override void Move(Vector3 target)
    {
        transform.position = Vector3.Lerp(
            transform.position,  // 現在地
            target,     // 目標地
            Consts.EntityMoveSpeed * Time.deltaTime
        );
    }

    // ダメージを受ける
    public override void TakeDamage(int damage)
    {
        Health -= damage;
        if (Health <= 0)
        {
            Die();
        }
    }
}

using UnityEngine;

public class People : Entity
{
    
    // 移動
    public override void Move(Vector3 direction)
    {
        
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

    // 死亡
    public override void Die()
    {
        Debug.Log($"{Type} has died.");
        ReturnToPool();
    }
}

using UnityEngine;
public abstract class Entity : MonoBehaviour
{
    public EntityType Type { get; set; }
    public Vector3 Position { get; set; }
    public int Health { get; set; }

    public void Spawn(Vector3 position, int health, EntityType type)
    {
        Type = type;
        Health = health;
        Position = position;
        transform.position = position;
    }

    protected void ReturnToPool()
    {
        EntityManager.Instance.ReturnEntity(this);
    }

    public abstract void Move(Vector3 direction);
    public abstract void TakeDamage(int damage);
    public abstract void Die();
}

public enum EntityType
{
    People,
    Enemy
}
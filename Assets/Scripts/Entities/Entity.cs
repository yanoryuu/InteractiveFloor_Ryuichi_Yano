using UnityEngine;
public abstract class Entity : MonoBehaviour
{
    public EntityType Type { get; set; }
    public Vector3 Position { get; set; }
    public int Health { get; set; }
    public bool IsActive { get; private set; }

    public void Spawn(Vector3 position, int health, EntityType type)
    {
        Type = type;
        Health = health;
        Position = position;
        transform.position = position;
        IsActive = true;
    }

    protected void ReturnToPool()
    {
        EntityManager.Instance.ReturnEntity(this);
    }

    public abstract void Move(Vector3 target);
    public abstract void TakeDamage(int damage);

    public void Die()
    {
        Debug.Log($"{Type} has died.");
        IsActive = false;
        ReturnToPool();
    }
}

public enum EntityType
{
    People,
    Enemy
}
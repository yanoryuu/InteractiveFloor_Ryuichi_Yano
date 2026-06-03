using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

public abstract class Entity : MonoBehaviour
{
    public EntityType Type { get; set; }
    public Vector3 Position { get; set; }
    public int Health { get; set; }
    public bool IsActive { get; private set; }

    public HashSet<Entity> IsMetEntities { get; } = new();

    public ReactiveProperty<float> GrowthRate { get; set; } = new(0.0f);
    public ReactiveProperty<int> GrowthStage { get; private set; } = new(0);

    private CompositeDisposable disposables = new();

    public void Spawn(Vector3 position, int health, EntityType type, Vector3? localScale = null)
    {
        Type = type;
        Health = health;
        Position = position;
        transform.position = position;
        if (localScale.HasValue)
            transform.localScale = localScale.Value;
        IsActive = true;
        IsMetEntities.Clear();

        // Bind前にリセットすることで、Subscribe の初回発火が正しい値で行われる
        if (type == EntityType.Manuchan)
            GrowthRate.Value = 0.0f;

        Bind();
    }

    private void Bind()
    {
        GrowthRate.Subscribe(rate =>
        {
            GrowthStage.Value = rate < 0.3f ? 0 : rate < 0.7f ? 1 : 2;
        }).AddTo(disposables);

        GrowthStage.Subscribe(stage =>
        {
            GrowUpEntity(stage);
        }).AddTo(disposables);
    }

    public abstract void GrowUpEntity(int stage);

    public virtual void OnContactEnter(Entity other) { }
    public virtual void OnContactStay(Entity other) { }
    public virtual void OnContactExit(Entity other) { }

    protected void ReturnToPool()
    {
        EntityManager.Instance.ReturnEntity(this);
    }

    public abstract void Move(Vector3 target);

    public void Die()
    {
        IsActive = false;
        disposables.Clear();
        ReturnToPool();
    }
}

public enum EntityType
{
    Manuchan,
    People,
    Enemy
}

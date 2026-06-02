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

    public ReactiveProperty<float> GrowthRate { get; set; } = new(0.0f); // 成長率

    public ReactiveProperty<int> GrowthStage { get; private set; } = new(0); // 成長段階（0: 幼年, 1: 青年, 2: 成人）
    
    private CompositeDisposable disposables = new CompositeDisposable();
    public void Spawn(Vector3 position, int health, EntityType type)
    {
        Type = type;
        Health = health;
        Position = position;
        transform.position = position;
        IsActive = true;
        IsMetEntities.Clear();
        
        Bind();
        
        if (type == EntityType.Manuchan)
        {
            GrowthStage.Value = 0; // 幼年スタート
            GrowthRate.Value = 0.1f; // 初期成長率
        }
    }

    private void Bind()
    {
        GrowthRate.Subscribe(rate =>
        {
            GrowthStage.Value = rate < 0.3f ? 0 : rate < 0.7f ? 1 : 2; // 成長率に応じて成長段階を更新
        })
            .AddTo(disposables);
        
        GrowthStage.Subscribe(stage =>
            {
                GrowUpEntity(stage);
            })
            .AddTo(disposables);

        Observable.EveryUpdate()
            .Subscribe(_ =>
            {
                
            })
            .AddTo(disposables);
    }

    public abstract void GrowUpEntity(int stage);

    public virtual void OnContactEnter(Entity other)
    {
    }

    public virtual void OnContactStay(Entity other)
    {
    }

    public virtual void OnContactExit(Entity other)
    {
    }

    protected void ReturnToPool()
    {
        EntityManager.Instance.ReturnEntity(this);
    }

    public abstract void Move(Vector3 target);

    public void Die()
    {
        Debug.Log($"{Type} has died.");
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

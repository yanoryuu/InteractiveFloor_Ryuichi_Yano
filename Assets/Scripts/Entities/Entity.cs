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

    // GrowthRate から GrowthStage を決めるしきい値（0=幼年 / 1=青年 / 2=成人）
    public const float Stage1Threshold = 0.3f;
    public const float Stage2Threshold = 0.7f;

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
            GrowthStage.Value = rate < Stage1Threshold ? 0 : rate < Stage2Threshold ? 1 : 2;
        }).AddTo(disposables);

        GrowthStage.Subscribe(stage =>
        {
            GrowUpEntity(stage);
        }).AddTo(disposables);
    }

    public abstract void GrowUpEntity(int stage);

    /// <summary>
    /// 現在の成長段階から一気に次の段階へ進化させる。
    /// GrowthRate を次段階のしきい値まで引き上げることで、
    /// GrowthStage の購読経由で GrowUpEntity（進化アニメ）が発火する。
    /// すでに最終段階(2)なら何もしない。
    /// </summary>
    public void AdvanceGrowthStage()
    {
        var stage = GrowthStage.Value;
        if (stage >= 2) return;

        var nextRate = stage == 0 ? Stage1Threshold : Stage2Threshold;
        if (GrowthRate.Value < nextRate)
            GrowthRate.Value = nextRate;
    }

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

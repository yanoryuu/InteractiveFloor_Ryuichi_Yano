using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntitySpawnSettings", menuName = "InteractiveFloor/Entity Spawn Settings")]
public class EntitySpawnSettings : ScriptableObject
{
    [Serializable]
    public class EntityTypeConfig
    {
        public EntityType type;
        public Entity prefab;
        public int initialSize = 10;
        public Vector3 localScale = Vector3.one;
    }

    [SerializeField] private List<EntityTypeConfig> entities = new();

    public IEnumerable<EntityTypeConfig> Entities => entities;

    public bool TryGetConfig(EntityType type, out EntityTypeConfig config)
    {
        config = entities.Find(e => e.type == type);
        return config != null && config.prefab != null;
    }
}

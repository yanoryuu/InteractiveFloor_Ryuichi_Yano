using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntitySpawnSettings", menuName = "InteractiveFloor/Entity Spawn Settings")]
public class EntitySpawnSettings : ScriptableObject
{
    [Serializable]
    public class EntityLevelConfig
    {
        public int level;
        public Entity prefab;
        public int initialSize = 10;
        public Vector3 localScale = Vector3.one;
    }

    [Serializable]
    public class EntityTypeConfig
    {
        public EntityType type;
        public List<EntityLevelConfig> levels = new();
    }

    [SerializeField] private List<EntityTypeConfig> entities = new();

    public IEnumerable<EntityTypeConfig> Entities => entities;

    public bool TryGetLevelConfig(EntityType type, int level, out EntityLevelConfig config)
    {
        config = null;

        var typeConfig = entities.Find(entry => entry.type == type);
        if (typeConfig == null || typeConfig.levels == null || typeConfig.levels.Count == 0)
        {
            return false;
        }

        config = typeConfig.levels.Find(entry => entry.level == level)
                 ?? typeConfig.levels.Find(entry => entry.level == 0)
                 ?? typeConfig.levels[0];

        return config != null && config.prefab != null;
    }
}

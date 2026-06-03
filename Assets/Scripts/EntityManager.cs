using System.Collections.Generic;
using InteractiveFloor.Detect;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
    public static EntityManager Instance { get; private set; }
    [SerializeField] private EntitySpawnSettings entitySpawnSettings;

    private readonly Dictionary<EntityPoolKey, Queue<Entity>> pools = new();
    private readonly Dictionary<EntityPoolKey, Entity> prefabs = new();
    private readonly Dictionary<EntityPoolKey, Vector3> spawnLocalScales = new();
    private readonly HashSet<Entity> activeEntities = new();

    private readonly struct EntityPoolKey
    {
        public EntityPoolKey(EntityType type, int level)
        {
            Type = type;
            Level = level;
        }

        public EntityType Type { get; }
        public int Level { get; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (entitySpawnSettings == null)
        {
            Debug.LogError("EntitySpawnSettings is not assigned.");
            return;
        }

        foreach (var typeConfig in entitySpawnSettings.Entities)
        {
            if (typeConfig.levels == null)
            {
                continue;
            }

            foreach (var levelConfig in typeConfig.levels)
            {
                if (levelConfig.prefab == null)
                {
                    continue;
                }

                var key = new EntityPoolKey(typeConfig.type, levelConfig.level);
                prefabs[key] = levelConfig.prefab;
                spawnLocalScales[key] = levelConfig.localScale;
                pools[key] = new Queue<Entity>();

                for (int i = 0; i < levelConfig.initialSize; i++)
                {
                    var entity = InstantiateEntity(key);
                    if (entity == null)
                    {
                        continue;
                    }

                    entity.gameObject.SetActive(false);
                    pools[key].Enqueue(entity);
                }
            }
        }
    }

    private Entity InstantiateEntity(EntityPoolKey key)
    {
        var entityObject = Instantiate(prefabs[key].gameObject, transform, false);
        entityObject.SetActive(false);
        entityObject.transform.localScale = spawnLocalScales[key];

        var entity = entityObject.GetComponent<Entity>();
        if (entity == null)
        {
            Debug.LogError($"Prefab for EntityType: {key.Type}, Level: {key.Level} does not have an Entity component on its root.");
        }

        return entity;
    }

    public T GetEntity<T>(EntityType type, Vector3 position, int health, int level = 0) where T : Entity
    {
        if (!TryResolvePoolKey(type, level, out var poolKey))
        {
            Debug.LogError($"No pool configured for EntityType: {type}, Level: {level}");
            return null;
        }

        var pool = pools[poolKey];
        var entity = pool.Count > 0 ? pool.Dequeue() : InstantiateEntity(poolKey);
        if (entity == null)
        {
            return null;
        }

        var spawnLocalScale = spawnLocalScales[poolKey];
        entity.Spawn(position, health, type, poolKey.Level, spawnLocalScale);
        entity.gameObject.SetActive(true);
        activeEntities.Add(entity);
        return entity as T;
    }

    public void ReturnEntity(Entity entity)
    {
        ReleaseContacts(entity);
        activeEntities.Remove(entity);
        entity.gameObject.SetActive(false);
        var key = new EntityPoolKey(entity.Type, entity.Level);
        if (pools.TryGetValue(key, out var pool))
            pool.Enqueue(entity);
        else
            Destroy(entity.gameObject);
    }

    public void ProcessContacts()
    {
        var entities = new List<Entity>(activeEntities);

        for (var leftIndex = 0; leftIndex < entities.Count; leftIndex++)
        {
            var left = entities[leftIndex];
            if (left == null || !left.gameObject.activeInHierarchy)
            {
                continue;
            }

            for (var rightIndex = leftIndex + 1; rightIndex < entities.Count; rightIndex++)
            {
                var right = entities[rightIndex];
                if (right == null || !right.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var isContacting = IsContacting(left, right);
                var isAlreadyContacting = left.IsMetEntities.Contains(right) && right.IsMetEntities.Contains(left);

                if (isContacting)
                {
                    if (!isAlreadyContacting)
                    {
                        left.IsMetEntities.Add(right);
                        right.IsMetEntities.Add(left);
                        left.OnContactEnter(right);
                        right.OnContactEnter(left);
                    }
                    else
                    {
                        left.OnContactStay(right);
                        right.OnContactStay(left);
                    }
                }
                else if (isAlreadyContacting || left.IsMetEntities.Contains(right) || right.IsMetEntities.Contains(left))
                {
                    left.IsMetEntities.Remove(right);
                    right.IsMetEntities.Remove(left);
                    left.OnContactExit(right);
                    right.OnContactExit(left);
                }
            }
        }
    }

    private bool IsContacting(Entity left, Entity right)
    {
        return Vector3.Distance(left.transform.position, right.transform.position) <= InteractiveFloorConsts.EntityContactDistance;
    }

    private void ReleaseContacts(Entity entity)
    {
        var metEntities = new List<Entity>(entity.IsMetEntities);
        foreach (var other in metEntities)
        {
            entity.IsMetEntities.Remove(other);

            if (other != null)
            {
                other.IsMetEntities.Remove(entity);
                other.OnContactExit(entity);
            }

            entity.OnContactExit(other);
        }
    }

    private bool TryResolvePoolKey(EntityType type, int level, out EntityPoolKey key)
    {
        key = new EntityPoolKey(type, level);
        if (pools.ContainsKey(key))
        {
            return true;
        }

        if (entitySpawnSettings != null && entitySpawnSettings.TryGetLevelConfig(type, level, out var config))
        {
            key = new EntityPoolKey(type, config.level);
            return pools.ContainsKey(key);
        }

        return false;
    }

}

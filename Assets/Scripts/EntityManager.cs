using System.Collections.Generic;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
    public static EntityManager Instance { get; private set; }

    [System.Serializable]
    public class EntityPoolConfig
    {
        public EntityType type;
        public Entity prefab;
        public int initialSize = 10;
    }

    [SerializeField] private List<EntityPoolConfig> poolConfigs;

    private readonly Dictionary<EntityType, Queue<Entity>> pools = new();
    private readonly Dictionary<EntityType, Entity> prefabs = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        foreach (var config in poolConfigs)
        {
            prefabs[config.type] = config.prefab;
            pools[config.type] = new Queue<Entity>();

            for (int i = 0; i < config.initialSize; i++)
            {
                var entity = InstantiateEntity(config.type);
                entity.gameObject.SetActive(false);
                pools[config.type].Enqueue(entity);
            }
        }
    }

    private Entity InstantiateEntity(EntityType type)
    {
        return Instantiate(prefabs[type], transform);
    }

    public T GetEntity<T>(EntityType type, Vector3 position, int health) where T : Entity
    {
        if (!pools.TryGetValue(type, out var pool))
        {
            Debug.LogError($"No pool configured for EntityType: {type}");
            return null;
        }

        var entity = pool.Count > 0 ? pool.Dequeue() : InstantiateEntity(type);
        entity.gameObject.SetActive(true);
        entity.Spawn(position, health, type);
        return entity as T;
    }

    public void ReturnEntity(Entity entity)
    {
        entity.gameObject.SetActive(false);
        if (pools.TryGetValue(entity.Type, out var pool))
            pool.Enqueue(entity);
        else
            Destroy(entity.gameObject);
    }
}

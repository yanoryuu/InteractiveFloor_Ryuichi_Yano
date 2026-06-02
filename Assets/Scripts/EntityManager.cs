using System.Collections.Generic;
using InteractiveFloor.Detect;
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
    private readonly HashSet<Entity> activeEntities = new();

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
        activeEntities.Add(entity);
        return entity as T;
    }

    public void ReturnEntity(Entity entity)
    {
        ReleaseContacts(entity);
        activeEntities.Remove(entity);
        entity.gameObject.SetActive(false);
        if (pools.TryGetValue(entity.Type, out var pool))
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
}

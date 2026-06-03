using System.Collections.Generic;
using InteractiveFloor;
using InteractiveFloor.Detect;
using UnityEngine;

public class InteractiveView : MonoBehaviour
{
    [SerializeField] private EntityManager _entityManager;

    private void Awake()
    {
        if (_entityManager == null)
            _entityManager = FindObjectOfType<EntityManager>();
    }

    private readonly Dictionary<string, Entity> _activeEntities = new();

    public void SyncEntities(List<Viewer> viewers)
    {
        var seenLabels = new HashSet<string>();

        foreach (var viewer in viewers)
        {
            seenLabels.Add(viewer.Label);

            if (!_activeEntities.TryGetValue(viewer.Label, out var entity) || entity == null)
            {
                entity = _entityManager.GetEntity<Entity>(EntityType.Manuchan, viewer.Position, 100);
                if (entity == null) continue;
                _activeEntities[viewer.Label] = entity;
            }

            entity.Move(viewer.Position);
        }

        var labelsToRemove = new List<string>();
        foreach (var pair in _activeEntities)
        {
            if (!seenLabels.Contains(pair.Key))
            {
                pair.Value?.Die();
                labelsToRemove.Add(pair.Key);
            }
        }

        foreach (var label in labelsToRemove)
            _activeEntities.Remove(label);
    }
}

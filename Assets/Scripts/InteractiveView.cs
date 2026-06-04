using System.Collections.Generic;
using InteractiveFloor.Detect;
using UnityEngine;

public class InteractiveView : MonoBehaviour
{
    private readonly List<Vector3> _viewerPositions = new();
    public IReadOnlyList<Vector3> ViewerPositions => _viewerPositions;

    public void SyncViewers(List<Viewer> viewers)
    {
        _viewerPositions.Clear();
        foreach (var viewer in viewers)
            _viewerPositions.Add(viewer.Position);
    }
}

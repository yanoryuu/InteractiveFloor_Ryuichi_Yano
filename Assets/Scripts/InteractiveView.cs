using System.Collections.Generic;
using InteractiveFloor.Detect;
using UnityEngine;

namespace InteractiveFloor
{
    /// <summary>
    /// Viewer の位置リストを保持する純粋な C# クラス。
    /// MonoBehaviour 不要なためシーンへの配置・Unity管理オーバーヘッドなし。
    /// </summary>
    public class InteractiveView
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
}

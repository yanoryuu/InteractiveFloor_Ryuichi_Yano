using System.Collections.Generic;
using InteractiveFloor;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace InteractiveFloor.Detect
{
    public class DetectViewerManager
    {
        private const float MatchingThreshold = InteractiveFloorConsts.MatchingThreshold;

        private int _nextLabelIndex = 1;
        private readonly List<TrackedSensorSnapshot> _previousSensors = new();

        // 毎フレームのアロケーションを防ぐキャッシュ
        private readonly List<Viewer> _viewers = new();
        private readonly HashSet<int> _usedPreviousSensorIndices = new();
        private readonly List<TrackedSensorSnapshot> _currentSnapshots = new();
        // ラベルをキーにViewerオブジェクトを再利用（新規Viewer追加時のみ生成）
        private readonly Dictionary<string, Viewer> _viewerCache = new();

        public List<Viewer> GetViewer(List<SensorObjectManager.SensorObject> sensorObjects)
        {
            _viewers.Clear();
            _usedPreviousSensorIndices.Clear();
            _currentSnapshots.Clear();

            if (sensorObjects == null || sensorObjects.Count == 0)
            {
                _previousSensors.Clear();
                return _viewers;
            }

            foreach (var sensorObject in sensorObjects)
            {
                var matchedPreviousIndex = FindNearestPreviousSensorIndex(
                    sensorObject.positionMedian,
                    _usedPreviousSensorIndices);

                string label;
                ViewerType viewerType;

                if (matchedPreviousIndex >= 0)
                {
                    label = _previousSensors[matchedPreviousIndex].Label;
                    viewerType = _previousSensors[matchedPreviousIndex].ViewerType;
                }
                else
                {
                    // 新規Viewer追加時のみ文字列を生成する
                    var idx = _nextLabelIndex++;
                    label = $"Person {idx}";
                    viewerType = idx % 2 == 0 ? ViewerType.Demon : ViewerType.God;
                }

                sensorObject.TrackingLabel = label;

                var position = ToVector3(sensorObject.positionMedian);

                // Viewerオブジェクトを再利用。新規ラベルのときだけ生成。
                if (!_viewerCache.TryGetValue(label, out var viewer))
                {
                    viewer = new Viewer(label, viewerType, position);
                    _viewerCache[label] = viewer;
                }
                else
                {
                    viewer.Position = position;
                }

                _viewers.Add(viewer);

                _currentSnapshots.Add(new TrackedSensorSnapshot
                {
                    Position = sensorObject.positionMedian,
                    Label = label,
                    ViewerType = viewerType,
                });

                if (matchedPreviousIndex >= 0)
                    _usedPreviousSensorIndices.Add(matchedPreviousIndex);
            }

            _previousSensors.Clear();
            _previousSensors.AddRange(_currentSnapshots);

            return _viewers;
        }

        private int FindNearestPreviousSensorIndex(float2 currentPosition, HashSet<int> usedPreviousSensorIndices)
        {
            var nearestIndex = -1;
            var nearestDistance = float.MaxValue;

            for (var index = 0; index < _previousSensors.Count; index++)
            {
                if (usedPreviousSensorIndices.Contains(index)) continue;

                var distanceToPrevious = distance(_previousSensors[index].Position, currentPosition);
                if (distanceToPrevious > MatchingThreshold || distanceToPrevious >= nearestDistance) continue;

                nearestDistance = distanceToPrevious;
                nearestIndex = index;
            }

            return nearestIndex;
        }

        private struct TrackedSensorSnapshot
        {
            public float2 Position;
            public string Label;
            public ViewerType ViewerType;
        }

        private static Vector3 ToVector3(float2 position)
        {
            return new Vector3(position.x, 0, position.y);
        }
    }
}

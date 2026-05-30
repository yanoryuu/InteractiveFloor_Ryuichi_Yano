using System.Collections.Generic;
using InteractiveFloor;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace InteractiveFloor.Detect
{
    public class DetectViewerManager
    {
        private const float MatchingThreshold = Consts.MatchingThreshold;

        private int _nextLabelIndex = 1;
        private readonly List<TrackedSensorSnapshot> _previousSensors = new();

        public List<Viewer> GetViewer(List<SensorObjectManager.SensorObject> sensorObjects)
        {
            var viewers = new List<Viewer>();

            if (sensorObjects == null || sensorObjects.Count == 0)
            {
                _previousSensors.Clear();
                return viewers;
            }

            var usedPreviousSensorIndices = new HashSet<int>();
            var currentSnapshots = new List<TrackedSensorSnapshot>(sensorObjects.Count);

            foreach (var sensorObject in sensorObjects)
            {
                var matchedPreviousIndex = FindNearestPreviousSensorIndex(
                    sensorObject.positionMedian,
                    usedPreviousSensorIndices);

                var labelIndex = _nextLabelIndex;
                var label = matchedPreviousIndex >= 0
                    ? _previousSensors[matchedPreviousIndex].Label
                    : $"Person {_nextLabelIndex++}";

                var viewerType = matchedPreviousIndex >= 0
                    ? _previousSensors[matchedPreviousIndex].ViewerType
                    : (labelIndex % 2 == 0 ? ViewerType.Demon : ViewerType.God);

                sensorObject.TrackingLabel = label;

                viewers.Add(new Viewer(label, viewerType, ToVector3(sensorObject.positionMedian)));

                currentSnapshots.Add(new TrackedSensorSnapshot
                {
                    Position = sensorObject.positionMedian,
                    Label = label,
                    ViewerType = viewerType,
                });

                if (matchedPreviousIndex >= 0)
                {
                    usedPreviousSensorIndices.Add(matchedPreviousIndex);
                }
            }

            _previousSensors.Clear();
            _previousSensors.AddRange(currentSnapshots);

            return viewers;
        }

        private int FindNearestPreviousSensorIndex(float2 currentPosition, HashSet<int> usedPreviousSensorIndices)
        {
            var nearestIndex = -1;
            var nearestDistance = float.MaxValue;

            for (var index = 0; index < _previousSensors.Count; index++)
            {
                if (usedPreviousSensorIndices.Contains(index))
                {
                    continue;
                }

                var distanceToPrevious = distance(_previousSensors[index].Position, currentPosition);
                if (distanceToPrevious > MatchingThreshold || distanceToPrevious >= nearestDistance)
                {
                    continue;
                }

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

        private Vector3 ToVector3(float2 position)
        {
            return new Vector3(position.x, 0, position.y);
        }
    }
}

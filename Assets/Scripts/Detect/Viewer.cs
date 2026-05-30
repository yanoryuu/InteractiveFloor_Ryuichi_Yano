using UnityEngine;

namespace InteractiveFloor.Detect
{
    public class Viewer
    {
        public string Label { get; private set; }
        public ViewerType Type { get; private set; }
        public Vector3 Position { get; set; }

        public Viewer(string label, ViewerType type, Vector3 position)
        {
            Label = label;
            Type = type;
            Position = position;
        }
    }

    public enum ViewerType
    {
        God,
        Demon
    }
}
namespace JayTom.Dws.Camera.Attributes.CameraAttributes {

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class CameraLabelColorAttribute : Attribute {
        public string Color { get; }

        public CameraLabelColorAttribute(string color) {
            Color = color;
        }
    }
}
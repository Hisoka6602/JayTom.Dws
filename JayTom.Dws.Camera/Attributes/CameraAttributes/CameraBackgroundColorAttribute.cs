namespace JayTom.Dws.Camera.Attributes.CameraAttributes {

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class CameraBackgroundColorAttribute : Attribute {
        public string Color { get; }

        public CameraBackgroundColorAttribute(string color) {
            Color = color;
        }
    }
}
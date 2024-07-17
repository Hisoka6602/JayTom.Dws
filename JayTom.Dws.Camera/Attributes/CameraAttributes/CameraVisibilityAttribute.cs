namespace JayTom.Dws.Camera.Attributes.CameraAttributes {

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class CameraVisibilityAttribute : Attribute {
        public bool Visibility { get; }

        public CameraVisibilityAttribute(bool visibility) {
            Visibility = visibility;
        }
    }
}
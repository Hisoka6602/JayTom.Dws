namespace JayTom.Dws.Camera.Attributes.CameraAttributes {

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class CameraFontIconAttribute : Attribute {
        public string Content { get; }

        public CameraFontIconAttribute(string content) {
            Content = content;
        }
    }
}
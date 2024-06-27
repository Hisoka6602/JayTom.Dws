namespace JayTom.Dws.CrossCutting.Attributes.WinClientAttributes {

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class BackgroundColorAttribute : Attribute {
        public string Color { get; }

        public BackgroundColorAttribute(string color) {
            Color = color;
        }
    }
}
namespace JayTom.Dws.CrossCutting.Attributes.WinClientAttributes {

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class LabelColorAttribute : Attribute {
        public string Color { get; }

        public LabelColorAttribute(string color) {
            Color = color;
        }
    }
}
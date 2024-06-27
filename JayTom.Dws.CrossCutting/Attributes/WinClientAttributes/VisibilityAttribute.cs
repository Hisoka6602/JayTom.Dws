namespace JayTom.Dws.CrossCutting.Attributes.WinClientAttributes {

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class EnumToVisibilityAttribute : Attribute {
        public bool Visibility { get; }

        public EnumToVisibilityAttribute(bool visibility) {
            Visibility = visibility;
        }
    }
}
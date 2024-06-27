namespace JayTom.Dws.CrossCutting.Attributes.WinClientAttributes {

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal class TypeAbbreviationAttribute : Attribute {
        public string Abbreviation { get; }

        public TypeAbbreviationAttribute(string abbreviation) {
            Abbreviation = abbreviation;
        }
    }
}
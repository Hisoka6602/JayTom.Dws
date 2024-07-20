using System;

namespace JayTom.Dws.Client.Attributes.WinClientAttributes {

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class AuxiliaryDescriptionAttribute : Attribute {
        public string Description { get; }

        public AuxiliaryDescriptionAttribute(string description) {
            Description = description;
        }
    }
}
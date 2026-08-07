using System;

namespace JayTom.Dws.Client.Attributes.WinClientAttributes
{

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class LabelColorAttribute : Attribute
    {
        public string Color { get; }

        public LabelColorAttribute(string color)
        {
            Color = color;
        }
    }
}
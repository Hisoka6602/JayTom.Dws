using System;

namespace JayTom.Dws.Client.Attributes.WinClientAttributes
{

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class FontIconAttribute : Attribute
    {
        public string Content { get; }

        public FontIconAttribute(string content)
        {
            Content = content;
        }
    }
}
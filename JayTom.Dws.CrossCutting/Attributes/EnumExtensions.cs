using System.ComponentModel;

namespace JayTom.Dws.CrossCutting.Attributes {

    public static class EnumExtensions {

        public static string GetDescription(this Enum value) {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .Cast<DescriptionAttribute>()
                .FirstOrDefault();
            return attribute?.Description ?? value.ToString();
        }
    }
}
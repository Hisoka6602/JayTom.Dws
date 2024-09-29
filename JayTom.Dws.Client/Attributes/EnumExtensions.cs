using System;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.ComponentModel;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Attributes.VideoAttribute;
using JayTom.Dws.Client.Attributes.WinClientAttributes;

namespace JayTom.Dws.Client.Attributes {

    public static class EnumExtensions {

        public static string GetDescription(this Enum value) {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .Cast<DescriptionAttribute>()
                .FirstOrDefault();
            return attribute?.Description ?? value.ToString();
        }

        public static string GetAuxiliaryDescription(this Enum value) {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(AuxiliaryDescriptionAttribute), false)
                .Cast<AuxiliaryDescriptionAttribute>()
                .FirstOrDefault();
            return attribute?.Description ?? value.ToString();
        }

        public static string GetBackgroundColor(this Enum value) {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(BackgroundColorAttribute), false)
                .Cast<BackgroundColorAttribute>()
                .FirstOrDefault();
            return attribute?.Color ?? string.Empty;
        }

        public static string GetFontIcon(this Enum value) {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(FontIconAttribute), false)
                .Cast<FontIconAttribute>()
                .FirstOrDefault();
            return attribute?.Content ?? string.Empty;
        }

        public static string GetLabelColor(this Enum value) {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(LabelColorAttribute), false)
                .Cast<LabelColorAttribute>()
                .FirstOrDefault();
            return attribute?.Color ?? string.Empty;
        }

        public static string GetTypeAbbreviation(this Enum value) {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(TypeAbbreviationAttribute), false)
                .Cast<TypeAbbreviationAttribute>()
                .FirstOrDefault();
            return attribute?.Abbreviation ?? string.Empty;
        }

        public static bool GetVisibility(this Enum value) {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(EnumToVisibilityAttribute), false)
                .Cast<EnumToVisibilityAttribute>()
                .FirstOrDefault();
            return attribute?.Visibility ?? false;
        }

        public static Size GetResolution(this Enum value) {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(ResolutionAttribute), false)
                .Cast<ResolutionAttribute>()
                .FirstOrDefault();
            return attribute is null ? Size.Empty : new Size(attribute.Width, attribute.Height);
        }
    }
}
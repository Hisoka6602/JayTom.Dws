using System.ComponentModel;
using JayTom.Dws.Camera.Attributes.CameraAttributes;

namespace JayTom.Dws.Camera.Attributes {

    public static class CameraEnumExtensions {

        public static string GetCameraBackgroundColor(this Enum value) {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(CameraBackgroundColorAttribute), false)
                .Cast<CameraBackgroundColorAttribute>()
                .FirstOrDefault();
            return attribute?.Color ?? string.Empty;
        }

        public static string GetCameraFontIcon(this Enum value) {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(CameraFontIconAttribute), false)
                .Cast<CameraFontIconAttribute>()
                .FirstOrDefault();
            return attribute?.Content ?? string.Empty;
        }

        public static string GetCameraLabelColor(this Enum value) {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(CameraLabelColorAttribute), false)
                .Cast<CameraLabelColorAttribute>()
                .FirstOrDefault();
            return attribute?.Color ?? string.Empty;
        }

        public static bool GetCameraVisibility(this Enum value) {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(CameraVisibilityAttribute), false)
                .Cast<CameraVisibilityAttribute>()
                .FirstOrDefault();
            return attribute?.Visibility ?? false;
        }
    }
}
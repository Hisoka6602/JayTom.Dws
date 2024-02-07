namespace JayTom.Dws.LicenseApi.Utils {

    public class DataUtils {

        public static string MaskPhoneNumber(string phoneNumber) {
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length < 8) {
                return "Invalid phone number";
            }

            var firstPart = phoneNumber.Substring(0, 3);
            var lastPart = phoneNumber.Substring(phoneNumber.Length - 4, 4);

            return $"{firstPart}****{lastPart}";
        }
    }
}
namespace JayTom.Dws.LicenseApiClient.Notification {

    public class NotificationService {

        private event Action<object>? OnNotify;

        public void Subscribe<T>(Action<T> action) {
            OnNotify += (data) => {
                if (data is T tData) {
                    action(tData);
                }
            };
        }

        public void Unsubscribe<T>(Action<T> action) {
            if (OnNotify is not null) {
                OnNotify -= (data) => {
                    action((T)data);
                };
            }
        }

        public void Notify<T>(T data) {
            if (data != null) OnNotify?.Invoke(data);
        }
    }
}
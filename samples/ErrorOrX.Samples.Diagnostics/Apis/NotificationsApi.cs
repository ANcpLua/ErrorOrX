using ErrorOrX.Samples.Diagnostics.Services;

namespace ErrorOrX.Samples.Diagnostics.Apis;

public static class NotificationsApi
{
    [Post("/api/notify/{to}")]
    public static ErrorOr<Success> Notify(string to, string message, INotificationService svc)
        => svc.Send(to, message);
}

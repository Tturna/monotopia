using Godot;

public class NotificationToast
{
    public Control ToastControl { get; private set; }
    public float DurationLeft;

    public NotificationToast(Control toastControl, float duration)
    {
        ToastControl = toastControl;
        DurationLeft = duration;
    }
}

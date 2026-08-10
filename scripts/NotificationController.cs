using System.Collections.Generic;
using Godot;

#nullable enable
public partial class NotificationController : Node
{
    [Export]
    private Node notificationsParent = null!;
    [Export]
    private PackedScene notificationToastScene = null!;

    private Queue<NotificationToast> activeNotificationToasts = new();
    private NotificationToast? visibleNotificationToast;

    public override void _Process(double delta)
    {
        if (activeNotificationToasts.Count == 0 && visibleNotificationToast is null) return;

        if (visibleNotificationToast is null)
        {
            var toast = activeNotificationToasts.Peek();
            visibleNotificationToast = toast;
            visibleNotificationToast.ToastControl.Show();
        }

        visibleNotificationToast.DurationLeft -= (float)delta;

        if (visibleNotificationToast.DurationLeft <= 0)
        {
            visibleNotificationToast.ToastControl.Hide();
            visibleNotificationToast.ToastControl.QueueFree();
            visibleNotificationToast = null;
            activeNotificationToasts.Dequeue();
        }
    }

    public void ShowNotificationToast(string title, string body, float duration)
    {
        var toastControl = notificationToastScene.Instantiate<Control>();
        var toastTitle = (Label)toastControl.FindChild("Toast Title");
        var toastBody = (Label)toastControl.FindChild("Toast Body");
        toastTitle.Text = title;
        toastBody.Text = body;
        toastControl.Hide();
        notificationsParent.AddChild(toastControl);
        activeNotificationToasts.Enqueue(new (toastControl, duration));
    }
}

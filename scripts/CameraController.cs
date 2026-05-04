using Godot;

public partial class CameraController : Node2D
{
    [Export]
    public float PanSpeed;

    private Camera2D camera;
    private float minZoom = 0.3f;
    private float maxZoom = 5f;

    public override void _Ready()
    {
        camera = (Camera2D)GetNode("./Camera2D");
    }

    public override void _Process(double delta)
    {
        var inputVector = Input.GetVector("panLeft", "panRight", "panUp", "panDown");
        var normalizedZoom = GetNormalizedZoom();
        var panMagnitude = Mathf.Clamp(1f - normalizedZoom / 1.333333f, 0.25f, 1f);
        var totalPanSpeed = PanSpeed * panMagnitude;

        Translate(inputVector * totalPanSpeed * (float)delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton buttonEvent) return;

        if (!buttonEvent.IsPressed()) return;

        if (buttonEvent.ButtonIndex == MouseButton.WheelUp)
        {
            UpdateZoom(buttonEvent.Factor == 0 ? 1 : buttonEvent.Factor);
        }
        else if (buttonEvent.ButtonIndex == MouseButton.WheelDown)
        {
            UpdateZoom(buttonEvent.Factor == 0 ? -1 : -buttonEvent.Factor);
        }
    }

    private float GetNormalizedZoom()
    {
        // Inverse lerp = (value - min) / (max - min)
        return Mathf.Clamp((camera.Zoom.X - minZoom) / (maxZoom - minZoom), 0f, 1f);
    }

    private void UpdateZoom(float delta)
    {
        var normalizedZoom = GetNormalizedZoom();

        // Make zoom smoother because I guess Camera2D zooms faster when zoom is low.
        var zoomMagnitude = Mathf.Clamp(normalizedZoom + 0.25f, 0.25f, 1f);

        var deltaVector = Vector2.One * delta * 0.3f * zoomMagnitude;
        camera.Zoom += deltaVector;

        if (camera.Zoom.X < minZoom)
        {
            camera.Zoom = Vector2.One * minZoom;
        }

        if (camera.Zoom.X > maxZoom)
        {
            camera.Zoom = Vector2.One * maxZoom;
        }
    }
}

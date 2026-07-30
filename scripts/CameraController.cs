using Godot;

public partial class CameraController : Node2D
{
    [Export]
    public float PanSpeed;

    private Camera2D camera;
    private float minZoom = 0.3f;
    private float maxZoom = 5f;
    private bool middleMouseHeld;

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

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton buttonEvent)
        {
            if (buttonEvent.IsPressed())
            {
                if (buttonEvent.ButtonIndex == MouseButton.WheelUp)
                {
                    UpdateZoom(buttonEvent.Factor == 0 ? 1 : buttonEvent.Factor);
                }
                else if (buttonEvent.ButtonIndex == MouseButton.WheelDown)
                {
                    UpdateZoom(buttonEvent.Factor == 0 ? -1 : -buttonEvent.Factor);
                }
            }

            if (buttonEvent.ButtonIndex == MouseButton.Middle)
            {
                middleMouseHeld = buttonEvent.IsPressed();
            }
        }
        else if (inputEvent is InputEventMouseMotion mouseMotionEvent && middleMouseHeld)
        {
            var motionDelta = mouseMotionEvent.Relative;
            var x = GetNormalizedZoom();
            var minZoomMag = 1.25f;
            var maxZoomMag = 0.25f;
            var panMagnitude = x * (1f - minZoomMag - (1f - maxZoomMag)) + minZoomMag;
            // negate to make panning with the mouse feel intuitive
            Translate(-motionDelta * panMagnitude);
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

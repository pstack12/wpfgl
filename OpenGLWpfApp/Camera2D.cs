namespace OpenGLWpfApp;

using OpenTK.Mathematics;
using System;
using System.Windows;

public class Camera2D
{
    private readonly Func<double> _getWidth;
    private readonly Func<double> _getHeight;
    private readonly Action       _requestRender;

    public float   Zoom      { get; private set; } = 1.0f;
    public Vector2 PanOffset { get; set; }         = Vector2.Zero;
    public float   RotationX { get; set; }
    public float   RotationY { get; set; }

    public Camera2D(Func<double> getWidth, Func<double> getHeight, Action requestRender)
    {
        _getWidth      = getWidth;
        _getHeight     = getHeight;
        _requestRender = requestRender;
    }

    public void ResetView()
    {
        Zoom      = 1.0f;
        PanOffset = Vector2.Zero;
        RotationX = 0f;
        RotationY = 0f;

        _requestRender(); // Trigger immediate redraw

        // Force WPF to flush rendering immediately
        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
            System.Windows.Threading.DispatcherPriority.Render,
            new Action(() => { })
            );

        // Trigger again after rendering pass
        _requestRender();
    }

    public void ZoomAt(float diag, Point mousePos, int delta)
    {
        var controlWidth  = _getWidth();
        var controlHeight = _getHeight();
        var aspect        = (float)(controlWidth / controlHeight);

        var mouseNormX = (float)(mousePos.X / controlWidth);
        var mouseNormY = (float)(mousePos.Y / controlHeight);

        var viewHeightBefore = diag             * Zoom;
        var viewWidthBefore  = viewHeightBefore * aspect;

        // World coordinate under the mouse before zoom
        var worldXBefore = (mouseNormX - 0.5f)       * viewWidthBefore  + PanOffset.X;
        var worldYBefore = (0.5f       - mouseNormY) * viewHeightBefore + PanOffset.Y;

        // Apply zoom
        var zoomFactor = delta > 0 ? 0.9f : 1.1f;
        var newZoom    = Math.Clamp(Zoom * zoomFactor, 0.01f, 100f);

        var viewHeightAfter = diag            * newZoom;
        var viewWidthAfter  = viewHeightAfter * aspect;

        // World coordinate under the mouse after zoom (no pan change yet)
        var worldXAfter = (mouseNormX - 0.5f)       * viewWidthAfter  + PanOffset.X;
        var worldYAfter = (0.5f       - mouseNormY) * viewHeightAfter + PanOffset.Y;

        // Delta required to keep the world point under mouse fixed
        var panCorrection = new Vector2(worldXBefore - worldXAfter, worldYBefore - worldYAfter);

        PanOffset += panCorrection;
        Zoom      =  newZoom;

        _requestRender();
    }

    public void Pan(float diag, float dxPixels, float dyPixels)
    {
        var aspect     = (float)(_getWidth() / _getHeight());

        var viewHeight = diag       * Zoom;
        var viewWidth  = viewHeight * aspect;

        var dxWorld = dxPixels / (float)_getWidth()  * viewWidth;
        var dyWorld = dyPixels / (float)_getHeight() * viewHeight;

        PanOffset -= new Vector2(dxWorld, -dyWorld);
        
        _requestRender();
    }

    public Matrix4 GetViewProjectionMatrix(float diag, float zScale)
    {
        var aspect = (float)(_getWidth() / _getHeight());

        var viewHeight = diag * Zoom;
        var viewWidth  = viewHeight * aspect;
        
        var projection = Matrix4.CreateOrthographic(viewWidth, viewHeight, 0.1f, 100f);

        // Look down the -Z axis (eye at +Z, looking toward origin)
        var eye    = new Vector3(PanOffset.X, PanOffset.Y, 10f);
        var target = new Vector3(PanOffset.X, PanOffset.Y, 0f);
        var up     = new Vector3(0,           1,           0);

        var view = Matrix4.LookAt(eye, target, up);

        var model = Matrix4.CreateScale(1.0f, 1.0f, zScale) *
            Matrix4.CreateRotationX(MathHelper.DegreesToRadians(RotationX)) *
            Matrix4.CreateRotationY(MathHelper.DegreesToRadians(RotationY));

        return model * view * projection;
    }

}
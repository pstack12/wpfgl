using System.ComponentModel;
using OpenTK.Mathematics;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OpenTK.Graphics.OpenGL;
using OpenTK.Wpf;
using Color = System.Drawing.Color;
using static OpenTK.Graphics.OpenGL.GL;

namespace OpenGLWpfApp;

/// <summary>
/// Interaction logic for SurfaceControl.xaml
/// </summary>
public partial class SurfaceControl : UserControl
{
    public SurfaceControl()
    {
        InitializeComponent();
        DataContext = this;
        
        _camera = new Camera2D(
            () => OpenGlControl.ActualWidth,
            () => OpenGlControl.ActualHeight,
            RequestRender
            );

        var settings = new GLWpfControlSettings
        {
            MajorVersion       = 3,
            MinorVersion       = 0,
            RenderContinuously = false,
        };

        OpenGlControl.Start(settings);

        InitializeGl();
    }

    public void ComputeAo()
    {
        if (ActiveMeasurement?.MeshTopology == null)
            return;
        
        var vertices = ActiveMeasurement.MeshTopology.Vertices;
        if (vertices == null)
            return;
        
        var meshData = ActiveMeasurement.MeshData;
        if (meshData == null)
            return;
        
        //GL.UseProgram(_minimalShaderProgram);
        
        //var aoBaker = new AoBaker();
        //aoBaker.BakeAmbientOcclusion(vertices, mvp =>
        //{
        //    UniformMatrix4(GL.GetUniformLocation(_minimalShaderProgram, "uMVP"), false, ref mvp);
        //    meshData.Draw();
        //});
    }
    
    private void InitializeGlslShaders()
    {
        var vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, ShaderSource.SurfaceVertexShader);
        GL.CompileShader(vertexShader);

        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, ShaderSource.SurfaceFragmentShader);
        GL.CompileShader(fragmentShader);

        _shaderProgram = GL.CreateProgram();
        GL.AttachShader(_shaderProgram, vertexShader);
        GL.AttachShader(_shaderProgram, fragmentShader);
        GL.LinkProgram(_shaderProgram);

        // Cleanup
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        var minimalVertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(minimalVertexShader, ShaderSource.MinimalVertexShader);
        GL.CompileShader(minimalVertexShader);

        var minimalFragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(minimalFragmentShader, ShaderSource.MinimalFragmentShader);
        GL.CompileShader(minimalFragmentShader);

        _minimalShaderProgram = GL.CreateProgram();
        GL.AttachShader(_minimalShaderProgram, minimalVertexShader);
        GL.AttachShader(_minimalShaderProgram, minimalFragmentShader);
        GL.LinkProgram(_minimalShaderProgram);

        // Cleanup
        GL.DeleteShader(minimalVertexShader);
        GL.DeleteShader(minimalFragmentShader);

        var shadowVertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(shadowVertexShader, ShaderSource.ShadowVertexShader);
        GL.CompileShader(shadowVertexShader);

        var shadowFragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(shadowFragmentShader, ShaderSource.ShadowFragmentShader);
        GL.CompileShader(shadowFragmentShader);

        _shadowShaderProgram = GL.CreateProgram();
        GL.AttachShader(_shadowShaderProgram, shadowVertexShader);
        GL.AttachShader(_shadowShaderProgram, shadowFragmentShader);
        GL.LinkProgram(_shadowShaderProgram);

        // Cleanup
        GL.DeleteShader(shadowVertexShader);
        GL.DeleteShader(shadowFragmentShader);

    }

    private void InitializeGl()
    {
        CheckGlError("InitializeGl start");

        InitializeGlslShaders();

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);

        _shadowTexture = new GlTexture("res/shadow.png", true);

        CheckGlError("InitializeGl end");
    }

    public void RequestRender()
    {
        OpenGlControl.InvalidateVisual();
    }

    private static void CheckGlError(string label)
    {
        var error = GL.GetError();
        if (error == ErrorCode.NoError)
            return;

        Debug.Assert(false, $"[OpenGL ERROR] {label}: {error}");
    }

    private void OpenGlControl_Render(TimeSpan delta)
    {
        CheckGlError("OpenGlControl_Render start");

        // Set the background color
        GL.ClearColor(Color.LightSlateGray);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.Enable(EnableCap.DepthTest);

        if (ActiveMeasurement?.MeshData == null)
            return;

        var bb   = ActiveMeasurement.GetMeshBoundingBox;
        var dx   = bb.Item2.X - bb.Item1.X;
        var dy   = bb.Item2.Y - bb.Item1.Y;
        var diag = (float)Math.Sqrt(dx * dx + dy * dy);
        
        // Use shader program for the mesh
        GL.UseProgram(_shaderProgram);

        // Set uniform matrices for mesh
        var mvp = _camera.GetViewProjectionMatrix(diag, ZScale);

        var model = Matrix4.CreateScale(1.0f, 1.0f, ZScale);
        Matrix4.Invert(model, out var inverse);
        var normalMatrix = Matrix4.Transpose(inverse);

        GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "uModel"),        false, ref model);
        GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "uMVP"),          false, ref mvp);
        GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "uNormalMatrix"), false, ref normalMatrix);

        var lightDir = new Vector3(LightX, LightY, LightZ); // Directional light location relative to the origin
        if (lightDir.LengthSquared > 0)
            lightDir.Normalize();
        
        GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "uLightDir"), ref lightDir);

        var viewPos = new Vector3(_camera.PanOffset.X, _camera.PanOffset.Y, 10.0f); // Camera position

        GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "uViewPos"),  ref viewPos);

        GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "uAmbientStrength"),  AmbientStrength);
        GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "uDiffuseStrength"),  DiffuseStrength);
        GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "uSpecularStrength"), SpecularStrength);
        GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "uShininess"),        Shininess);

        GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "uFrontColor"), new Vector3(1.0f, 1.0f, 1.0f));
        GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "uBackColor"),  new Vector3(1.0f, 1.0f, 1.0f));

        var meshData = ActiveMeasurement.MeshData;

        // Draw mesh
        meshData.Draw();

        CheckGlError("OpenGlControl_Render end");

        RenderDropShadow();
    }

    private void RenderDropShadow()
    {
        if (_shadowTexture == null)
            return;

        if (ActiveMeasurement == null)
            return;

        CheckGlError("RenderDropShadow start");

        var bb      = ActiveMeasurement.GetMeshBoundingBox;
        var minX    = bb.Item1.X; 
        var maxX    = bb.Item2.X;
        var minY    = bb.Item1.Y;
        var maxY    = bb.Item2.Y;
        var minZ    = bb.Item1.Z;
        var maxZ    = bb.Item2.Z;
        var dx      = maxX - minX;
        var dy      = maxY - minY;
        var dz      = maxZ - minZ;
        var shadowZ = minZ - dz * 0.1f;
        var diag    = (float)Math.Sqrt(dx * dx + dy * dy);

        minX -= dx * (80 / 1000f);
        maxX += dx * (80 / 1000f);
        minY -= dy * (80 / 800f);
        maxY += dy * (80 / 800f);
        
        GL.UseProgram(_shadowShaderProgram);

        float[] quadVertices = {
            //    X     Y     Z     U    V
            minX, minY,  shadowZ,   0f,  0f, // bottom-left
            maxX, minY,  shadowZ,   1f,  0f, // bottom-right
            maxX, maxY,  shadowZ,   1f,  1f, // top-right
            minX, maxY,  shadowZ,   0f,  1f  // top-left
        };

        uint[] indices =
        [
            0, 1, 2,
            2, 3, 0
        ];

        var mvp = _camera.GetViewProjectionMatrix(diag, ZScale);
        GL.UniformMatrix4(GL.GetUniformLocation(_shadowShaderProgram, "uMVP"), false, ref mvp);

        var vao = GL.GenVertexArray();
        var vbo = GL.GenBuffer();
        var ebo = GL.GenBuffer();

        GL.BindVertexArray(vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        // position
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);                 // position
        GL.EnableVertexAttribArray(0);

        // texcoord
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float)); // texcoord
        GL.EnableVertexAttribArray(1);

        // Blending for alpha transparency
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // Set uniforms
        GL.Uniform1(GL.GetUniformLocation(_shadowShaderProgram, "uUseRedAsAlpha"), 1); // true

        // Bind texture
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _shadowTexture.TextureId);
        GL.Uniform1(GL.GetUniformLocation(_shadowShaderProgram, "uTexture"), 0);

        GL.Enable(EnableCap.CullFace);

        // Draw
        GL.BindVertexArray(vao);
        GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

        GL.Disable(EnableCap.CullFace);

        GL.BindVertexArray(0);

        CheckGlError("RenderDropShadow end");
    }

    private void OpenGlControl_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var now = DateTime.Now;
        if (e.ChangedButton == MouseButton.Left &&
            (now - _lastClickTime).TotalMilliseconds < DoubleClickThresholdMs)
        {
            _camera.ResetView();
        }

        _lastClickTime = now;

        _lastMousePos = e.GetPosition(OpenGlControl);
        _isDragging   = true;
        OpenGlControl.CaptureMouse();
    }

    private void OpenGlControl_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        OpenGlControl.ReleaseMouseCapture();
    }

    private void OpenGlControl_MouseMove(object sender, MouseEventArgs e)
    {
        if (ActiveMeasurement == null)
            return;
        
        if (!_isDragging)
            return;

        var currentPos = e.GetPosition(OpenGlControl);
        var delta      = currentPos - _lastMousePos;
        _lastMousePos = currentPos;

        var shiftHeld = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        var rightDown = e.RightButton == MouseButtonState.Pressed;
        var leftDown  = e.LeftButton  == MouseButtonState.Pressed;

        if (shiftHeld)
        {
            // Adjust Z scale when dragging with space held
            const float scaleFactor = -0.01f;
            var newScale = Math.Max(0.01f, ZScale + (float)delta.Y * scaleFactor);
            ZScale = newScale;
        }
        else if (rightDown || (leftDown && shiftHeld))
        {
            // Panning
            var bb   = ActiveMeasurement.GetMeshBoundingBox;
            var dx   = bb.Item2.X - bb.Item1.X;
            var dy   = bb.Item2.Y - bb.Item1.Y;
            var diag = (float)Math.Sqrt(dx * dx + dy * dy);
            
            _camera.Pan(diag, (float)delta.X, (float)delta.Y);
        }
        else if (leftDown)
        {
            // Rotation
            _camera.RotationY += (float)delta.X * 0.5f;
            _camera.RotationX += (float)delta.Y * 0.5f;
            RequestRender();
        }

    }

    private void OpenGlControl_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (ActiveMeasurement == null)
            return;
        
        var bb   = ActiveMeasurement.GetMeshBoundingBox;
        var dx   = bb.Item2.X - bb.Item1.X;
        var dy   = bb.Item2.Y - bb.Item1.Y;
        var diag = (float)Math.Sqrt(dx * dx + dy * dy);
        
        _camera.ZoomAt(diag, e.GetPosition(OpenGlControl), e.Delta);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private float _ambientStrength  = 0.15f;
    private float _diffuseStrength  = 0.7f;
    private float _specularStrength = 0.3f;
    private float _shininess        = 64.0f;

    public float AmbientStrength
    {
        get => _ambientStrength;
        set { _ambientStrength = value; Notify(nameof(AmbientStrength)); RequestRender(); }
    }

    public float DiffuseStrength
    {
        get => _diffuseStrength;
        set { _diffuseStrength = value; Notify(nameof(DiffuseStrength)); RequestRender(); }
    }

    public float SpecularStrength
    {
        get => _specularStrength;
        set { _specularStrength = value; Notify(nameof(SpecularStrength)); RequestRender(); }
    }

    public float Shininess
    {
        get => _shininess;
        set { _shininess = value; Notify(nameof(Shininess)); RequestRender(); }
    }

    private float _zScale = 1.0f;
    public float ZScale
    {
        get => _zScale;
        set
        {
            if (_zScale != value)
            {
                _zScale = value;
                Notify(nameof(ZScale));
                RequestRender();
            }
        }
    }

    private float _lightX = 1.0f;
    public float LightX
    {
        get => _lightX;
        set { _lightX = value; Notify(nameof(LightX)); RequestRender(); }
    }

    private float _lightY = 1.0f;
    public float LightY
    {
        get => _lightY;
        set { _lightY = value; Notify(nameof(LightY)); RequestRender(); }
    }

    private float _lightZ = 1.0f;
    public float LightZ
    {
        get => _lightZ;
        set { _lightZ = value; Notify(nameof(LightZ)); RequestRender(); }
    }

    public Measurement? ActiveMeasurement { get; set; }

    private void Notify(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private int _shaderProgram;
    private int _minimalShaderProgram;
    private int _shadowShaderProgram;

    private readonly Camera2D _camera;

    private GlTexture? _shadowTexture;

    private       Point    _lastMousePos;
    private       bool     _isDragging            = false;
    private       DateTime _lastClickTime         = DateTime.MinValue;
    private const int      DoubleClickThresholdMs = 300;  // Adjust if needed

}
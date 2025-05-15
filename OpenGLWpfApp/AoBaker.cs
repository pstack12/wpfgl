using OpenGLWpfApp;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

public class AoBaker : IDisposable
{
    private int fbo;
    private int depthTex;
    private int width;
    private int height;
    private List<float[]> depthBuffers = new();

    public AoBaker(int width = 512, int height = 512)
    {
        this.width = width;
        this.height = height;
        SetupFramebuffer();
    }

    private void SetupFramebuffer()
    {
        fbo = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

        depthTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, depthTex);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32, width, height, 0,
                      PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthTex, 0);
        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);

        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
            throw new Exception($"Framebuffer incomplete: {status}");

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void RenderDepthFromView(Matrix4 viewProj, Action<Matrix4> renderScene)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
        GL.Viewport(0, 0, width, height);
        GL.Clear(ClearBufferMask.DepthBufferBit);
        GL.Enable(EnableCap.DepthTest);

        renderScene(viewProj);

        float[] depthData = new float[width * height];
        GL.ReadPixels(0, 0, width, height, PixelFormat.DepthComponent, PixelType.Float, depthData);
        depthBuffers.Add(depthData);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public float SampleOcclusionAt(Vector3 worldPos, List<Matrix4> viewProjMatrices)
    {
        int visibleCount = 0;
        for (int i = 0; i < viewProjMatrices.Count; i++)
        {
            Matrix4 vp = viewProjMatrices[i];
            Vector4 clip = vp * new Vector4(worldPos, 1.0f);
            if (clip.W <= 0) continue;
            Vector3 ndc = new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
            Vector2 screen = new Vector2((ndc.X * 0.5f + 0.5f) * width, (0.5f - ndc.Y * 0.5f) * height);

            int x = (int)MathF.Floor(screen.X);
            int y = (int)MathF.Floor(screen.Y);
            if (x < 0 || x >= width || y < 0 || y >= height) continue;

            float viewDepth = ndc.Z * 0.5f + 0.5f;
            float[] buffer = depthBuffers[i];
            float sampled = buffer[x + y * width];

            if (viewDepth <= sampled + 0.001f)
                visibleCount++;
        }

        return visibleCount / (float)viewProjMatrices.Count;
    }

    public static List<Matrix4> GenerateUpperHemisphereViewMatrices(Vector3 center, float radius)
    {
        var directions = new List<Vector3>
        {
            new Vector3( 1,  0,  1).Normalized(),
            new Vector3(-1,  0,  1).Normalized(),
            new Vector3( 0,  1,  1).Normalized(),
            new Vector3( 0, -1,  1).Normalized(),
            new Vector3( 1,  1,  1).Normalized(),
            new Vector3(-1, -1,  1).Normalized(),
            new Vector3(-1,  1,  1).Normalized(),
            new Vector3( 1, -1,  1).Normalized(),
            new Vector3( 0,  0,  1)
        };

        var viewProj = new List<Matrix4>();
        foreach (var dir in directions)
        {
            var eye = center + dir * radius;
            var view = Matrix4.LookAt(eye, center, Vector3.UnitZ);
            float viewSize = radius * 2f;
            var proj = Matrix4.CreateOrthographic(viewSize, viewSize, radius * 0.1f, radius * 2f);
            viewProj.Add(proj * view);
        }
        return viewProj;
    }

    public void BakeAmbientOcclusion(Vertex[] vertices, Action<Matrix4> renderScene)
    {
        Vector3 center = ComputeMeshCenter(vertices);
        float radius = ComputeMeshRadius(vertices, center);
        List<Matrix4> views = GenerateUpperHemisphereViewMatrices(center, radius);

        depthBuffers.Clear();
        foreach (var vp in views)
        {
            RenderDepthFromView(vp, renderScene);
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            float ao = SampleOcclusionAt(vertices[i].Position, views);
            vertices[i].AmbientOcclusion = ao;
        }
    }

    private Vector3 ComputeMeshCenter(Vertex[] verts)
    {
        Vector3 min = new(float.MaxValue), max = new(float.MinValue);
        foreach (var v in verts)
        {
            min = Vector3.ComponentMin(min, v.Position);
            max = Vector3.ComponentMax(max, v.Position);
        }
        return (min + max) * 0.5f;
    }

    private float ComputeMeshRadius(Vertex[] verts, Vector3 center)
    {
        float r2 = 0f;
        foreach (var v in verts)
            r2 = Math.Max(r2, (v.Position - center).LengthSquared);
        return (float)Math.Sqrt(r2);
    }

    public void Dispose()
    {
        GL.DeleteTexture(depthTex);
        GL.DeleteFramebuffer(fbo);
    }
}

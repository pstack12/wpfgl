using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace OpenGLWpfApp;

public struct Vertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public float   AmbientOcclusion; // AO factor (0 = dark, 1 = full light)

    public Vertex(Vector3 pos, Vector3 normal, float ao = 1.0f)
    {
        Position         = pos;
        Normal           = normal;
        AmbientOcclusion = ao;
    }

    public static readonly int SizeInBytes = sizeof(float) * (3 + 3 + 1);
}

public class GlMesh : IDisposable
{
    public int Vao          { get; private set; }
    public int VertexBuffer { get; private set; }
    public int IndexBuffer  { get; private set; }
    public int IndexCount   { get; private set; }
    
    public void UploadMesh(MeshTopology mesh)
    {
        if (mesh.Vertices == null || mesh.Indices == null)
            throw new ArgumentNullException(nameof(mesh), "Vertices and Indices cannot be null.");

        Vao          = GL.GenVertexArray();
        VertexBuffer = GL.GenBuffer();
        IndexBuffer  = GL.GenBuffer();
        IndexCount   = mesh.Indices.Length;

        GL.BindVertexArray(Vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, mesh.Vertices.Length * Vertex.SizeInBytes, mesh.Vertices, BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndexBuffer);
        GL.BufferData(BufferTarget.ElementArrayBuffer, mesh.Indices.Length * sizeof(uint), mesh.Indices, BufferUsageHint.StaticDraw);

        var offset = 0;

        // Position (location = 0)
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, offset);
        GL.EnableVertexAttribArray(0);
        offset += sizeof(float) * 3;

        // Normal (location = 1)
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, offset);
        GL.EnableVertexAttribArray(1);
        offset += sizeof(float) * 3;

        // AO (location = 2)
        GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, offset);
        GL.EnableVertexAttribArray(2);

        GL.BindVertexArray(0);
    }

    public void Draw()
    {
        GL.BindVertexArray(Vao);
        GL.DrawElements(PrimitiveType.Triangles, IndexCount, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(VertexBuffer);
        GL.DeleteBuffer(IndexBuffer);
        GL.DeleteVertexArray(Vao);
    }

}

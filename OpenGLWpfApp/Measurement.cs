using OpenTK.Mathematics;

namespace OpenGLWpfApp;

public class Measurement
{
    public bool Initialize(string filename)
    {
        Filename  = filename;

        if (!CsvImporter.Parse(filename, out var heightmap, out var xInterval, out var yInterval, out _, out _))
            return false;

        MeshTopology = HeightmapGenerator.GenerateMesh(heightmap, xInterval, yInterval);
        
        MeshData = new GlMesh();
        MeshData.UploadMesh(MeshTopology);

        Heightmap = heightmap;
        XInterval = xInterval;
        YInterval = yInterval;

        _meshBoundingBox = ComputeMeshBoundingBox(MeshTopology);

        return true;
    }

    private static (Vector3, Vector3) ComputeMeshBoundingBox(MeshTopology mesh)
    {
        if (mesh.Vertices == null)
            throw new ArgumentNullException(nameof(mesh.Vertices), "Vertices cannot be null.");
        
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);

        foreach (var v in mesh.Vertices)
        {
            min = Vector3.ComponentMin(min, v.Position);
            max = Vector3.ComponentMax(max, v.Position);
        }

        return (min, max);
    }
    
    public  string?          Filename  { get; set; }
    public  float[,]?        Heightmap { get; set; }
    public  float            XInterval { get; private set; }
    public  float            YInterval { get; private set; }

    private (Vector3, Vector3) _meshBoundingBox;
    public  (Vector3, Vector3) GetMeshBoundingBox => (_meshBoundingBox.Item1, _meshBoundingBox.Item2);

    public GlMesh? MeshData { get; private set; }
    public MeshTopology? MeshTopology { get; private set; }
}
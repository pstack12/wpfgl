using System.IO;
using OpenTK.Mathematics;

namespace OpenGLWpfApp;

public class HeightmapGenerator
{
    public static MeshTopology GenerateMesh(float[,] heightmap, float xInterval, float yInterval)
    {
        const float zScale = 1.0f; // Scale factor for height
        
        var         rows     = heightmap.GetLength(0);
        var         cols     = heightmap.GetLength(1);
        var         vertices = new Vertex[rows * cols];
        var         indices  = new List<uint>();

        var zMin = float.MaxValue;
        var zMax = float.MinValue;
        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < cols; x++)
            {
                var h = heightmap[y, x];
                if (h < zMin) zMin = h;
                if (h > zMax) zMax = h;
            }
        }

        var zCenterOffset = (zMin + zMax) * 0.5f * zScale;
        var xOffset       = (cols - 1)    * 0.5f * xInterval;
        var yOffset       = (rows - 1)    * 0.5f * yInterval;

        Parallel.For(0, rows, y =>
        {
            for (var x = 0; x < cols; x++)
            {
                var fx = x               * xInterval - xOffset;
                var fy = y               * yInterval - yOffset;
                var fz = heightmap[y, x] * zScale    - zCenterOffset;
                vertices[y * cols + x] = new Vertex(new Vector3(fx, fy, fz), Vector3.Zero);
            }
        });

        Parallel.For(1, rows - 1, y =>
        {
            for (var x = 1; x < cols - 1; x++)
            {
                var hl = heightmap[y, x - 1];
                var hr = heightmap[y, x + 1];
                var hd = heightmap[y    + 1, x];
                var hu = heightmap[y    - 1, x];

                var dx     = new Vector3(2 * xInterval, 0, (hr - hl) * zScale);
                var dy     = new Vector3(0,             2            * yInterval, (hd - hu) * zScale);
                var normal = Vector3.Cross(dx, dy).Normalized();
                vertices[y * cols + x].Normal = normal;
            }
        });

        for (var x = 0; x < cols; x++)
        {
            vertices[0 * cols + x].Normal = vertices[1 * cols + x].Normal;
            vertices[(rows - 1) * cols + x].Normal = vertices[(rows - 2) * cols + x].Normal;
        }

        for (var y = 0; y < rows; y++)
        {
            vertices[y * cols + 0].Normal = vertices[y * cols + 1].Normal;
            vertices[y * cols + (cols - 1)].Normal = vertices[y * cols + (cols - 2)].Normal;
        }

        //// Hemisphere AO sampling (approximate, ~16 directions)
        //const int sampleCount = 16;
        //const float maxDistance = 0.05f; // sampling radius in world units
        
        //for (var y = 1; y < rows - 1; y++)
        //{
        //    for (var x = 1; x < cols - 1; x++)
        //    {
        //        var hits = 0;

        //        for (var i = 0; i < sampleCount; i++)
        //        {
        //            // Random angle on hemisphere
        //            var angle = 2 * Math.PI * i / sampleCount;
        //            var dx    = (float)Math.Cos(angle);
        //            var dy    = (float)Math.Sin(angle);

        //            // Sample point in local tangent plane
        //            const float stepSize = maxDistance / 10f;
        //            var         occluded = false;

        //            for (var s = stepSize; s <= maxDistance; s += stepSize)
        //            {
        //                var sx = x + dx * (s / xInterval);
        //                var sy = y + dy * (s / yInterval);
        //                var ix = (int)Math.Round(sx);
        //                var iy = (int)Math.Round(sy);

        //                if (ix < 0 || ix >= cols || iy < 0 || iy >= rows)
        //                    continue;

        //                var sampleHeight = heightmap[iy, ix] * zScale;
        //                var originHeight = heightmap[y, x]   * zScale;

        //                var dz        = sampleHeight - originHeight;
        //                var expectedZ = s * 0.2f; // threshold slope factor

        //                if (dz > expectedZ)
        //                {
        //                    occluded = true;
        //                    break;
        //                }
        //            }

        //            if (occluded) hits++;
        //        }

        //        var ao = 1.0f - hits / (float)sampleCount;
        //        vertices[y * cols + x].AmbientOcclusion = ao;
        //    }
        //}

        //for (var x = 0; x < cols; x++)
        //{
        //    vertices[0 * cols + x].AmbientOcclusion = vertices[1 * cols + x].AmbientOcclusion;
        //    vertices[(rows - 1) * cols + x].AmbientOcclusion = vertices[(rows - 2) * cols + x].AmbientOcclusion;
        //}
        //for (var y = 0; y < rows; y++)
        //{
        //    vertices[y * cols + 0].AmbientOcclusion = vertices[y * cols + 1].AmbientOcclusion;
        //    vertices[y * cols + (cols - 1)].AmbientOcclusion = vertices[y * cols + (cols - 2)].AmbientOcclusion;
        //}
        
        //// Approximate AO using curvature
        //for (var y = 1; y < rows - 1; y++)
        //{
        //    for (var x = 1; x < cols - 1; x++)
        //    {
        //        var h   = heightmap[y, x];
        //        var sum = 0f;
        //        sum += Math.Abs(heightmap[y, x - 1] - h);
        //        sum += Math.Abs(heightmap[y, x + 1] - h);
        //        sum += Math.Abs(heightmap[y - 1, x] - h);
        //        sum += Math.Abs(heightmap[y + 1, x] - h);
        //        var ao = 1.0f - Math.Clamp(sum * 5.0f, 0f, 1f); // scale & invert
        //        vertices[y * cols + x].AmbientOcclusion = ao;
        //    }       
        //}

        //// AO border copy
        //for (var x = 0; x < cols; x++)
        //{
        //    vertices[0          * cols + x].AmbientOcclusion = vertices[1          * cols + x].AmbientOcclusion;
        //    vertices[(rows - 1) * cols + x].AmbientOcclusion = vertices[(rows - 2) * cols + x].AmbientOcclusion;
        //}
        //for (var y = 0; y < rows; y++)
        //{
        //    vertices[y * cols + 0].AmbientOcclusion          = vertices[y * cols + 1].AmbientOcclusion;
        //    vertices[y * cols + (cols - 1)].AmbientOcclusion = vertices[y * cols + (cols - 2)].AmbientOcclusion;
        //}

        for (var y = 0; y < rows - 1; y++)
        {
            for (var x = 0; x < cols - 1; x++)
            {
                var topLeft     = (uint)(y * cols + x);
                var topRight    = topLeft + 1;
                var bottomLeft  = (uint)((y + 1) * cols + x);
                var bottomRight = bottomLeft + 1;

                indices.Add(topLeft);
                indices.Add(topRight);
                indices.Add(bottomLeft);

                indices.Add(topRight);
                indices.Add(bottomRight);
                indices.Add(bottomLeft);
            }
        }

        return new MeshTopology
        {
            Vertices = vertices,
            Indices = indices.ToArray(),
        };
    }

}
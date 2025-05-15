using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGLWpfApp;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

public static class CsvImporter
{
    public static bool Parse(
        string       filename,
        out float[,] heightmap,
        out float    xSpacing,
        out float    ySpacing,
        out float    xOrigin,
        out float    yOrigin)
    {
        var points = new List<(float x, float y, float z)>();
        var xSet   = new HashSet<float>();
        var ySet   = new HashSet<float>();

        using var reader = new StreamReader(filename);
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split('\t');
            if (parts.Length < 3) continue;

            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
                !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
                !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
            {
                continue;
            }
            
            x /= 1000;
            y /= 1000;
            z /= 1000;

            points.Add((x, y, z));
            xSet.Add(x);
            ySet.Add(y);
        }

        var xVals = xSet.ToArray();
        var yVals = ySet.ToArray();
        Array.Sort(xVals);
        Array.Sort(yVals);

        var cols = xVals.Length;
        var rows = yVals.Length;

        xSpacing = cols > 1 ? xVals[1] - xVals[0] : 0;
        ySpacing = rows > 1 ? yVals[1] - yVals[0] : 0;
        xOrigin  = xVals[0];
        yOrigin  = yVals[0];

        var xIndex                                      = new Dictionary<float, int>(cols);
        var yIndex                                      = new Dictionary<float, int>(rows);
        for (var i = 0; i < cols; i++) xIndex[xVals[i]] = i;
        for (var i = 0; i < rows; i++) yIndex[yVals[i]] = i;

        heightmap = new float[rows, cols];

        foreach (var (x, y, z) in points)
        {
            var xi = xIndex[x];
            var yi = yIndex[y];
            heightmap[yi, xi] = z;
        }

        return true;
    }
}
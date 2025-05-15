using System.Drawing;
using OpenTK.Graphics.OpenGL4;

namespace OpenGLWpfApp;

public class GlTexture : IDisposable
{
    public int TextureId { get; private set; } = -1;
    public int Width     { get; private set; }
    public int Height    { get; private set; }

    public GlTexture(string path, bool alphaOnly)
    {
        LoadFromFile(path, alphaOnly);
    }

    private void LoadFromFile(string path, bool alphaOnly)
    {
        using var bmp = new Bitmap(path);
        Width = bmp.Width;
        Height = bmp.Height;

        byte[] textureData;
        PixelInternalFormat internalFormat;
        PixelFormat format;

        if (alphaOnly)
        {
            textureData = new byte[Width * Height];
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var color = bmp.GetPixel(x, y);
                    textureData[y * Width + x] = (byte)(255 - color.R); // Alpha from red
                }
            }

            internalFormat = PixelInternalFormat.R8;
            format = PixelFormat.Red;
        }
        else
        {
            textureData = new byte[Width * Height * 4];
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var color = bmp.GetPixel(x, y);
                    var index = (y * Width + x) * 4;
                    textureData[index + 0] = color.R;
                    textureData[index + 1] = 0;
                    textureData[index + 2] = 0;
                    textureData[index + 3] = color.R;
                }
            }

            internalFormat = PixelInternalFormat.Rgba;
            format = PixelFormat.Rgba;
        }

        TextureId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, TextureId);

        GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat,
                      Width, Height, 0,
                      format, PixelType.UnsignedByte, textureData);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Bind(TextureUnit unit = TextureUnit.Texture0)
    {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, TextureId);
    }

    public void Unbind()
    {
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Dispose()
    {
        if (TextureId != -1)
        {
            GL.DeleteTexture(TextureId);
            TextureId = -1;
        }
    }
}
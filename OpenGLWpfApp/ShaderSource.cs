namespace OpenGLWpfApp;

public static class ShaderSource
{
    public const string SurfaceVertexShader = @"
        #version 330 core

        layout(location = 0) in vec3 aPosition;
        layout(location = 1) in vec3 aNormal;
        layout(location = 2) in float aAO;

        uniform mat4 uModel;
        uniform mat4 uMVP;
        uniform mat4 uNormalMatrix;

        out vec3 FragPos;
        out vec3 Normal;
        out float AO;

        void main()
        {
            vec4 worldPos = uModel * vec4(aPosition, 1.0);
            FragPos = worldPos.xyz;
            Normal = mat3(uNormalMatrix) * aNormal;
            AO = aAO;

            gl_Position = uMVP * vec4(aPosition, 1.0);
        }
        ";

    public const string SurfaceFragmentShader = @"
        #version 330 core

        in vec3 FragPos;
        in vec3 Normal;
        in float AO;

        uniform vec3 uLightDir;         // Light direction in world space
        uniform vec3 uViewPos;          // Camera position

        uniform float uAmbientStrength;
        uniform float uDiffuseStrength;
        uniform float uSpecularStrength;
        uniform float uShininess;

        uniform vec3 uFrontColor;       // Material color for front face
        uniform vec3 uBackColor;        // Material color for back face

        out vec4 FragColor;

        void main()
        {
            int faceDirection = gl_FrontFacing ? 1 : -1;
            vec3 norm = normalize(Normal) * faceDirection;

            vec3 lightDir = normalize(uLightDir);
            vec3 viewDir = normalize(uViewPos - FragPos);
            vec3 halfDir = normalize(lightDir + viewDir);

            float diff = abs(dot(norm, lightDir));
            float spec = pow(max(dot(norm, halfDir), 0.0), uShininess);

            vec3 ambient  = uAmbientStrength  * vec3(1.0);
            vec3 diffuse  = uDiffuseStrength  * diff * vec3(1.0);
            vec3 specular = uSpecularStrength * spec * vec3(1.0);

            vec3 baseColor = (faceDirection > 0) ? uFrontColor : uBackColor;
            vec3 lighting = (ambient + diffuse + specular) * baseColor * AO;

            FragColor = vec4(lighting, 1.0);
        }
        ";

    public const string MinimalVertexShader = @"
        #version 330 core

        layout(location = 0) in vec3 aPosition;

        uniform mat4 uMVP;

        void main()
        {
            gl_Position = uMVP * vec4(aPosition, 1.0);
        }
        ";

    public const string MinimalFragmentShader = @"
        #version 330 core
        void main() {}
        ";

    public const string ShadowVertexShader = @"
        #version 330 core

        layout(location = 0) in vec3 aPos;       // X, Y, Z
        layout(location = 1) in vec2 aTexCoord;  // U, V

        uniform mat4 uMVP; // model-view-projection matrix

        out vec2 TexCoord;

        void main()
        {
            gl_Position = uMVP * vec4(aPos, 1.0);
            TexCoord = aTexCoord;
        }
        ";

    public const string ShadowFragmentShader = @"
        #version 330 core

        in vec2 TexCoord;
        uniform sampler2D uTexture;
        uniform bool uUseRedAsAlpha;

        out vec4 FragColor;

        void main()
        {
            vec4 tex = texture(uTexture, TexCoord);
            float alpha = uUseRedAsAlpha ? tex.r : tex.a;
            FragColor = vec4(0.0, 0.0, 0.0, alpha);
        }";

}
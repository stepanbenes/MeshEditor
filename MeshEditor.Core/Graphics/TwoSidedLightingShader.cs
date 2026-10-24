using OpenTK.Graphics.OpenGL;

namespace MeshEditor.Graphics
{
	/// <summary>
	/// Two-sided lighting of faces in a vertex shader, replacing the fixed-function GL_LIGHT_MODEL_TWO_SIDE, which is very slow
	/// on some drivers: on NVIDIA its cost grows with pixels and MSAA samples (14 ms per frame for a mesh covering a 4K view
	/// with 4x MSAA, 2 ms with this shader). Choosing between a front and a back color (GL_VERTEX_PROGRAM_TWO_SIDE) is just as slow.
	/// </summary>
	/// <remarks>
	/// Mirrors the fixed-function setup of SceneFacade.InitializeGL: Light0 without attenuation and spot, color material for
	/// ambient and diffuse, no specular. Light0 keeps its default position, a headlight shining along the view direction, so a
	/// visible face is lit by its normal facing the viewer: |N.L| equals the two-sided result without choosing a side.
	/// The fragment stage stays fixed-function, so the shade model (flat/smooth) still applies.
	/// </remarks>
	public class TwoSidedLightingShader : ShaderHolder
	{
		private const string vertexShaderString =
@"
void main()
{
	vec3 normal = normalize(gl_NormalMatrix * gl_Normal);
	vec4 position = gl_ModelViewMatrix * gl_Vertex;
	vec4 light = gl_LightSource[0].position;
	vec3 lightVector = normalize(light.w == 0.0 ? light.xyz : light.xyz - position.xyz);
	vec4 ambient = (gl_LightModel.ambient + gl_LightSource[0].ambient) * gl_Color;
	vec4 diffuse = gl_LightSource[0].diffuse * gl_Color;
	gl_FrontColor = vec4((ambient + diffuse * abs(dot(normal, lightVector))).rgb, gl_Color.a);
	gl_Position = ftransform(); // the same depth as the fixed-function edges drawn over the faces
}
";

		public TwoSidedLightingShader()
		{
			IsReady = LoadShaderStrings(new[] { vertexShaderString }, null) && SetActiveShaders(0, -1);
		}

		public bool IsReady { get; }

		public void Use()
		{
			GL.UseProgram(Program);
		}

		public void Unuse()
		{
			GL.UseProgram(0);
		}
	}
}

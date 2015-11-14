using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace MeshEditor.Graphics
{
	public class CrossHatchShader : ShaderHolder
	{

		#region Non-lighting shaders

		private readonly string vertexShaderString =
@"
varying vec4 color;

void main()
{
	color = gl_Color;
	gl_Position = ftransform();
}
";

		private readonly string fragmentShaderString =
@"
uniform vec4 propertyColors[254];

varying vec4 color;

void main()
{
	int alphaIndex = int(color.a * 256.0);
	if (alphaIndex < 255)
	{
		if (alphaIndex > 0)
		{
			int greenIndex = int(color.g * 256.0);
			int redIndex = int(color.r * 256.0);
			float bandwidth = 20.0;
			if (greenIndex > 0)
			{
				bandwidth += 10.0;
				if (redIndex > 0)
				{
					bandwidth += 10.0;
				}
			}

			float modulo40 = mod(gl_FragCoord.x + gl_FragCoord.y, bandwidth);
			if (modulo40 < 10.0)
			{
				gl_FragColor = propertyColors[alphaIndex - 1];
			}
			else if (modulo40 < 20.0)
			{
				int blueIndex = int(color.b * 256.0);
				gl_FragColor = propertyColors[blueIndex - 1];
			}
			else if (modulo40 < 30.0)
			{
				gl_FragColor = propertyColors[greenIndex - 1];
			}
			else
			{
				gl_FragColor = propertyColors[redIndex - 1];
			}
		}
		else
		{
			float modulo20 = mod(gl_FragCoord.x + gl_FragCoord.y, 20.0);
			if (modulo20 < 10.0)
			{
				gl_FragColor = color;
			}
			else
			{
				gl_FragColor = vec4(0, 0, 0, 0); // black
			}
		}
	}
	else
	{
		gl_FragColor = color;
	}
}
";

		#endregion

		#region Lighting shaders

		private readonly string vertexShaderLightingString =
@"
varying vec4 color;
varying float NdotL;

void main()
{
	vec3 normal = normalize(gl_NormalMatrix * gl_Normal);
	vec4 position = gl_ModelViewMatrix * gl_Vertex;
	vec3 lightVector = normalize(gl_LightSource[0].position.xyz - position.xyz);
	NdotL = abs(dot(normal, lightVector.xyz));
	color = gl_Color;
	gl_Position = ftransform();
}
";

		private readonly string fragmentShaderLightingString =
@"
uniform vec4 propertyColors[254];

varying vec4 color;
varying float NdotL;

void main()
{
	vec4 fragmentColorWithoutLight;

	int alphaIndex = int(color.a * 256.0);
	if (alphaIndex < 255)
	{
		if (alphaIndex > 0)
		{
			int greenIndex = int(color.g * 256.0);
			int redIndex = int(color.r * 256.0);
			float bandwidth = 20.0;
			if (greenIndex > 0)
			{
				bandwidth += 10.0;
				if (redIndex > 0)
				{
					bandwidth += 10.0;
				}
			}

			float modulo40 = mod(gl_FragCoord.x + gl_FragCoord.y, bandwidth);
			if (modulo40 < 10.0)
			{
				fragmentColorWithoutLight = propertyColors[alphaIndex - 1];
			}
			else if (modulo40 < 20.0)
			{
				int blueIndex = int(color.b * 256.0);
				fragmentColorWithoutLight = propertyColors[blueIndex - 1];
			}
			else if (modulo40 < 30.0)
			{
				fragmentColorWithoutLight = propertyColors[greenIndex - 1];
			}
			else
			{
				fragmentColorWithoutLight = propertyColors[redIndex - 1];
			}
		}
		else
		{
			float modulo20 = mod(gl_FragCoord.x + gl_FragCoord.y, 20.0);
			if (modulo20 < 10.0)
			{
				fragmentColorWithoutLight = color;
			}
			else
			{
				fragmentColorWithoutLight = vec4(0, 0, 0, 0); // black
			}
		}
	}
	else
	{
		fragmentColorWithoutLight = color;
	}

	vec4 diffuse = fragmentColorWithoutLight * gl_LightSource[0].diffuse;
	vec4 ambient = fragmentColorWithoutLight * gl_LightSource[0].ambient + fragmentColorWithoutLight * gl_LightModel.ambient;

	gl_FragColor = ambient + NdotL * diffuse;
}
";

		#endregion

//		#region Per-vertex lighting shaders

//		private readonly string perVertexLightingVertexShaderString =
//@"

//varying vec4 color;

//void main()
//{
//	vec3 normal = normalize(gl_NormalMatrix * gl_Normal);
//	vec4 position = gl_ModelViewMatrix * gl_Vertex;
//	vec3 lightVector = normalize(gl_LightSource[0].position.xyz - position.xyz);
//	float df = abs(dot(normal, lightVector.xyz));
	
//	vec4 diffuse = gl_Color * gl_LightSource[0].diffuse;
//	vec4 ambient = gl_Color * gl_LightSource[0].ambient + gl_Color * gl_LightModel.ambient;

//	color = ambient + df * diffuse;
//	gl_Position = ftransform();
//}
//";

//		private readonly string perVertexLightingFragmentShaderString =
//@"

//uniform vec4 propertyColors[255];
//varying vec4 color;

//void main()
//{
//	gl_FragColor = color;
//}
//";

//		#endregion

		#region Fields, constructor

		int propertyColorsArrayLocation;
		bool lightingEnabled;

		public CrossHatchShader(bool lighting)
		{
			IsReady = LoadShaderStrings(new[] { vertexShaderLightingString, vertexShaderString }, new[] { fragmentShaderLightingString, fragmentShaderString });
			if (IsReady)
			{
				this.lightingEnabled = lighting;
				InitShaders();
			}
		}

		#endregion

		#region Properties

		public bool IsReady { get; private set; }

		public bool LightingEnabled
		{
			get { return lightingEnabled; }
			set
			{
				if (lightingEnabled != value)
				{
					lightingEnabled = value;
					InitShaders();
				}
			}
		}

		#endregion

		#region Public methods

		public void Use(/*TODO: IReadOnlyList<int>*/ IList<int> colorPalette)
		{
			Debug.Assert(colorPalette != null && colorPalette.Count > 0);

			if (!IsReady)
				return;

			GL.UseProgram(Program);

			int propertyColorsArrayLength = Math.Min(colorPalette.Count, 254);
			float[] colorComponents = new float[propertyColorsArrayLength * 4];

			for (int i = 0; i < propertyColorsArrayLength; i++)
			{
				float red, green, blue;
				Utilities.Functions.GetColorComponents(colorPalette[i], out red, out green, out blue);
				colorComponents[i * 4] = red;
				colorComponents[i * 4 + 1] = green;
				colorComponents[i * 4 + 2] = blue;
				colorComponents[i * 4 + 3] = 1.0f; // alpha
			}

			GL.Uniform4(propertyColorsArrayLocation, propertyColorsArrayLength, colorComponents);
		}

		public void Unuse()
		{
			GL.UseProgram(0);
		}

		#endregion

		#region Private methods

		private void InitShaders()
		{
			int index = lightingEnabled ? 0 : 1;
			SetActiveShaders(vertexShaderIndex: index, fragmentShaderIndex: index);
			propertyColorsArrayLocation = GL.GetUniformLocation(Program, "propertyColors");
		}

		#endregion

	}
}

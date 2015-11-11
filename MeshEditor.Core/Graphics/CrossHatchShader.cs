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

uniform vec4 propertyColors[255];
varying vec4 color;

void main()
{
	int alphaIndex = int(color.a * 256.0);
	if (alphaIndex < 255)
	{
		if (alphaIndex > 0)
		{
			int blueIndex = int(color.b * 256.0);
			int greenIndex = int(color.g * 256.0);
			int redIndex = int(color.r * 256.0);
			float bandwidth = 10.0;
			if (blueIndex > 0)
			{
				bandwidth += 10.0;
				if (greenIndex > 0)
				{
					bandwidth += 10.0;
					if (redIndex > 0)
					{
						bandwidth += 10.0;
					}
				}
			}
			float modulo40 = mod(gl_FragCoord.x + gl_FragCoord.y, bandwidth);
			if (modulo40 < 10.0)
			{
				gl_FragColor = propertyColors[alphaIndex];
			}
			else if (modulo40 < 20.0)
			{
				gl_FragColor = propertyColors[blueIndex];
			}
			else if (modulo40 < 30.0)
			{
				gl_FragColor = propertyColors[greenIndex];
			}
			else
			{
				gl_FragColor = propertyColors[redIndex];
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

void main()
{
	color = gl_Color;
	gl_Position = ftransform();
}
";

		private readonly string fragmentShaderLightingString =
@"

uniform vec4 propertyColors[255];
varying vec4 color;

void main()
{

}
";

		#endregion

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

		public void Use(int[] colorPalette)
		{
			Debug.Assert(colorPalette != null && colorPalette.Length > 0);

			if (!IsReady)
				return;

			GL.UseProgram(Program);

			int propertyColorsArrayLength = Math.Min(colorPalette.Length, 255);
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

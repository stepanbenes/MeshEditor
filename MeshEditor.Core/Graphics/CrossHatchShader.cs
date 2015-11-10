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

uniform vec3 propertyColors[255];
varying vec4 color;

void main()
{
	int alpha = int(color.a * 256.0);
	if (alpha < 255)
	{
		int xCoordinate = int(gl_FragCoord.x);
		int yCoordinate = int(gl_FragCoord.y);
		if (mod(xCoordinate + yCoordinate, 20) >= 10)
		{
			//gl_FragColor = vec4(1, 1, 1, 1); // white
			gl_FragColor.rgb = propertyColors[alpha];
			gl_FragColor.a = 1.0;
			return;
		}
	}
	gl_FragColor.rgb = color.rgb;
	gl_FragColor.a = 1.0;
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

uniform vec3 propertyColors[255];
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

		public void Use(int[] propertyColorsOfTwinElements)
		{
			if (!IsReady)
				return;

			Debug.Assert(propertyColorsOfTwinElements != null && propertyColorsOfTwinElements.Length <= 255);

			GL.UseProgram(Program);

			float[] colorComponents = new float[propertyColorsOfTwinElements.Length * 3];

			for (int i = 0; i < propertyColorsOfTwinElements.Length; i++)
			{
				float r, g, b;
				Utilities.Functions.GetColorComponents(propertyColorsOfTwinElements[i], out r, out g, out b);
				colorComponents[i * 3] = r;
				colorComponents[i * 3 + 1] = g;
				colorComponents[i * 3 + 2] = b;
			}

			GL.Uniform3(propertyColorsArrayLocation, propertyColorsOfTwinElements.Length, colorComponents);
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

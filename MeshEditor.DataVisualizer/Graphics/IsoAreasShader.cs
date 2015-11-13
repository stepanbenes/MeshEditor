using MeshEditor.DataVisualizer.Data;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MeshEditor.Graphics;

namespace MeshEditor.DataVisualizer.Graphics
{
	public class IsoAreasShader : ShaderHolder
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
uniform int subIntervalNumber;
uniform int controlPointCount;
uniform vec3 controlPoints[5];

varying vec4 color;

void main()
{
	// -------------------------------------------------------

	if (color.a < 0.999) // special color - undefined value, out-of-range value, selection
	{
		gl_FragColor = color;
		return;
	}

	vec3 first, second;
	vec3 u, v;
	float minDistance = 1e20; // initial big number
	float crossLength, uLength, distance;

	float intervalNumber = 0.0;

	// compute distance of color from interval
	
	// 0 - 1
	u = controlPoints[1] - controlPoints[0];
	v = color.rgb - controlPoints[0];
	crossLength = length(cross(u, v));
	uLength = length(u);
	distance = crossLength / uLength;
	if (distance < minDistance)
	{
		minDistance = distance;
		first = controlPoints[0];
		second = controlPoints[1];
		intervalNumber = 0.0;
	}

	// 1 - 2
	if (controlPointCount > 2)
	{
		u = controlPoints[2] - controlPoints[1];
		v = color.rgb - controlPoints[1];
		crossLength = length(cross(u, v));
		uLength = length(u);
		distance = crossLength / uLength;
		if (distance < minDistance)
		{
			minDistance = distance;
			first = controlPoints[1];
			second = controlPoints[2];
			intervalNumber = 1.0;
		}

		// 2 - 3
		if (controlPointCount > 3)
		{
			u = controlPoints[3] - controlPoints[2];
			v = color.rgb - controlPoints[2];
			crossLength = length(cross(u, v));
			uLength = length(u);
			distance = crossLength / uLength;
			if (distance < minDistance)
			{
				minDistance = distance;
				first = controlPoints[2];
				second = controlPoints[3];
				intervalNumber = 2.0;
			}

			// 3 - 4
			if (controlPointCount > 4)
			{
				u = controlPoints[4] - controlPoints[3];
				v = color.rgb - controlPoints[3];
				crossLength = length(cross(u, v));
				uLength = length(u);
				distance = crossLength / uLength;
				if (distance < minDistance)
				{
					minDistance = distance;
					first = controlPoints[3];
					second = controlPoints[4];
					intervalNumber = 3.0;
				}
			}
		}
	}

	// -----------------------------------

	vec3 interval = second - first;
	float projection;

	if (subIntervalNumber > 1)
	{
		// compute projection of color to interval
		vec3 div = color.rgb - first;
		float intervalLength = length(interval);
		projection = dot(div, interval) / (intervalLength * intervalLength);

		// restrict to sub-interval
		float step = 1.0 / float(subIntervalNumber - 1);
		projection = step * floor(projection / step + 0.5);
	}
	else
	{
		projection = 0.5;
	}

	intervalNumber += projection;

	// set color
	gl_FragColor.rgb = interval * projection + first; // interpolate
	gl_FragColor.a = color.a;
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
uniform int subIntervalNumber;
uniform int controlPointCount;
uniform vec3 controlPoints[5];

varying vec4 color;
varying float NdotL;

void main()
{
	vec4 fragmentColorWithoutLight;

	if (color.a < 0.999) // special color - undefined value, out-of-range value, selection
	{
		fragmentColorWithoutLight = color;
	}
	else
	{
		vec3 first, second;
		vec3 u, v;
		float minDistance = 1e20; // initial big number
		float crossLength, uLength, distance;

		// compute distance of color from interval
		
		// 0 - 1
		u = controlPoints[1] - controlPoints[0];
		v = color.rgb - controlPoints[0];
		crossLength = length(cross(u, v));
		uLength = length(u);
		distance = crossLength / uLength;
		if (distance < minDistance)
		{
			minDistance = distance;
			first = controlPoints[0];
			second = controlPoints[1];
		}

		// 1 - 2
		if (controlPointCount > 2)
		{
			u = controlPoints[2] - controlPoints[1];
			v = color.rgb - controlPoints[1];
			crossLength = length(cross(u, v));
			uLength = length(u);
			distance = crossLength / uLength;
			if (distance < minDistance)
			{
				minDistance = distance;
				first = controlPoints[1];
				second = controlPoints[2];
			}

			// 2 - 3
			if (controlPointCount > 3)
			{
				u = controlPoints[3] - controlPoints[2];
				v = color.rgb - controlPoints[2];
				crossLength = length(cross(u, v));
				uLength = length(u);
				distance = crossLength / uLength;
				if (distance < minDistance)
				{
					minDistance = distance;
					first = controlPoints[2];
					second = controlPoints[3];
				}

				// 3 - 4
				if (controlPointCount > 4)
				{
					u = controlPoints[4] - controlPoints[3];
					v = color.rgb - controlPoints[3];
					crossLength = length(cross(u, v));
					uLength = length(u);
					distance = crossLength / uLength;
					if (distance < minDistance)
					{
						minDistance = distance;
						first = controlPoints[3];
						second = controlPoints[4];
					}
				}
			}
		}

		// -----------------------------------

		vec3 interval = second - first;
		float projection;

		if (subIntervalNumber > 1)
		{
			// compute projection of color to interval
			vec3 div = color.rgb - first;
			float intervalLength = length(interval);
			projection = dot(div, interval) / (intervalLength * intervalLength);

			// restrict to sub-interval
			float step = 1.0 / float(subIntervalNumber - 1);
			projection = step * floor(projection / step + 0.5);
		}
		else
		{
			projection = 0.5;
		}

		// set color
		fragmentColorWithoutLight = vec4(interval * projection + first, color.a);
	}

	vec4 diffuse = fragmentColorWithoutLight * gl_LightSource[0].diffuse;
	vec4 ambient = fragmentColorWithoutLight * gl_LightSource[0].ambient + fragmentColorWithoutLight * gl_LightModel.ambient;

	gl_FragColor = ambient + NdotL * diffuse;
}
";

		#endregion

		#region Fields, constructor

		int subIntervalNumberLocation;
		int controlPointCountLocation;
		int controlPointsLocation;

		bool lightingEnabled;

		public IsoAreasShader(bool lighting)
		{
			IsReady = LoadShaderStrings(new[] { vertexShaderLightingString, vertexShaderString }, new[] { fragmentShaderLightingString, fragmentShaderString });
			if (IsReady)
			{
				this.lightingEnabled = lighting;
				setupAppropriateShaders();
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
					this.lightingEnabled = value;
					setupAppropriateShaders();
				}
			}
		}

		#endregion

		#region Public methods

		public void Use(int subIntervalNumber, ColorScale colorScale)
		{
			Debug.Assert(IsReady && subIntervalNumber > 0 && colorScale != null);

			int controlPointLength = colorScale.ControlPoints.Length;

			Debug.Assert(controlPointLength >= 2 && controlPointLength <= 5);

			GL.UseProgram(Program);

			GL.Uniform1(subIntervalNumberLocation, subIntervalNumber);
			GL.Uniform1(controlPointCountLocation, controlPointLength);

			float[] colorComponents = new float[controlPointLength * 3];

			for (int i = 0; i < controlPointLength; i++)
			{
				//double value = colorScale.ControlPoints[i].Value;
				//value = Math.Max(colorScale.MinValue, value);
				//value = Math.Min(colorScale.MaxValue, value);
				//int color = colorScale.GetColorForValue(value);
				int color = colorScale.ControlPoints[i].Color;
				float r, g, b;
				Utilities.Functions.GetColorComponents(color, out r, out g, out b);
				colorComponents[i * 3] = r;
				colorComponents[i * 3 + 1] = g;
				colorComponents[i * 3 + 2] = b;
			}

			GL.Uniform3(controlPointsLocation, controlPointLength, colorComponents);
		}

		public void Unuse()
		{
			GL.UseProgram(0);
		}

		#endregion

		#region Private methods

		private void setupAppropriateShaders()
		{
			int index = lightingEnabled ? 0 : 1;
			SetActiveShaders(vertexShaderIndex: index, fragmentShaderIndex: index);

			subIntervalNumberLocation = GL.GetUniformLocation(Program, "subIntervalNumber");
			controlPointCountLocation = GL.GetUniformLocation(Program, "controlPointCount");
			controlPointsLocation = GL.GetUniformLocation(Program, "controlPoints");
		}

		#endregion

	}
}

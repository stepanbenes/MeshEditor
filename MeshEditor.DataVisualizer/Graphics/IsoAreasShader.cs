using MeshEditor.DataVisualizer.Data;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.Graphics
{
	public class IsoAreasShader : ShaderHolder
	{
		private string vertexShaderLightingString =
@"

varying vec4 color;

varying vec4 diffuse, ambient;
varying vec3 normal, halfVector;

void main()
{
	/* first transform the normal into eye space and
	normalize the result */
	normal = normalize(gl_NormalMatrix * gl_Normal);
 
	/* pass the halfVector to the fragment shader */
	halfVector = gl_LightSource[0].halfVector.xyz;
 
	/* Compute the diffuse, ambient and globalAmbient terms */
	diffuse = gl_FrontMaterial.diffuse * gl_LightSource[0].diffuse;
	ambient = gl_FrontMaterial.ambient * gl_LightSource[0].ambient;
	ambient += gl_LightModel.ambient * gl_FrontMaterial.ambient;

	color = gl_Color;
	gl_Position = ftransform();
}
";

		private string fragmentShaderLightingString =
@"

uniform int subIntervalNumber;
uniform int controlPointCount;
uniform vec3 controlPoints[5];

varying vec4 color;

varying vec4 diffuse, ambient;
varying vec3 normal, halfVector;

void main()
{
	// LIGHTING ----------------------------------------------
	vec3 n, halfV, lightDir;
	float NdotL, NdotHV;
 
	lightDir = vec3(gl_LightSource[0].position);
 
	/* The ambient term will always be present */
	vec4 lightColor = ambient;
	/* a fragment shader can't write a varying variable, hence we need
	a new variable to store the normalized interpolated normal */
	n = normalize(normal);
	/* compute the dot product between normal and ldir */
 
	/* for ONE-SIDED Lighting:
	NdotL = max(dot(n, lightDir), 0.0);
	if (NdotL > 0.0)
	{
	    lightColor += diffuse * NdotL;
	    halfV = normalize(halfVector);
	    NdotHV = max(dot(n, halfV), 0.0);
	    lightColor += gl_FrontMaterial.specular * gl_LightSource[0].specular * pow(NdotHV, gl_FrontMaterial.shininess);
	}
	*/

	NdotL = abs(dot(n, lightDir));

	lightColor += diffuse * NdotL;
	halfV = normalize(halfVector);
	NdotHV = abs(dot(n, halfV));
	lightColor += gl_FrontMaterial.specular * gl_LightSource[0].specular * pow(NdotHV, gl_FrontMaterial.shininess);

	gl_FragColor = lightColor * 2.0;

	// -------------------------------------------------------

	if (color.a < 0.999) // special color - undefined value, out-of-range value, selection
	{
		gl_FragColor *= color;
		return;
	}

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
	gl_FragColor.rgb *= interval * projection + first; // interpolate
	//gl_FragColor.a = color.a;
}
";

		#region Non-lighting shaders

		private string vertexShaderString =
@"

varying vec4 color;

void main()
{
	color = gl_Color;
	gl_Position = ftransform();
}
";

		private string fragmentShaderString =
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

		public bool IsReady { get; private set; }

		int subIntervalNumberLocation;
		int controlPointCountLocation;
		int controlPointsLocation;

		bool lightingEnabled;

		public bool LightingEnabled
		{
			get { return lightingEnabled; }
			set
			{
				if (lightingEnabled != value)
				{
					setupAppropriateShaders(value);
				}
			}
		}

		private void setupAppropriateShaders(bool lighting)
		{
			this.lightingEnabled = lighting;
			int index = lighting ? 0 : 1;
			SetActiveShaders(vertexShaderIndex: index, fragmentShaderIndex: index);

			subIntervalNumberLocation = GL.GetUniformLocation(Program, "subIntervalNumber");
			controlPointCountLocation = GL.GetUniformLocation(Program, "controlPointCount");
			controlPointsLocation = GL.GetUniformLocation(Program, "controlPoints");
		}

		public IsoAreasShader(bool lighting)
		{
			IsReady = LoadShaderStrings(new[] { vertexShaderLightingString, vertexShaderString }, new[] { fragmentShaderLightingString, fragmentShaderString });
			if (IsReady)
			{
				setupAppropriateShaders(lighting);
			}
		}

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

	}
}

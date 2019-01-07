using System;

namespace SummerGUI
{
	public static class ShaderPrograms
	{
		/// <summary>
		/// Test for a Multisampled fragment shader - http://www.opentk.com/node/2251
		/// </summary>
		public const string fragmentShaderTestSrc =
			@"#version 330

uniform sampler2DMS Sampler;

in vec2 InTexture;
in vec4 OutColour;

out vec4 OutFragColor;

int samples = 16;
float div= 1.0/samples;

void main()
{
    OutFragColor = vec4(0.0);
    ivec2 texcoord = ivec2(textureSize(Sampler) * InTexture); // used to fetch msaa texel location
    for (int i=0;i<samples;i++)
    {
        OutFragColor += texelFetch(Sampler, texcoord, i) * OutColour;  // add  color samples together
    }

    OutFragColor*= div; //devide by num of samples to get color avg.
}";

		/// <summary>
		/// Default vertex shader that only applies specified matrix transformation
		/// </summary>
		public const string vertexShaderDefaultSrc =
			@"#version 330

uniform mat4 MVPMatrix;

layout (location = 0) in vec2 Position;
layout (location = 1) in vec2 Texture;
layout (location = 2) in vec4 Colour;

out vec2 InVTexture;
out vec4 vFragColorVs;

void main()
{
    gl_Position = MVPMatrix * vec4(Position, 0, 1);
    InVTexture = Texture;
    vFragColorVs = Colour;
}";

	}
}


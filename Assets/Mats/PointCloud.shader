Shader "Custom/PointCloud"
{
    Properties
    {
		_DepthTex("Depth", 2D) = "white" {}
		_ColorTex("Color", 2D) = "white" {}
	}
	SubShader
	{
		Pass
		{
			Tags { "RenderType" = "Opaque" }
			LOD 200

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			Texture2D _DepthTex;
			Texture2D _ColorTex;

			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.0

			// Vertex output
			struct VertexOutput {
				float4 pos : SV_POSITION;
				float4 col : COLOR;
			};

			VertexOutput vert(appdata_base input)
			{
				VertexOutput output;

				output.pos = UnityObjectToClipPos(input.vertex);

				// Need the depth value to set the grayscale color
				// How to get depth information here?
				float4 u = input.texcoord.x;
				float4 v = input.texcoord.y;
				int3 colorCoordinates = int3(input.texcoord.x, input.texcoord.y, 0);

				// Figure out how to do grayscale too
				output.col = _ColorTex.Load(colorCoordinates);

				return output;
			}

			float4 frag(VertexOutput input) : COLOR
			{
				return input.col;
			}

			ENDCG
		}
	}
    FallBack "Diffuse"
}

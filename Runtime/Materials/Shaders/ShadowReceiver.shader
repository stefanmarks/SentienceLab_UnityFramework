Shader "FX/Shadow Receiver"
{
	Properties
	{
		_ShadowColor("Shadow Color", COLOR)       = (0,0,0,1)
		_LightmapOffset("Lightmap Offset", COLOR) = (.5, 0.5, 0.5, 1)
	}

	SubShader
	{
		Tags { "RenderType" = "Transparent" }
		LOD 100

		Pass
		{
			Tags {"LightMode" = "ForwardBase"}

			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma multi_compile_fwdbase
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"

			float4 _ShadowColor;
			float4 _LightmapOffset;

			struct appdata {
				float4 vertex    : POSITION;
				float4 texcoord  : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 pos        : SV_POSITION;
				LIGHTING_COORDS(0, 1)
#ifdef LIGHTMAP_ON
				float2 lightmap_uv: TEXCOORD2;
#endif
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
#ifdef LIGHTMAP_ON
				o.lightmap_uv = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#endif
				TRANSFER_VERTEX_TO_FRAGMENT(o);

				return o;
			}

			fixed4 frag(v2f i) : COLOR
			{
				fixed  attenuation = 1 - LIGHT_ATTENUATION(i);
#ifdef LIGHTMAP_ON
				fixed3 lightMap    = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lightmap_uv)) / _LightmapOffset;
				       attenuation = max(1 - lightMap.r, attenuation);
#endif
				fixed4 finalColor  = fixed4(_ShadowColor.rgb, _ShadowColor.a * attenuation);
				return finalColor;
			}
			ENDCG
		}

	}

	Fallback "VertexLit"
}

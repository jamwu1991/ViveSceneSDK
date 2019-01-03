Shader "Unlit/Line"
{
	Properties
	{
		//_Color("Color", Color) = (1,1,1,1)
		_Opacity("Opacity", Range(0, 1)) = 1.0
	}
	
	SubShader
	{
		Tags
		{
			"Queue" = "Overlay"
			"RenderType" = "Transparent"
		}
		LOD 100

		Pass
		{
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
			};

			//uniform fixed4 _Color;
			uniform float _Opacity;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 c = i.color;
				c.a *= _Opacity;
				return c;

				//return i.color;
			}
			ENDCG
		}
	}
}

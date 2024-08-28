Shader "Unlit/PostProcessingShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            float4 _FogColor;
            float _FogDensity;
            float4x4 InverseViewProjection;
            float4 SunDirection;
            float4 SunColor;

            float3 NDCToWorld(float2 positionNDC, float deviceDepth) {
				float4 positionCS = float4(positionNDC * 2.0 - 1.0, deviceDepth, 1.0);
				float4 hpositionWS = mul(InverseViewProjection, positionCS);
				return hpositionWS.xyz / hpositionWS.w;
			}


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float depth = tex2D(_CameraDepthTexture, i.uv).r;

                float linearDepth = Linear01Depth(depth);

                float3 sun;
                if(linearDepth > 0.9){
                    float3 worldPos = NDCToWorld(i.uv, depth);
                    float3 viewDir = normalize(worldPos - _WorldSpaceCameraPos);
                    float3 sunDir = normalize(_WorldSpaceCameraPos - SunDirection);
                    sun = SunColor * pow(max(-1 * dot(viewDir, sunDir), 0.0), 3500.0f);
                }
                else sun = float4(0,0,0,1);

                
                float fogFactor = 1 - exp(-_FogDensity * linearDepth);


                col = lerp(col, _FogColor, fogFactor);

                return float4(col.rgb + sun, 1.0f);
            }
            ENDCG
        }
    }
}

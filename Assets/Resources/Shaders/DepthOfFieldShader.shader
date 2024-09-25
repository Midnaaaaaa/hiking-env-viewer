Shader "Unlit/DepthOfFieldShader"
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
            
            uniform float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float depth = tex2D(_CameraDepthTexture, i.uv).r;
                depth = Linear01Depth(depth); // Convert to linear depth

                if(depth >= 0.001){
                    float2 pixelSize = float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y);
                    // Define las direcciones a los píxeles vecinos.
                    float2 offsets[8] = { float2(-1, -1), float2(0, -1), float2(1, -1), float2(-1, 0), float2(1, 0), float2(-1, 1), float2(0, 1), float2(1, 1) };
                    // Define los pesos para un kernel gaussiano 3x3.
                    float weights[9] = { 1.0/16, 2.0/16, 1.0/16, 2.0/16, 4.0/16, 2.0/16, 1.0/16, 2.0/16, 1.0/16 };

                    // Muestrea el color de cada píxel vecino y aplica el filtro gaussiano.
                    float4 sum = float4(0, 0, 0, 0);
                    for (int j = 0; j < 8; ++j) {
                        float4 neighborColor = tex2D(_MainTex, i.uv + offsets[j] * pixelSize);
                        sum += neighborColor * weights[j];
                    }
                    return sum;

                }
                else return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}

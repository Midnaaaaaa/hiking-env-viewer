Shader "TextureShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _LandCover ("LandCover", 2D) = "white" {}
        _GrassTexture("GrassTexture", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _NumOfChunks("NumOfChunks", Int) = 4
        _ChunkXCoord("ChunkXCoord", Int) = 0
        _ChunkYCoord("ChunkYCoord", Int) = 0

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _LandCover;
        half4 _LandCover_TexelSize;
        sampler2D _GrassTexture;
        int _NumOfChunks;
        int _ChunkXCoord;
        int _ChunkYCoord;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float _LandCoverSize = _LandCover_TexelSize.z;
            float2 UVChunkCoords = float2(((_LandCoverSize * _ChunkXCoord) / (_NumOfChunks * _NumOfChunks)) + IN.uv_MainTex.x, 
                                          ((_LandCoverSize * _ChunkXCoord) / (_NumOfChunks * _NumOfChunks)) + IN.uv_MainTex.y);

            fixed4 landCoverPixel = tex2D (_LandCover, UVChunkCoords);

            /*
            if(abs(landCoverPixel.x - 0.19) < 0.01 && 
               abs(landCoverPixel.y - 0.81) < 0.01 &&
               abs(landCoverPixel.z - 0.16) < 0.01) // Pixel verde
            {
                o.Albedo = tex2D (_GrassTexture, IN.uv_MainTex);
            }
            */


            o.Albedo = landCoverPixel.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = landCoverPixel.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

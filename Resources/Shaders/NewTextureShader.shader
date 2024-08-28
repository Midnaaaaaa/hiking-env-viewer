Shader "Unlit/NewTextureShader"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("LandCoverArray", 2DArray) = "" {}
        _TexturesArray("TexturesArray", 2DArray) = "" {}
        _NormalMaps("NormalMaps", 2DArray) = "" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.0
        _Metallic("Metallic", Range(0,1)) = 0.0
        _NumOfChunks("NumOfChunks", Int) = 4
        _ChunkXCoord("ChunkXCoord", Int) = 0
        _ChunkYCoord("ChunkYCoord", Int) = 0
        _NumOfTextures("NumOfTextures", Int) = 5
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
            #include "UnityCG.cginc"

            UNITY_DECLARE_TEX2DARRAY(_MainTex);
            UNITY_DECLARE_TEX2DARRAY(_TexturesArray);
            UNITY_DECLARE_TEX2DARRAY(_NormalMaps);
            UNITY_DECLARE_TEXCUBE(tex);

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 localNormal : NORMAL;
            };

            struct v2f
            {
                float3 worldPos : TEXCOORD2;
                float2 uv : TEXCOORD0;
                float4 vertexClipPos : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
            };

            float4 _MainTex_ST;
            float contribution[12]; //We define the size so we don't run out of textures
            float _Tags[15];
            sampler2D primaryUVs;
            samplerCUBE SkyBox;
            int _NumOfChunks;
            int _ChunkXCoord;
            int _ChunkYCoord;
            int _NumOfTextures;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertexClipPos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = mul(UNITY_MATRIX_IT_MV, v.localNormal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f params) : SV_Target
            {
                float scale = 0.01;
                float distance = length(_WorldSpaceCameraPos - params.worldPos);
                if (distance < 500) scale = 0.03;

                // calculate triplanar blend
                half3 triblend = saturate(pow(params.worldNormal, 4));
                triblend = triblend / (triblend.x + triblend.y + triblend.z);

                // calculate triplanar uvs
                // applying texture scale and offset values ala TRANSFORM_TEX macro
                float2 uvX = params.worldPos.zy * scale;
                float2 uvY = params.worldPos.xz * scale;
                float2 uvZ = params.worldPos.xy * scale;

                half3 axisSign = params.worldNormal < 0 ? -1 : 1;

                float sumaTotal = 0;

                for (int i = 0; i < _NumOfTextures; i++) //Calcular nivell de vermell de cada landCover
                {
                    contribution[i] = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(params.uv, i)).r;
                    if(_Tags[i] == 2.0 || _Tags[i] == 3.0){
                        contribution[i] *= 8;
                    }
                
                    sumaTotal += contribution[i];
                }
                fixed4 finalCol = fixed4(0,0,0,1);
                fixed3 finalNormal = fixed3(0,0,0);
                float smooth = 0;
                for (int j = 0; j < _NumOfTextures; j++) //Calculem color final dels 3 eixos fer aplicar el triplanar mapping
                {
                    float contributionValue = contribution[j] / sumaTotal;


                    //if(contributionValue > 0) //If the contribution is 0 then it means that we dont need to calculate that texture contribution
                    //{
                    //    float2 offset1X = float2(0,0);
                    //    float2 offset1Y = float2(0,0);
                    //    half3 tnormalX2 = half3(0,0,0);
                    //    half3 tnormalY2 = half3(0,0,0);
                    //    half3 tnormalZ2 = half3(0,0,0);


                    //    if(_Tags[j] == 1.0 && contributionValue > 0.6) //If texture is water we will create an offset that we will add to the uvs in order to move its normals we will create 2 offsets and we will sample the same normal map twice but with different wave movements.
                    //    {
                    //        offset1X = _Time.x * float2(1, 1);
                    //        offset1Y = offset1X;
                    //        float2 offset2X = _Time.x * float2(-0.5, 0.28);
                    //        float2 offset2Y;
                    //        offset2Y = offset2X;

                    //        offset1X += sin(_Time.y * 2 + (uvX + uvY) * 25.0) * 0.01;
                    //        offset1Y += cos(_Time.y * 2 + (uvX - uvY) * 25.0) * 0.01;
                    //        offset2X += sin(_Time.y * 2 + (uvX + uvY) * 25.0) * 0.01;
                    //        offset2Y += cos(_Time.y * 2 + (uvX - uvY) * 25.0) * 0.01;

                    //        tnormalX2 = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(uvX + offset2X, j)));
                    //        tnormalY2 = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(uvY + offset2Y, j)));
                    //        tnormalZ2 = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(uvZ + offset2X, j)));

                    //        // swizzle world normals to match tangent space and apply reoriented normal mapping blend
                    //        tnormalX2 = half3(tnormalX2.xy + params.worldNormal.zy, tnormalX2.z * params.worldNormal.x);
                    //        tnormalY2 = half3(tnormalY2.xy + params.worldNormal.xz, tnormalY2.z * params.worldNormal.y);
                    //        tnormalZ2 = half3(tnormalZ2.xy + params.worldNormal.xy, tnormalZ2.z * params.worldNormal.z);

                    //        smooth = 0.8;
                    //    }
                    //    if(_Tags[j] == 2.0) //If texture is path we make the texture smaller
                    //    {
                    //        scale = 0.1;
                    //    }
                    //    if(_Tags[j] == 3.0 && contributionValue > 0.6){
                    //        float3 col = tex2D(primaryUVs, params.uv);
                    //        float2 realUVS = float2(col.x, col.y);
                    //        finalCol += UNITY_SAMPLE_TEX2DARRAY(_TexturesArray, float3(realUVS, j)) * contributionValue;
                    //        finalNormal += UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(realUVS, j))) * contributionValue;
                    //    }
                    //    else
                    //    {
                    //        fixed4 xProjection = UNITY_SAMPLE_TEX2DARRAY(_TexturesArray, float3(uvX, j)) * triblend.x * contributionValue;
                    //        fixed4 yProjection = UNITY_SAMPLE_TEX2DARRAY(_TexturesArray, float3(uvY, j)) * triblend.y * contributionValue;
                    //        fixed4 zProjection = UNITY_SAMPLE_TEX2DARRAY(_TexturesArray, float3(uvZ, j)) * triblend.z * contributionValue;
                   
                    //        half3 tnormalX = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(uvX + offset1X, j)));
                    //        half3 tnormalY = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(uvY + offset1Y, j)));
                    //        half3 tnormalZ = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(uvZ + offset1X, j)));

                    //        // swizzle world normals to match tangent space and apply reoriented normal mapping blend
                    //        tnormalX = half3(tnormalX.xy + params.worldNormal.zy, tnormalX.z * params.worldNormal.x);
                    //        tnormalY = half3(tnormalY.xy + params.worldNormal.xz, tnormalY.z * params.worldNormal.y);
                    //        tnormalZ = half3(tnormalZ.xy + params.worldNormal.xy, tnormalZ.z * params.worldNormal.z);

                    //        // swizzle tangent normals to match world normal and blend together
                    //        half3 worldNormal = tnormalX.zyx * triblend.x * contributionValue + tnormalX2.zyx * triblend.x * contributionValue + tnormalY.xzy * triblend.y * contributionValue + tnormalY2.xzy * triblend.y * contributionValue + tnormalZ.xyz * triblend.z * contributionValue + tnormalZ2.xyz * triblend.z * contributionValue;


                    //        finalNormal += worldNormal;
                    //        finalCol += xProjection + yProjection + zProjection;

                    //        if(_Tags[j] == 1.0 && contributionValue > 0.6)
                    //        {
                    //            float3 viewDir = normalize(_WorldSpaceCameraPos - params.worldPos);
                    //            float3 reflectDir = reflect(viewDir, worldNormal);
                    //            finalCol = lerp(texCUBE(SkyBox, reflectDir), finalCol, 0.5);
                    //        }
                    //    }
                    //}
                    finalCol += UNITY_SAMPLE_TEX2DARRAY(_TexturesArray, float3(params.uv, j)) * contributionValue;
                }

                //Lighting
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 viewDir = normalize(_WorldSpaceCameraPos - params.worldPos);

                ////Ambient Light
                float lightStrength = 1;
                float4 ambient = float4(1,1,1,1) * lightStrength;

                //// Diffuse
                float diff = max(0.0, abs(dot(params.worldNormal, lightDir))); // Use the absolute value of the dot product for two-sided lighting
                float4 diffuse = diff * float4(1,1,1,1);

                //// Specular
                //float specPower = 1.0; // Adjust this to change the size of the specular highlight
                //float3 reflectDir = reflect(lightDir, normal);
                //float spec = pow(max(dot(viewDir, reflectDir), 0.0), specPower);
                //float3 specular = spec * _LightColor0.rgb;


                return (finalCol * (ambient + diffuse));
            }
            ENDCG
        }
    }
}

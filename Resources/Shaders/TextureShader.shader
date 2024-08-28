Shader "TextureShader"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("LandCoverArray", 2DArray) = "" {}
        _TexturesArray("TexturesArray", 2DArray) = "" {}
        _PathLandCovers("PathLandCovers", 2DArray) = "" {}
        _NormalMaps("NormalMaps", 2DArray) = "" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.0
        _Metallic("Metallic", Range(0,1)) = 0.0
        _NumOfChunks("NumOfChunks", Int) = 4
        _ChunkXCoord("ChunkXCoord", Int) = 0
        _ChunkYCoord("ChunkYCoord", Int) = 0
        _NumOfLandCovers("NumOfTextures", Int) = 5
        _NumOfPathLandCovers("NumOfPathLandCovers", Int) = 5

    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows //vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        UNITY_DECLARE_TEX2DARRAY(_MainTex);
        UNITY_DECLARE_TEX2DARRAY(_PathLandCovers);
        UNITY_DECLARE_TEX2DARRAY(_TexturesArray);
        UNITY_DECLARE_TEX2DARRAY(_NormalMaps);
        UNITY_DECLARE_TEXCUBE(tex);

        int _NumOfChunks;
        int _ChunkXCoord;
        int _ChunkYCoord;
        int _NumOfLandCovers;
        int _NumOfPathLandCovers;

        struct Input
        {
            float3 worldNormal;
            float3 worldPos;
            float2 uv_MainTex;
            INTERNAL_DATA
        };

        half _Glossiness;
        half _Metallic;
        half4 _MainTex_TexelSize;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        float contribution[12]; //We define the size so we don't run out of textures
        float pathContributions[12]; //We define the size so we don't run out of textures
        float _Tags[15];
        float hasNormals[15];
        sampler2D primaryUVs;
        samplerCUBE SkyBox;
        int typeOfWaterAnimation;
        float2 windDir;
        float windStrength;
        float3 FogColor;
        float FogDensity;
        sampler2D distortionTex;
        float distortionRegions;
        static const float PI = 3.14159265f;

        float generateRandomNum(float min, float max, float2 co){
            float rand = frac(sin(dot(co, float2(12.9898,78.233))) * 43758.5453);
            return min + rand * (max - min);

        }

        float3 WorldToTangentNormalVector(Input IN, float3 normal) {
            float3 t2w0 = WorldNormalVector(IN, float3(1,0,0));
            float3 t2w1 = WorldNormalVector(IN, float3(0,1,0));
            float3 t2w2 = WorldNormalVector(IN, float3(0,0,1));
            float3x3 t2w = float3x3(t2w0, t2w1, t2w2);
            return normalize(mul(t2w, normal));
        }

        float3 VistaUVsColor(half3 triblend, float contributionValue, float2 uvX, float2 uvY, float2 uvZ, int j, half3 axisSign){
            //We solve the flipped planes
            uvX.x *= axisSign.x;
            uvY.x *= axisSign.y;
            uvZ.x *= -axisSign.z;

            //Triplanar + normal mapping
            float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(_TexturesArray, float3(uvX, j)).rgb * triblend.x * contributionValue;
            float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(_TexturesArray, float3(uvY, j)).rgb * triblend.y * contributionValue;
            float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(_TexturesArray, float3(uvZ, j)).rgb * triblend.z * contributionValue;

            return xProjection + yProjection + zProjection;
        }

        float3 VistaUVsNormal(half3 triblend, float contributionValue, float2 uvX, float2 uvY, float2 uvZ, Input IN, int j, half3 axisSign){
            //We solve the flipped planes
            uvX.x *= axisSign.x;
            uvY.x *= axisSign.y;
            uvZ.x *= -axisSign.z;


            float3 tnormalX = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(uvX, j)));
            float3 tnormalY = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(uvY, j)));
            float3 tnormalZ = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(uvZ, j)));

            tnormalX.x *= axisSign.x;
            tnormalY.x *= axisSign.y;
            tnormalZ.x *= -axisSign.z;

            // swizzle to world normals
            tnormalX = float3(tnormalX.xy + IN.worldNormal.zy, tnormalX.z * IN.worldNormal.x);
            tnormalY = float3(tnormalY.xy + IN.worldNormal.xz, tnormalY.z * IN.worldNormal.y);
            tnormalZ = float3(tnormalZ.xy + IN.worldNormal.xy, tnormalZ.z * IN.worldNormal.z); 

            // we apply triblending and contribution
            return tnormalX.zyx * triblend.x * contributionValue + tnormalY.xzy * triblend.y * contributionValue + tnormalZ.xyz * triblend.z * contributionValue;
        }

        // void vert(inout appdata_full v, out Input o)
        // {
        //     UNITY_INITIALIZE_OUTPUT(Input, o);
        //     float offsetY;
        //     for(int i = 0; i < _NumOfLandCovers; ++i)
        //     {
        //         if(i == 8) // Extrude rock vertices based on the normal
        //         {
        //             if(UNITY_SAMPLE_TEX2DARRAY_LOD(_MainTex, float3(v.texcoord.xy, i), 0).r > 0)
        //             {
        //                 float3 normal = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY_LOD(_NormalMaps, float3(v.texcoord.xy, i), 0));
        //                 float3 tangent = normalize(v.tangent.xyz);
        //                 float3 bitangent = cross(normal, tangent) * v.tangent.w;
        //                 float3x3 TBN = float3x3(tangent, bitangent, normal);
        //
        //                 // Transformar la normal al espacio del objeto
        //                 float3 normalEyeSpace = normalize(mul(TBN, normal));
        //                 float3 worldNormal = mul(transpose(UNITY_MATRIX_V), float4(normalEyeSpace.xyz, 0)).xyz;
        //                 float3 worldSpaceVert = mul(unity_ObjectToWorld, v.vertex);
        //                 worldSpaceVert += worldNormal * 2;
        //                 v.vertex = mul(unity_WorldToObject, worldSpaceVert);
        //             }
        //         }
        //     }
        //     // Flattening trails
        //     // for (int k = 0; k < _NumOfPathLandCovers; k++)
        //     // {
        //     //     if(_Tags[k] != 3.0) offsetY += UNITY_SAMPLE_TEX2DARRAY_LOD(_PathLandCovers, float3(v.texcoord.xy, k), 0).r;
        //     // }
        //     // v.vertex.y -= offsetY;
        // }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float scale = 0.1;
            float distance = length(_WorldSpaceCameraPos - IN.worldPos);
            float normalizedDistance = min(distance / (float)200, 1.0);

            // work around bug where IN.worldNormal is always (0,0,0)!
            IN.worldNormal = WorldNormalVector(IN, float3(0, 0, 1));

            // calculate triplanar blend
            // fem el dot product de la normal en world space per saber la contribucio de cada una de les proejccions
            half3 triblend = saturate(pow(IN.worldNormal, 4));
            triblend /= max(dot(triblend, half3(1, 1, 1)), 0.0001);

            // calculate triplanar uvs
            // applying texture scale and offset values ala TRANSFORM_TEX macro
            float2 uvX = IN.worldPos.zy;
            float2 uvY = IN.worldPos.xz;
            float2 uvZ = IN.worldPos.xy;

            float sumaTotal = 0;

            for (int i = 0; i < _NumOfLandCovers; i++) //Calcular nivell de vermell de cada landCover
            {
                contribution[i] = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(IN.uv_MainTex, i)).r;              
                sumaTotal += contribution[i];
            }
            for (int k = 0; k < _NumOfPathLandCovers; k++)
            {
                pathContributions[k] = UNITY_SAMPLE_TEX2DARRAY(_PathLandCovers, float3(IN.uv_MainTex, k)).r;
                if(_Tags[k + _NumOfLandCovers] == 2.0){
                    pathContributions[k] *= 4;
                }
                else if(_Tags[k + _NumOfLandCovers] == 3.0){
                    pathContributions[k] *= 16;
                }
                sumaTotal += pathContributions[k];
            }
            
            float3 finalCol;
            float3 finalNormal;
            float smooth = 0;
            float metallic = 0.5;
            int actualNormalCount = 0;
            half3 axisSign = IN.worldNormal < 0 ? -1 : 1;

            //Biome texturing
            for (int j = 0; j < _NumOfLandCovers; j++)
            {
                float contributionValue = contribution[j] / sumaTotal;
                float hasNormalMap = hasNormals[j];

                if(contributionValue > 0) //If the contribution is 0 then it means that we dont need to calculate that texture contribution
                {
                    if(_Tags[j] == 0) // We don't do triplanar if its another type of tag
                    {
                        float2 uvXFar = uvX * 0.01;
                        float2 uvYFar = uvY * 0.01;
                        float2 uvZFar = uvZ * 0.01;
                        float2 uvXNear = uvX * scale;
                        float2 uvYNear = uvY * scale;
                        float2 uvZNear = uvZ * scale;

                        if(distortionRegions > 0)
                        {
                            float noise = tex2D(distortionTex, IN.uv_MainTex);
                            noise = round(noise * distortionRegions) * 1 / distortionRegions;
                            float randomOffsetRotation = generateRandomNum(0,1,float2(noise, 0)) * 2 * PI;
                            float2x2 rotationMatrix = float2x2(float2(cos(randomOffsetRotation), sin(randomOffsetRotation)), float2(-sin(randomOffsetRotation), cos(randomOffsetRotation)));
                            uvXFar = mul(rotationMatrix, uvXFar);
                            uvYFar = mul(rotationMatrix, uvYFar);
                            uvZFar = mul(rotationMatrix, uvZFar);
                            uvXNear = mul(rotationMatrix, uvXNear);
                            uvYNear = mul(rotationMatrix, uvYNear);
                            uvZNear = mul(rotationMatrix, uvZNear);
                        }
                        float3 Col1 = VistaUVsColor(triblend, contributionValue, uvXNear, uvYNear, uvZNear, j, axisSign);
                        float3 Col2 = VistaUVsColor(triblend, contributionValue, uvXFar, uvYFar, uvZFar, j, axisSign);

                        if(hasNormalMap == 1.0){
                            float3 Normal1 = VistaUVsNormal(triblend, contributionValue, uvXNear, uvYNear, uvZNear, IN, actualNormalCount, axisSign);
                            float3 Normal2 = VistaUVsNormal(triblend, contributionValue, uvXFar, uvYFar, uvZFar, IN, actualNormalCount, axisSign);
                            finalNormal += lerp(Normal1, Normal2, normalizedDistance);
                        }
                        else finalNormal += IN.worldNormal; 

                        finalCol += lerp(Col1, Col2, normalizedDistance);                   
                    }
                    else if(_Tags[j] == 1.0 && contributionValue > 0.6)
                    {
                        contributionValue -= 0.6;
                        contributionValue = contributionValue / 0.4;
                        float3 waterCol = UNITY_SAMPLE_TEX2DARRAY(_TexturesArray, float3(uvY, j)).rgb;
                        float fogFactor = 1 - exp(-FogDensity * 1); // 1 is linearDepth

                        smooth = 0.8 * contributionValue;

                        if(typeOfWaterAnimation == 0)
                        {
                            fogFactor -= 0.2;
                            if(hasNormalMap == 1.0){
                                float2 offset1 = float2(0,0);

                                offset1.x += sin(_Time.y * 2 + (uvY.x + uvY.y) * 0.04) * 0.0005;
                                offset1.y += cos(_Time.y * 2 + (uvY.x - uvY.y) * 0.04) * 0.0005;

                                float3 normal1 = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(uvY * (1/(float)250) * 0.04 + offset1, actualNormalCount)));
                                normal1 = float3(normal1.xy + IN.worldNormal.xz, normal1.z * IN.worldNormal.y);

                                finalNormal += normal1.xzy * contributionValue;
                            }
                            else finalNormal += IN.worldNormal;
                        }
                        else if(typeOfWaterAnimation == 1)
                        {
                            fogFactor -= 0.2;
                            finalNormal += float3(0,1,0);
                        }
                        else if(typeOfWaterAnimation == 2){
                            if(hasNormalMap == 1.0){
                                windDir = normalize(-windDir);
                                float rotationAngle = radians(-10);

                                float2x2 rotationMatrix = float2x2(float2(cos(rotationAngle), sin(rotationAngle)), float2(-sin(rotationAngle), cos(rotationAngle)));
                                float2 newWinDir = mul(rotationMatrix, windDir);

                                float2 offset1 = _Time.x * windDir * windStrength;
                                float2 offset2 = _Time.x * newWinDir * windStrength/2;

                                offset1.x += cos(_Time.y * 2 + (uvY.x + uvY.y) * 0.1) * 0.01;
                                offset1.y += sin(_Time.y * 2 + (uvY.x - uvY.y) * 0.1) * 0.01;

                                offset2.x += cos(_Time.y * 2 + (uvY.x + uvY.y) * 0.1) * 0.01;
                                offset2.y += sin(_Time.y * 2 + (uvY.x - uvY.y) * 0.1) * 0.01;


                                float3 normal1 = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(uvY * 0.1 + offset1, actualNormalCount)));
                                normal1 = float3(normal1.xy + IN.worldNormal.xz, normal1.z * IN.worldNormal.y).xzy * contributionValue;
                                float3 normal2 = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(uvY * 0.1 + offset2, actualNormalCount)));
                                normal2 = float3(normal2.xy + IN.worldNormal.xz, normal2.z * IN.worldNormal.y).xzy * contributionValue;


                                float3 normal3 = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(uvY * 0.015 + offset1, actualNormalCount)));
                                normal3 = float3(normal3.xy + IN.worldNormal.xz, normal3.z * IN.worldNormal.y).xzy * contributionValue;
                                float3 normal4 = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(uvY * 0.015 + offset2, actualNormalCount)));
                                normal4 = float3(normal4.xy + IN.worldNormal.xz, normal4.z * IN.worldNormal.y).xzy * contributionValue;

                                float3 finalNormal1 = normal1 + normal2;
                                float3 finalNormal2 = normal3 + normal4;

                                finalNormal += lerp(finalNormal1, finalNormal2, normalizedDistance);
                            }
                            else finalNormal += IN.worldNormal;
                        }

                        else if(typeOfWaterAnimation == 3){
                            if(hasNormalMap == 1.0){
                                windDir = normalize(-windDir);
                                float rotationAngle = radians(60);

                                float2x2 rotationMatrix = float2x2(float2(cos(rotationAngle), sin(rotationAngle)), float2(-sin(rotationAngle), cos(rotationAngle)));
                                float2 newWinDir = mul(rotationMatrix, windDir);

                                float2 offset1 = _Time.x * windDir * windStrength;
                                float2 offset2 = _Time.x * newWinDir * windStrength/2;

                                offset1.x += cos(_Time.y * 2 + (uvY.x + uvY.y) * 0.1 * 10) * 0.01;
                                offset1.y += sin(_Time.y * 2 + (uvY.x - uvY.y) * 0.1 * 10) * 0.01;

                                offset2.x += sin(_Time.y * 2 + (uvY.x - uvY.y) * 0.1 * 10) * 0.01;
                                offset2.y += cos(_Time.y * 2 + (uvY.x + uvY.y) * 0.1 * 10) * 0.01;


                                float3 normal1 = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(uvY * 0.1 + offset1, actualNormalCount)));
                                normal1 = float3(normal1.xy + IN.worldNormal.xz, normal1.z * IN.worldNormal.y).xzy * contributionValue;
                                float3 normal2 = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(uvY * 0.1 + offset2, actualNormalCount)));
                                normal2 = float3(normal2.xy + IN.worldNormal.xz, normal2.z * IN.worldNormal.y).xzy * contributionValue;


                                float3 normal3 = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(uvY * 0.015 + offset1, actualNormalCount)));
                                normal3 = float3(normal3.xy + IN.worldNormal.xz, normal3.z * IN.worldNormal.y).xzy * contributionValue;
                                float3 normal4 = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(uvY * 0.015 + offset2, actualNormalCount)));
                                normal4 = float3(normal4.xy + IN.worldNormal.xz, normal4.z * IN.worldNormal.y).xzy * contributionValue;

                                float3 finalNormal1 = normal1 + normal2;
                                float3 finalNormal2 = normal3 + normal4;

                                finalNormal += lerp(finalNormal1, finalNormal2, normalizedDistance);
                            }
                            else finalNormal += IN.worldNormal;
                        }
                        
                        float3 viewDir = normalize(IN.worldPos - _WorldSpaceCameraPos);
                        float angle = -dot(viewDir, float3(0,1,0));
                        float airRefractionIndex = 1;
                        float waterRefractionIndex = 1/(float)3;
                        float r = pow((airRefractionIndex - waterRefractionIndex) / (airRefractionIndex + waterRefractionIndex), 2);
                        float fresnel = r + (1-r)*pow(1 - angle, 5);
                        float3 reflectDir = reflect(viewDir, finalNormal);
                        float3 skyboxCol = texCUBE(SkyBox, reflectDir).rgb;
                        float3 skyboxColWithFog = lerp(skyboxCol, FogColor, fogFactor);

                        finalCol += lerp(waterCol, skyboxColWithFog, fresnel) * contributionValue;
                    }
                }
                if(hasNormalMap == 1.0) ++actualNormalCount;
            }


            //Path texturing
            for(int l = 0; l < _NumOfPathLandCovers; ++l){
                int index = l + _NumOfLandCovers;
                
                float contributionValue = pathContributions[l] / sumaTotal;
                float hasNormalMap = hasNormals[index];

                if(contributionValue > 0){
                    if(_Tags[index] == 2.0){

                        float3 Col1 = VistaUVsColor(triblend, contributionValue, uvX * 0.1, uvY * 0.1, uvZ * 0.1, index, axisSign);
                        float3 Col2 = VistaUVsColor(triblend, contributionValue, uvX * 0.01, uvY * 0.01, uvZ * 0.01, index, axisSign);

                        if(hasNormalMap == 1.0){
                            float3 Normal1 = VistaUVsNormal(triblend, contributionValue, uvX * 0.1, uvY * 0.1, uvZ * 0.1, IN, actualNormalCount, axisSign);
                            float3 Normal2 = VistaUVsNormal(triblend, contributionValue, uvX * 0.01, uvY * 0.01, uvZ * 0.01, IN, actualNormalCount, axisSign);
                            finalNormal += lerp(Normal1, Normal2, normalizedDistance);
                        }
                        else finalNormal += IN.worldNormal; 

                        finalCol += lerp(Col1, Col2, normalizedDistance);  
                    }
                    else if(_Tags[index] == 3.0 && contributionValue > 0.6) //Carretera
                    {
                        contributionValue -= 0.6;
                        contributionValue = contributionValue / 0.4;
                        float3 col = tex2D(primaryUVs, IN.uv_MainTex);
                        float2 realUVS = float2(col.x, col.y);
                        finalCol = UNITY_SAMPLE_TEX2DARRAY(_TexturesArray, float3(realUVS, index)).rgb;
                        metallic = 0;
                        smooth = 0;

                        if(hasNormalMap == true){
                            finalNormal = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(realUVS, actualNormalCount)));
                        }
                        else finalNormal = IN.worldNormal;
                    }

                }
                if(hasNormalMap == 1.0) ++actualNormalCount;
            }
            o.Normal = WorldToTangentNormalVector(IN, finalNormal);
            o.Albedo = finalCol;
            o.Metallic = metallic;
            o.Smoothness = smooth;
            o.Alpha = 1;
        }
        ENDCG
    }
        FallBack "Diffuse"
}
Shader "GrassShader"
{
    Properties
    {
        _MainTex ("GrassLandcover", 2D) = "white" {}    //GrassLandcover
        _TexturesArray("TexturesArray", 2DArray) = "" {}
        _NormalMaps("NormalMaps", 2DArray) = "" {}
        _LandCovers("LandCovers", 2DArray) = "" {}
        _NumOfChunks("NumOfChunks", Int) = 4
        _ChunkXCoord("ChunkXCoord", Int) = 0
        _ChunkYCoord("ChunkYCoord", Int) = 0
        _GrassNormalMap("NormalMap", 2D) = "bump" {}    //NormalMap
        _Radius("Radius", Float) = 1500.0               //Radius
    }
    SubShader {
        Tags {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        Pass {
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "UnityPBSLighting.cginc"

            UNITY_DECLARE_TEX2DARRAY(_TexturesArray);
            UNITY_DECLARE_TEX2DARRAY(_NormalMaps);
            UNITY_DECLARE_TEX2DARRAY(_LandCovers);

            struct appdata_t {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float3 normal   : NORMAL;
                float4 tangent  : TANGENT;
            };

            struct v2f {
                float4 vertex   : SV_POSITION;
                float2 uvOfGrassLandcover : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 normal   : TEXCOORD5;
                float3 tangent  : TEXCOORD6;
                float3 bitangent : TEXCOORD7;
                int textureUsed : TEXCOORD3;
                float objectProb : TEXCOORD4;
            }; 

            //struct MeshProperties {
            //    float3 pos;
            //    float2 uv;
            //};

            int _NumOfChunks;
            int _ChunkXCoord;
            int _ChunkYCoord;
            int length;
            sampler2D _MainTex;
            sampler2D _SlopeAngle;
            float _WindFrequency;
            StructuredBuffer<float3> _Properties;
            StructuredBuffer<float4x4> _ModelTransformBuffer;
            StructuredBuffer<int> _TypeOfTextureBuffer;
            int _NumOfLandCovers;
            int usingJsonTrees;
            StructuredBuffer<float> landCoverProbs;

            float4 _FogColor;
            float _FogDensity;

            float rand(float seed)
            {
                float2 uv = float2(seed * 0.618033988749895 + 0.447213595499958, seed * 0.618033988749895);
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }


            v2f vert(appdata_t i, uint instanceID: SV_InstanceID) {
                v2f o;

                float2 uvs = float2((_Properties[instanceID].x % length) / (float) length, (_Properties[instanceID].z % length) / (float) length);

                float2 UVChunkCoords = float2((_ChunkXCoord / (float)_NumOfChunks) + uvs.x / (float)_NumOfChunks,
                                              (_ChunkYCoord / (float)_NumOfChunks) + uvs.y / (float)_NumOfChunks);
                

                float3x3 rotationMatrix = (float3x3)_ModelTransformBuffer[instanceID];
                
                //Transformem la matriu de rotacio a objectSpace
                float3x3 inverseRotationMatrix = transpose(rotationMatrix);

                float3 windDir = float3(rand(instanceID),0,rand(instanceID+1));
                windDir = mul(inverseRotationMatrix, windDir);

                float4 movedVertex = i.vertex;
                float val;
                if(movedVertex.y > 0){
                    val = cos(_Time.y * (rand(instanceID))) * ((rand(instanceID) + 1) * 0.3);
                    val = val * val * 0.5;
                    movedVertex.xyz += windDir * val;
                }
                
                float4 position = mul(_ModelTransformBuffer[instanceID], movedVertex);
                o.vertex = UnityObjectToClipPos(position);
                o.worldPos = position.xyz;

                o.uvOfGrassLandcover = UVChunkCoords;
                o.uv = i.uv;
                o.textureUsed = _TypeOfTextureBuffer[instanceID];
                o.objectProb = rand((float) instanceID);
                o.normal = UnityObjectToWorldNormal(i.normal);
                o.tangent = UnityObjectToWorldDir(i.tangent.xyz);
                o.bitangent = cross(o.normal, o.tangent) * i.tangent.w;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                //Contribution
                if(usingJsonTrees == 0)
                {
                    float sumaTotal = 0;
                    float sumaTotalNoProb = 0;
                    float k = 0;
                    for (int j = 0; j < _NumOfLandCovers; j++) //Calcular nivell de vermell de cada landCover
                    {
                        float col = UNITY_SAMPLE_TEX2DARRAY(_LandCovers, float3(i.uvOfGrassLandcover, j)).r;
                        if(col > 0.2) ++k;
                        sumaTotalNoProb += col;
                        sumaTotal += col * landCoverProbs[j] / (float) 100;
                    }
                    float fullContributionNoProb = 0;
                    float fullContribution = 0;
                    if(k != 0)
                    {
                        fullContributionNoProb = sumaTotalNoProb / k; 
                        fullContribution = sumaTotal / k;
                    }
                    if(fullContributionNoProb < 0.6){
                        discard;
                    }

                    if(i.objectProb >= fullContribution){
                        discard;
                    }

                }

                //Discarding alpha
                float4 col = UNITY_SAMPLE_TEX2DARRAY(_TexturesArray, float3(i.uv, i.textureUsed));
                if(col.w <= 0.6) discard;

                float linearDist = distance(i.worldPos, _WorldSpaceCameraPos) / _ProjectionParams.z;
                float fogFactor = 1 - exp(-_FogDensity * linearDist);


                col = lerp(col, _FogColor, fogFactor);
                
                
                //Lighting
                float3 tangentSpaceNormal = normalize(UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(i.uv, i.textureUsed))));

                float3x3 TBN = {
                    i.tangent.x, i.bitangent.x, i.normal.x,
                    i.tangent.y, i.bitangent.y, i.normal.y,
                    i.tangent.z, i.bitangent.z, i.normal.z
                };

                float3 worldNormal = mul(TBN, tangentSpaceNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 viewDir = normalize(i.worldPos - _WorldSpaceCameraPos);


                //Ambient Light
                float lightStrength = 0.1;
                float3 ambient = _LightColor0.rgb * lightStrength;


                // Diffuse
                float diff = max(0.0, pow(abs(dot(worldNormal, lightDir)), 1/4)); // Use the absolute value of the dot product for two-sided lighting
                float3 diffuse = diff * _LightColor0.rgb;

                //// Specular
                // float specPower = 1.0; // Adjust this to change the size of the specular highlight
                // float3 reflectDir = reflect(lightDir, normal);
                // float spec = pow(max(dot(viewDir, reflectDir), 0.0), specPower);
                // float3 specular = spec * _LightColor0.rgb;

                // Combine
                col.rgb *= diffuse + ambient;

                return col;
            }
            ENDCG
        }
    }
}
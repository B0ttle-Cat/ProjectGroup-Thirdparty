Shader "Hidden/FullScreen/FOW/GrayScale"
{
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma multi_compile_local IS_2D IS_3D

            #pragma vertex Vert
            #pragma fragment Frag

            #include_with_pragmas "FogOfWarLogic.hlsl"
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            #if UNITY_VERSION <= 202310
            uniform float4 _BlitTexture_TexelSize;
            #endif

            float _maxDistance;
            float2 _fowTiling;
            float _fowScrollSpeed;
            float4 _unKnownColor;
            float _saturationStrength;

            float4x4 _camToWorldMatrix;
            float4x4 _inverseProjectionMatrix;

            float4 Frag (Varyings i) : SV_Target
            {
                //float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, i.texcoord, _BlitMipLevel);

                float2 pos;
                float height;
            #if IS_2D
                
                pos = (i.texcoord * float2(2,2) - float2(1,1)) * _cameraSize * float2(_BlitTexture_TexelSize.z / _BlitTexture_TexelSize.w, 1);
                pos+= _cameraPosition;
                Unity_Rotate_Degrees_float(pos, _cameraPosition, -_cameraRotation, pos);
                height = 0;
            #elif IS_3D
                float2 uv = i.texcoord;

            #if UNITY_REVERSED_Z
                real depth = SampleSceneDepth(i.texcoord);
            #else
                real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(i.texcoord));
            #endif

                float3 vpos = ComputeViewSpacePosition(i.texcoord, depth, UNITY_MATRIX_I_P);
                if (vpos.z > _maxDistance)
                    return color;

                vpos.z*=-1;
                float4 worldPos = mul(_camToWorldMatrix, float4(vpos, 1));

                GetFowSpacePosition(worldPos, pos, height);
            #endif

                float coneCheckOut = 0;
                FOW_Sample_float(pos, height, coneCheckOut);
                CustomCurve_float(coneCheckOut, coneCheckOut);
                TextureSample_float(pos, coneCheckOut);

                OutOfBoundsCheck(pos, color);
                float luma = dot(color.rgb * _unKnownColor, float3(0.2126729, 0.7151522, 0.0721750));
                float3 saturatedColor = luma.xxx + _saturationStrength.xxx * (color.rgb - luma.xxx);
                return float4(lerp(saturatedColor, color.rgb, coneCheckOut), color.a);
            }
            ENDHLSL
        }
    }
}
// Shader "Hidden/FullScreen/FOW/GrayScale"
// {
//     Properties
//     {
//         _MainTex("Texture", 2D) = "white" {}
//     }
//     SubShader
//     {
//         // No culling or depth
//         Cull Off ZWrite Off ZTest Always

//         Pass
//         {
//             CGPROGRAM
//             #pragma multi_compile_local IS_2D IS_3D

//             #pragma vertex vert
//             #pragma fragment frag

//             #include "UnityCG.cginc"
//             #include_with_pragmas "FogOfWarLogic.hlsl"

//             struct appdata
//             {
//                 float4 vertex : POSITION;
//                 float2 uv : TEXCOORD0;
//             };

//             struct v2f
//             {
//                 float2 uv : TEXCOORD0;
//                 float4 vertex : SV_POSITION;
//             };

//             v2f vert (appdata v)
//             {
//                 v2f o;
//                 o.vertex = UnityObjectToClipPos(v.vertex);
//                 o.uv = v.uv;
//                 return o;
//             }

//             sampler2D _MainTex;
//             float4 _MainTex_TexelSize;
//             sampler2D _CameraDepthTexture;

//             float _maxDistance;
//             float4x4 _camToWorldMatrix;
//             float4x4 _inverseProjectionMatrix;

//             float3 _unKnownColor;
//             float _saturationStrength;

//             fixed4 frag (v2f i) : SV_Target
//             {
//                 fixed4 color = tex2D(_MainTex, i.uv);
                
//                 float2 pos;
//                 float height;
// #if IS_2D
//                 pos = (i.uv * float2(2,2) - float2(1,1)) * _cameraSize * float2(_MainTex_TexelSize.z/ _MainTex_TexelSize.w,1);
//                 pos+= _cameraPosition;
//                 Unity_Rotate_Degrees_float(pos, _cameraPosition, -_cameraRotation, pos);
//                 height = 0;
// #elif IS_3D
//                 const float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
//                 const float2 p13_31 = float2(unity_CameraProjection._13, unity_CameraProjection._23);
//                 const float isOrtho = unity_OrthoParams.w;
//                 const float near = _ProjectionParams.y;
//                 const float far = _ProjectionParams.z;

//                 float d = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
//         #if defined(UNITY_REVERSED_Z)
//                 d = 1 - d;
//         #endif
//                 float zOrtho = lerp(near, far, d);
//                 float zPers = near * far / lerp(far, near, d);
//                 float vz = lerp(zPers, zOrtho, isOrtho);

//                 if (vz > _maxDistance)
//                     return color;

//                 float3 vpos = float3((i.uv * 2 - 1 - p13_31) / p11_22 * lerp(vz, 1, isOrtho), -vz);
//                 float4 worldPos = mul(_camToWorldMatrix, float4(vpos, 1));

//                 GetFowSpacePosition(worldPos, pos, height);
// #endif

//                 float coneCheckOut = 0;
//                 FOW_Sample_float(pos, height, coneCheckOut);
//                 CustomCurve_float(coneCheckOut, coneCheckOut);
//                 TextureSample_float(pos, coneCheckOut);

//                 OutOfBoundsCheck(pos, color);
//                 float luma = dot(color.rgb * _unKnownColor, float3(0.2126729, 0.7151522, 0.0721750));
//                 float3 saturatedColor = luma.xxx + _saturationStrength.xxx * (color.rgb - luma.xxx);
//                 return float4(lerp(saturatedColor, color.rgb, coneCheckOut), color.a);
//             }
//             ENDCG
//         }
//     }
// }

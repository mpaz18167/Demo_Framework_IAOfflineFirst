Shader "Demo/Outline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0.10, 0.08, 0.16, 1)
        _OutlineWidth("Outline Width", Range(0.0, 0.1)) = 0.03
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            CBUFFER_START(UnityPerMaterial)
                float  _OutlineWidth;
                half4  _OutlineColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nrmWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                posWS += nrmWS * _OutlineWidth;
                OUT.positionCS = TransformWorldToHClip(posWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target { return _OutlineColor; }
            ENDHLSL
        }
    }
    Fallback Off
}
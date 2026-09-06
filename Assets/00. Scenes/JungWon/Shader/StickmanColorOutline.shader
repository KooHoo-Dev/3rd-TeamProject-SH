Shader "Custom/StickmanColorOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.85, 0.3, 1)
        // Range 상한을 넉넉하게 열어서 "너무 얇다"는 문제를 인스펙터에서 바로 해결할 수 있게 함
        _OutlineWidth ("Outline Width", Range(0.0, 0.2)) = 0.06
    }
    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry+1" // 원본 지오메트리 뒤에 그려지도록
        }

        Pass
        {
            Name "Outline"
            Cull Front   // Inverted Hull 핵심: 뒷면만 남겨서 바깥으로 삐져나온 테두리를 만든다
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // 월드 스페이스에서 오프셋을 줘야 비균일 스케일에서도
                // 두께가 방향별로 다르게 왜곡되지 않는다.
                float3 posWS    = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                posWS += normalWS * _OutlineWidth;

                OUT.positionCS = TransformWorldToHClip(posWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
    FallBack Off
}

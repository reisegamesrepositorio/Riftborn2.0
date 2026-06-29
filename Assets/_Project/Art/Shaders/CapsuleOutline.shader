Shader "Custom/CapsuleOutline"
{
    Properties
    {
        _OutlineColor(
            "Outline Color",
            Color
        ) = (1, 0, 0, 1)

        _OutlineWidth(
            "Outline Width",
            Range(0.001, 0.1)
        ) = 0.03
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry+1"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "Outline"

            Cull Front
            ZWrite Off
            ZTest LEqual

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
            };

            fixed4 _OutlineColor;
            float _OutlineWidth;

            v2f vert(appdata input)
            {
                v2f output;

                float3 expandedPosition =
                    input.vertex.xyz +
                    normalize(input.normal) * _OutlineWidth;

                output.position =
                    UnityObjectToClipPos(
                        float4(expandedPosition, 1.0)
                    );

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                return _OutlineColor;
            }

            ENDCG
        }
    }

    Fallback Off
}
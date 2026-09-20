Shader "CustomUI/UIShadowBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ShadowOffset ("Shadow Offset", Vector) = (0.02, -0.02, 0, 0)
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
        _BlurSize ("Blur Size", Range(0, 0.05)) = 0.01 // Độ nhòe của bóng
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _ShadowOffset;
            fixed4 _ShadowColor;
            float _BlurSize;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1. Lấy màu gốc (Foreground)
                fixed4 col = tex2D(_MainTex, IN.texcoord) * IN.color;

                // 2. Tính toán độ mờ của bóng (Blur Shadow)
                // Lấy mẫu 9 điểm xung quanh vị trí bóng đổ để tạo hiệu ứng nhòe
                float shadowAlpha = 0;
                float2 shadowUV = IN.texcoord - _ShadowOffset.xy;

                // Ma trận lấy mẫu (3x3 grid)
                for (float x = -1; x <= 1; x++)
                {
                    for (float y = -1; y <= 1; y++)
                    {
                        float2 offset = float2(x, y) * _BlurSize;
                        shadowAlpha += tex2D(_MainTex, shadowUV + offset).a;
                    }
                }
                shadowAlpha /= 9.0; // Chia trung bình cho 9 mẫu
                shadowAlpha *= _ShadowColor.a; // Nhân với độ trong suốt của màu bóng đã chọn

                // 3. Trộn màu (Blending)
                // Ưu tiên hiển thị màu gốc, nếu không có màu gốc thì hiển thị màu bóng
                float3 finalColor = lerp(_ShadowColor.rgb, col.rgb, col.a);
                
                // Alpha tổng hợp: lấy giá trị lớn nhất giữa hình gốc và bóng mờ
                float finalAlpha = max(col.a, shadowAlpha);

                return fixed4(finalColor, finalAlpha);
            }
            ENDCG
        }
    }
}
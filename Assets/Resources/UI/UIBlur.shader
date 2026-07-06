Shader "UI/Blur"
{
    // Built-in RP용 GrabPass 기반 UI 블러. Settings backdrop 등 UI overlay에 사용.
    // 뒷 화면(그랩)에 tint color 오버레이 + gaussian 4-tap 블러.
    Properties
    {
        _Color ("Tint Color (RGBA)", Color) = (0,0,0,0.5)
        _BlurSize ("Blur Size (px)", Range(0, 20)) = 4
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        // 뒤 화면 캡처
        GrabPass { "_UIBlurGrabTex" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _UIBlurGrabTex;
            float4 _UIBlurGrabTex_TexelSize;
            float4 _Color;
            float _BlurSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 grabPos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.grabPos.xy / i.grabPos.w;
                float2 texel = _UIBlurGrabTex_TexelSize.xy * _BlurSize;

                // 5-tap box blur (center + 4 diagonal)
                fixed4 c = tex2D(_UIBlurGrabTex, uv);
                c += tex2D(_UIBlurGrabTex, uv + float2( texel.x,  texel.y));
                c += tex2D(_UIBlurGrabTex, uv + float2(-texel.x,  texel.y));
                c += tex2D(_UIBlurGrabTex, uv + float2( texel.x, -texel.y));
                c += tex2D(_UIBlurGrabTex, uv + float2(-texel.x, -texel.y));
                c /= 5.0;

                // tint 오버레이 (검은색 반투명)
                fixed3 rgb = lerp(c.rgb, _Color.rgb, _Color.a);
                return fixed4(rgb, 1.0);
            }
            ENDCG
        }
    }
}

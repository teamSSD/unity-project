Shader "Hidden/UIBlurPostProcess"
{
    // Graphics.Blit 오프스크린 블러용. UI 자체에 안 붙임 (RawImage는 blit 결과 텍스처만 표시).
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color (RGBA — a로 검게 섞음)", Color) = (0,0,0,0.6)
        _BlurSize ("Blur Size (px)", Range(0, 30)) = 6
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Color;
            float _BlurSize;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy * _BlurSize;

                // 9-tap box blur
                fixed4 c = tex2D(_MainTex, i.uv);
                c += tex2D(_MainTex, i.uv + float2( texel.x,  texel.y));
                c += tex2D(_MainTex, i.uv + float2(-texel.x,  texel.y));
                c += tex2D(_MainTex, i.uv + float2( texel.x, -texel.y));
                c += tex2D(_MainTex, i.uv + float2(-texel.x, -texel.y));
                c += tex2D(_MainTex, i.uv + float2( texel.x, 0));
                c += tex2D(_MainTex, i.uv + float2(-texel.x, 0));
                c += tex2D(_MainTex, i.uv + float2(0,  texel.y));
                c += tex2D(_MainTex, i.uv + float2(0, -texel.y));
                c /= 9.0;

                // tint 오버레이 (검정 α 만큼 어둡게)
                fixed3 rgb = lerp(c.rgb, _Color.rgb, _Color.a);
                return fixed4(rgb, 1.0);
            }
            ENDCG
        }
    }
}

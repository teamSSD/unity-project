Shader "Hidden/UIBlurPostProcess"
{
    // Graphics.Blit 오프스크린 블러용. 분리형(separable) 5-tap 가우시안 2-pass:
    //   Pass 0: horizontal 5-tap (weights 1,4,6,4,1 / sum 16)
    //   Pass 1: vertical 5-tap + tint 적용
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color (RGBA — a로 검게 섞음)", Color) = (0,0,0,0.75)
        _BlurSize ("Blur Size (px)", Range(0, 30)) = 6
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    float _BlurSize;

    fixed4 gaussian5(float2 uv, float2 dir)
    {
        float2 t = dir * _BlurSize;
        fixed4 c = tex2D(_MainTex, uv)                * 6.0;
        c += tex2D(_MainTex, uv + t)                  * 4.0;
        c += tex2D(_MainTex, uv - t)                  * 4.0;
        c += tex2D(_MainTex, uv + t * 2.0)            * 1.0;
        c += tex2D(_MainTex, uv - t * 2.0)            * 1.0;
        return c / 16.0;
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Pass 0: horizontal
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            fixed4 frag(v2f_img i) : SV_Target
            {
                return gaussian5(i.uv, float2(_MainTex_TexelSize.x, 0));
            }
            ENDCG
        }

        // Pass 1: vertical + tint
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            float4 _Color;
            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 c = gaussian5(i.uv, float2(0, _MainTex_TexelSize.y));
                fixed3 rgb = lerp(c.rgb, _Color.rgb, _Color.a);
                return fixed4(rgb, 1.0);
            }
            ENDCG
        }
    }
}

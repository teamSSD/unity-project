Shader "Hidden/UIBlurPostProcess"
{
    // Dual Filter / Dual Kawase Blur (Marius Bjørge, ARM 2015).
    // 다운샘플 → 업샘플 반복으로 넓은 반경 가우시안 근사. UI 블러 표준.
    // Pass 0: Downsample (bilinear-friendly 5-tap)
    // Pass 1: Upsample (8-tap tent filter)
    // Pass 2: Tint 오버레이만 (마지막 pass)
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color (RGBA — a로 검게 섞음)", Color) = (0,0,0,0.75)
        _Offset ("Sample Offset", Range(0, 3)) = 1
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    float _Offset;

    fixed4 downsample(float2 uv)
    {
        float2 halfPix = _MainTex_TexelSize.xy * _Offset * 0.5;
        fixed4 c = tex2D(_MainTex, uv) * 4.0;
        c += tex2D(_MainTex, uv + float2( halfPix.x,  halfPix.y));
        c += tex2D(_MainTex, uv + float2(-halfPix.x,  halfPix.y));
        c += tex2D(_MainTex, uv + float2( halfPix.x, -halfPix.y));
        c += tex2D(_MainTex, uv + float2(-halfPix.x, -halfPix.y));
        return c / 8.0;
    }

    fixed4 upsample(float2 uv)
    {
        float2 pix = _MainTex_TexelSize.xy * _Offset;
        fixed4 c = tex2D(_MainTex, uv + float2(-pix.x * 2.0, 0));
        c += tex2D(_MainTex, uv + float2(-pix.x,  pix.y)) * 2.0;
        c += tex2D(_MainTex, uv + float2( 0, pix.y * 2.0));
        c += tex2D(_MainTex, uv + float2( pix.x,  pix.y)) * 2.0;
        c += tex2D(_MainTex, uv + float2( pix.x * 2.0, 0));
        c += tex2D(_MainTex, uv + float2( pix.x, -pix.y)) * 2.0;
        c += tex2D(_MainTex, uv + float2( 0,-pix.y * 2.0));
        c += tex2D(_MainTex, uv + float2(-pix.x, -pix.y)) * 2.0;
        return c / 12.0;
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass // 0 — Downsample
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            fixed4 frag(v2f_img i) : SV_Target { return downsample(i.uv); }
            ENDCG
        }

        Pass // 1 — Upsample
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            fixed4 frag(v2f_img i) : SV_Target { return upsample(i.uv); }
            ENDCG
        }

        Pass // 2 — Tint (마지막 pass)
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            float4 _Color;
            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                fixed3 rgb = lerp(c.rgb, _Color.rgb, _Color.a);
                return fixed4(rgb, 1.0);
            }
            ENDCG
        }
    }
}

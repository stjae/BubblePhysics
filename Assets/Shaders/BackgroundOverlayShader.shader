Shader "Custom/BackgroundOverlay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _Mask;
            sampler2D _NoiseTexture;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = float4(v.vertex.xy, 0, 1);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 bg = tex2D(_MainTex, i.uv);
                float4 mask = tex2D(_Mask, i.uv);
                float4 color = lerp(bg, float4(0, 0, 0, 0), mask.r);

                float4 noise = tex2D(_NoiseTexture, i.uv + _Time.x);
                float4 noiseBg = tex2D(_MainTex, i.uv + noise.r * 0.1);

                if (bg.a > 0 && mask.r == 1)
                    return float4(0, 0, 0, 0);
                else if (bg.a > 0 && mask.r > 0 && mask.r < 1)
                {
                    if (noiseBg.a > 0)
                        return bg;
                    else
                        return float4(bg.rgb, 1 - mask.r);
                }
                else
                    return color;
            }
            ENDCG
        }
    }
}

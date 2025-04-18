Shader "Custom/SeparableGaussianBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            Name "HorizontalBlur"
            CGPROGRAM
            #pragma exclude_renderers d3d11
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Width;
            float _Height;
            float _Radius;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            static const float weights[5] = {
                0.227027f, 0.1945946f, 0.1216216f, 0.054054f, 0.016216f
            };

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv) * weights[0];
                for (int k = 1; k < 5; k++)
                {
                    float offset = k * _Radius;
                    color += tex2D(_MainTex, i.uv + float2(1 / _Width * offset, 0)) * weights[k];
                    color += tex2D(_MainTex, i.uv - float2(1 / _Width * offset, 0)) * weights[k];
                }
                return color;
            }
            ENDCG
        }

        Pass
        {
            Name "VerticalBlur"
            CGPROGRAM
            #pragma exclude_renderers d3d11
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Width;
            float _Height;
            float _Radius;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            static const float weights[5] = {
                0.227027f, 0.1945946f, 0.1216216f, 0.054054f, 0.016216f
            };

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv) * weights[0];
                for (int k = 1; k < 5; k++)
                {
                    float offset = k * _Radius;
                    color += tex2D(_MainTex, i.uv + float2(0, 1 / _Height * offset)) * weights[k];
                    color += tex2D(_MainTex, i.uv - float2(0, 1 / _Height * offset)) * weights[k];
                }
                return color;
            }
            ENDCG
        }
    }
}

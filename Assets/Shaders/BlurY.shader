Shader "Custom/BlurY"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            CGPROGRAM
// Upgrade NOTE: excluded shader from DX11 because it uses wrong array syntax (type[size] name)
#pragma exclude_renderers d3d11
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            static const float guassianKernel[7] = {0.00038771, 0.01330373, 0.11098164, 0.22508352, 0.11098164, 0.01330373, 0.00038771};

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
            float4 _MainTex_ST;
            float _Width;
            float _Height;
            float _Radius;

            v2f vert (appdata v)
            {
                v2f o;
                // o.vertex = float4(v.vertex.xy, 0, 1);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 color;
                for(int y = 0; y < 7; y++) {
                    float2 uvOffset = float2(0, y - 3) * float2(0, 1 / _Height) * _Radius;
                    color += tex2D(_MainTex, i.uv + uvOffset) * guassianKernel[y];
                }
                return color;
            }
            ENDCG
        }
    }
}

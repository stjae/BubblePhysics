Shader "Instanced/Debug/Point"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
#pragma exclude_renderers d3d11 gles
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            uniform float Scale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // use this to access instanced properties in the fragment shader.
            };

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex * Scale);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i, uint instanceID : SV_InstanceID) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float3 color = float3(1,1,1);
                float2 offset = float2(0.5, 0.5);
                float alpha = 0.5 - length(i.uv - float2(0.5, 0.5));
                clip(alpha);

                return float4(color,1);
            }
            ENDCG
        }
    }
}
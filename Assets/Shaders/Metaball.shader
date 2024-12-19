Shader "Custom/Metaball"
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

            uniform float4 Positions[1000];
            uniform int Count;
            uniform float Scale;
            uniform float Threshold;

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
                o.vertex = UnityObjectToClipPos(v.vertex * 2.0);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i, uint instanceID : SV_InstanceID) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float3 color = float3(0,0,0);
                float influence = 0.0;
                for(int idx = 0; idx < Count; idx++)
                {
                    float2 current = Positions[instanceID] + (i.uv.xy - 0.5) * 2.0;
                    float2 other = Positions[idx];

                    float distance = length(current - other);
                    float currentInfluence = (Scale / 2) * (Scale / 2);
		            currentInfluence /= (pow(abs(current.x - other.x), 2.0) + pow(abs(current.y - other.y), 2.0));
            		influence += currentInfluence;
                    color += float3(1,1,1) * currentInfluence;
                }

                if(influence < Threshold)
                    discard;

                return float4(color,1);
            }
            ENDCG
        }
    }
}
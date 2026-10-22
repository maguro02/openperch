Shader "OpenPerch/TestToon"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _ShadeColor ("Shade Color", Color) = (0.62, 0.58, 0.72, 1)
        _ShadeThreshold ("Shade Threshold", Range(-1, 1)) = 0
        _ShadeSoftness ("Shade Softness", Range(0.001, 1)) = 0.04
        [Toggle(_ALPHATEST_ON)] _AlphaTest ("Cutout", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _OutlineWidth ("Outline Width (m)", Range(0, 0.01)) = 0.0015
        _OutlineColor ("Outline Color", Color) = (0.16, 0.12, 0.14, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        CGINCLUDE
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _MainTex_ST;
        fixed4 _Color;
        fixed _Cutoff;

        void ApplyCutout(fixed alpha)
        {
        #if defined(_ALPHATEST_ON)
            clip(alpha - _Cutoff);
        #endif
        }
        ENDCG

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap
            #pragma shader_feature_local _ALPHATEST_ON
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            fixed4 _ShadeColor;
            half _ShadeThreshold;
            half _ShadeSoftness;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                half3 vertexLight : TEXCOORD3;
                SHADOW_COORDS(4)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.vertexLight = half3(0, 0, 0);
            #if defined(VERTEXLIGHT_ON)
                o.vertexLight = Shade4PointLights(
                    unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
                    unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
                    unity_4LightAtten0, o.worldPos, o.worldNormal);
            #endif
                TRANSFER_SHADOW(o)
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
                ApplyCutout(albedo.a);

                float3 n = normalize(i.worldNormal);
                // ForwardBase passes the main directional light's direction already at unit length, and zero when the
                // scene has no directional light; normalizing that zero would turn the whole color into NaN.
                float ndl = dot(n, _WorldSpaceLightPos0.xyz);
                fixed shadow = SHADOW_ATTENUATION(i);
                half lit = smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, ndl) * shadow;
                half3 direct = lerp(_ShadeColor.rgb, half3(1, 1, 1), lit) * _LightColor0.rgb;
                half3 ambient = ShadeSH9(half4(n, 1));

                // The app takes transparency from the render target's alpha, so a visible opaque or cut-out
                // surface must write exactly 1.
                return fixed4(albedo.rgb * (direct + ambient + i.vertexLight), 1);
            }
            ENDCG
        }

        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode" = "ForwardBase" }
            Cull Front

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON

            float _OutlineWidth;
            fixed4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                // Extruding in world units keeps the outline width independent of the bone and mesh scale.
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                o.pos = UnityWorldToClipPos(worldPos + worldNormal * _OutlineWidth);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                ApplyCutout(tex2D(_MainTex, i.uv).a * _Color.a);
                return fixed4(_OutlineColor.rgb, 1);
            }
            ENDCG
        }

        Pass
        {
            Name "SHADOWCASTER"
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #pragma shader_feature_local _ALPHATEST_ON

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
                // TEXCOORD0 is taken by V2F_SHADOW_CASTER in the point-light (cube map) variant.
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                ApplyCutout(tex2D(_MainTex, i.uv).a * _Color.a);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}

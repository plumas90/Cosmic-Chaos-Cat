Shader "CosmicChaosCat/UIFistClashExplosion"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Progress ("Progress", Range(0, 1)) = 0
        _Color ("Core Color", Color) = (1, 0.9, 0.25, 1)
        _OuterColor ("Outer Color", Color) = (1, 0.12, 0.01, 1)
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t { float4 vertex : POSITION; float2 texcoord : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            float _Progress;
            fixed4 _Color;
            fixed4 _OuterColor;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * 2.0;
                float radius = length(p);
                float angle = atan2(p.y, p.x);
                float fade = saturate(1.0 - _Progress);

                float expandingRadius = lerp(0.04, 0.9, _Progress);
                float ring = 1.0 - smoothstep(0.025, 0.11, abs(radius - expandingRadius));
                float core = (1.0 - smoothstep(0.0, lerp(0.5, 0.08, _Progress), radius)) * fade;
                float rayPattern = pow(saturate(sin(angle * 11.0 + sin(angle * 7.0) * 2.0)), 10.0);
                float rays = rayPattern * smoothstep(0.08, 0.28, radius) * (1.0 - smoothstep(0.35, 1.0, radius)) * fade;
                float sparks = pow(saturate(sin(angle * 23.0 + 1.7)), 18.0) *
                               (1.0 - smoothstep(0.035, 0.12, abs(radius - expandingRadius * 0.82))) * fade;

                float intensity = saturate(core * 1.4 + ring + rays * 0.9 + sparks);
                fixed4 color = lerp(_OuterColor, _Color, saturate(core + ring * 0.45));
                color.rgb *= 1.0 + core * 1.8;
                color.a = intensity * fade * i.color.a;
                return color;
            }
            ENDCG
        }
    }
}

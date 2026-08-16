Shader "CosmicChaosCat/UIThunderCatLightning"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Progress ("Progress", Range(0, 1)) = 0
        _Seed ("Seed", Float) = 0
        _CoreColor ("Core Color", Color) = (0.92, 0.98, 1, 1)
        _GlowColor ("Glow Color", Color) = (0.15, 0.55, 1, 1)
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
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
            float _Progress, _Seed;
            fixed4 _CoreColor, _GlowColor;
            v2f vert(appdata_t v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; o.color = v.color; return o; }

            float hash(float n) { return frac(sin(n * 127.1 + _Seed * 19.19) * 43758.5453); }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * 2.0;
                float radius = length(p);
                float angle = atan2(p.y, p.x);
                const float boltCount = 14.0;
                const float twoPi = 6.28318530718;
                float sector = floor((angle + UNITY_PI) / (twoPi / boltCount));
                float centerAngle = -UNITY_PI + (sector + 0.5) * (twoPi / boltCount);
                float jitter = (hash(sector) - 0.5) * 0.28;
                float bend = sin(radius * (17.0 + hash(sector + 4.0) * 12.0) + _Seed + sector) * (0.055 + radius * 0.025);
                float angularDistance = abs(atan2(sin(angle - centerAngle - jitter - bend), cos(angle - centerAngle - jitter - bend)));
                float lineWidth = lerp(0.026, 0.006, saturate(radius));
                float bolt = 1.0 - smoothstep(lineWidth, lineWidth * 3.8, angularDistance);
                float reach = smoothstep(0.0, 0.16, radius) * (1.0 - smoothstep(0.78, 1.0, radius));
                float front = smoothstep(radius - 0.12, radius, saturate(_Progress * 1.35));
                float flicker = 0.72 + 0.28 * sin(_Time.y * 95.0 + sector * 8.3);
                float core = bolt * reach * front * flicker;
                float glow = (1.0 - smoothstep(lineWidth * 3.0, lineWidth * 12.0, angularDistance)) * reach * front * 0.55;
                float flash = (1.0 - smoothstep(0.0, 0.16 + _Progress * 0.08, radius)) * (1.0 - _Progress);
                float fade = saturate(1.0 - _Progress);
                float alpha = saturate(core + glow + flash) * fade * i.color.a;
                fixed3 rgb = _GlowColor.rgb * glow + _CoreColor.rgb * (core * 1.8 + flash);
                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}

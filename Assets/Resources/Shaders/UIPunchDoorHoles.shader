Shader "CosmicChaosCat/UIPunchDoorHoles"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _HoleTex ("Punch Hole Mask", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; float4 worldPosition : TEXCOORD1; };
            sampler2D _MainTex;
            sampler2D _HoleTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _Hole0, _Hole1, _Hole2, _Hole3, _Hole4, _Hole5, _Hole6, _Hole7, _Hole8, _Hole9;
            float4 _Hole10, _Hole11, _Hole12, _Hole13, _Hole14, _Hole15, _Hole16, _Hole17, _Hole18, _Hole19;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float InHole(float2 uv, float4 hole)
            {
                if (hole.z <= 0.0 || hole.w <= 0.0) return 0.0;
                float2 maskUV = (uv - hole.xy) / (hole.zw * 2.0) + 0.5;
                float inside = step(0.0, maskUV.x) * step(maskUV.x, 1.0) *
                               step(0.0, maskUV.y) * step(maskUV.y, 1.0);
                fixed4 maskColor = tex2D(_HoleTex, maskUV);
                // Every non-transparent source pixel cuts the door, including
                // both the filled black center and the full torn/cracked rim.
                return inside * maskColor.a;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                float cut = 0.0;
                cut = max(cut, InHole(i.texcoord, _Hole0));
                cut = max(cut, InHole(i.texcoord, _Hole1));
                cut = max(cut, InHole(i.texcoord, _Hole2));
                cut = max(cut, InHole(i.texcoord, _Hole3));
                cut = max(cut, InHole(i.texcoord, _Hole4));
                cut = max(cut, InHole(i.texcoord, _Hole5));
                cut = max(cut, InHole(i.texcoord, _Hole6));
                cut = max(cut, InHole(i.texcoord, _Hole7));
                cut = max(cut, InHole(i.texcoord, _Hole8));
                cut = max(cut, InHole(i.texcoord, _Hole9));
                cut = max(cut, InHole(i.texcoord, _Hole10));
                cut = max(cut, InHole(i.texcoord, _Hole11));
                cut = max(cut, InHole(i.texcoord, _Hole12));
                cut = max(cut, InHole(i.texcoord, _Hole13));
                cut = max(cut, InHole(i.texcoord, _Hole14));
                cut = max(cut, InHole(i.texcoord, _Hole15));
                cut = max(cut, InHole(i.texcoord, _Hole16));
                cut = max(cut, InHole(i.texcoord, _Hole17));
                cut = max(cut, InHole(i.texcoord, _Hole18));
                cut = max(cut, InHole(i.texcoord, _Hole19));
                color.a *= 1.0 - saturate(cut);
                return color;
            }
            ENDCG
        }
    }
}

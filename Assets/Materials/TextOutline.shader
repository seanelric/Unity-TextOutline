// 专用于战斗飘字的描边效果，删除了模板测试和裁剪功能
Shader "UI/TextOutline" 
{
    Properties
    {
		[PerRendererData] _MainTex("Font Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1, 1, 1, 1)
	}

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Lighting Off ZTest Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        CGINCLUDE

        fixed IsInRect(float2 pos, float2 uvMin, float2 uvMax)
        {
            pos = step(uvMin, pos) * step(pos, uvMax);
            return pos.x * pos.y;
        }

        ENDCG

        // 第一个Pass，绘制描边
        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct vertex
            {
                float4 pos : POSITION;
                fixed4 color : COLOR;
                float4 tangent : TANGENT;
                float2 texcoord : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float2 uv3 : TEXCOORD3;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float4 tangent : TANGENT;
                float4 uv : TEXCOORD0;
                float4 uvOrigin : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _TextureSampleAdd;

            // 确定每个像素周围8方向的坐标偏移
            // 可根据需要增加方向以提升效果，当然是以牺牲一定效率为代价
            static const float2 dirList[8] =
            {
                float2(-1, -1), float2(0, -1), float2(1, -1),
                float2(-1, 0), float2(1, 0),
                float2(-1, 1), float2(0, 1), float2(1, 1)
            };

            v2f vert(vertex v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.pos);
                o.color = v.color;
                o.tangent = v.tangent;
                o.uv = float4(v.texcoord, v.uv3);
                o.uvOrigin = float4(v.uv1, v.uv2);
                return o;
            }

            float SampleAlpha(float index, v2f i)
            {
                float2 pos = i.uv.xy + _MainTex_TexelSize.xy * dirList[index] * i.uv.zw;
                return IsInRect(pos, i.uvOrigin.xy, i.uvOrigin.zw) * (tex2D(_MainTex, pos) + _TextureSampleAdd).a;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = i.tangent;
                
                float alpha = SampleAlpha(0, i);
                alpha += SampleAlpha(1, i);
                alpha += SampleAlpha(2, i);
                alpha += SampleAlpha(3, i);
                alpha += SampleAlpha(4, i);
                alpha += SampleAlpha(5, i);
                alpha += SampleAlpha(6, i);
                alpha += SampleAlpha(7, i);
                color.a *= clamp(alpha, 0, 1);
                // 镂空效果
                color.a *= (1 - (tex2D(_MainTex, i.uv) + _TextureSampleAdd).a);

                return color;
            }

            ENDCG
        }

        // 第二个Pass，绘制文字内容
        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct vertex
            {
                float4 pos : POSITION;
                fixed4 color : COLOR;
                float4 texcoord : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 uvOrigin : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;

            v2f vert(vertex v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.pos);
                o.color = v.color;
                o.uv = v.texcoord;
                o.uvOrigin = float4(v.uv1, v.uv2);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = i.color;
                col.a *= IsInRect(i.uv.xy, i.uvOrigin.xy, i.uvOrigin.zw) * (tex2D(_MainTex, i.uv) + _TextureSampleAdd).a;
                return col;
            }

            ENDCG
        }
    }
}
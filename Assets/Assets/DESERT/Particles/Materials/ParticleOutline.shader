Shader "Custom/ParticleOutline"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineThickness ("Outline Thickness", Range(0, 0.1)) = 0.02
        _OutlineSoftness ("Outline Softness", Range(0, 5)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent" 
            "PreviewType" = "Plane"
            "IgnoreProjector" = "True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _OutlineColor;
            float _OutlineThickness;
            float _OutlineSoftness;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the main texture
                fixed4 mainTex = tex2D(_MainTex, i.uv);
                fixed4 col = mainTex * i.color;
                
                // Sample surrounding pixels to detect edges for outline
                float outlineAlpha = 0;
                
                // 8-direction sampling for outline detection
                float2 offsets[8] = {
                    float2(-1, 0), float2(1, 0), float2(0, -1), float2(0, 1),
                    float2(-1, -1), float2(1, -1), float2(-1, 1), float2(1, 1)
                };
                
                for (int j = 0; j < 8; j++)
                {
                    float2 sampleUV = i.uv + offsets[j] * _OutlineThickness;
                    outlineAlpha = max(outlineAlpha, tex2D(_MainTex, sampleUV).a);
                }
                
                // Create outline where there's surrounding alpha but low current alpha
                float outlineMask = outlineAlpha * (1 - mainTex.a);
                
                // Apply softness
                outlineMask = smoothstep(0, _OutlineSoftness, outlineMask);
                
                // Blend outline color with main color
                fixed4 outline = _OutlineColor;
                outline.a *= outlineMask * i.color.a;
                
                // Combine: show main texture on top, outline behind
                fixed4 result;
                result.rgb = lerp(outline.rgb, col.rgb, col.a);
                result.a = max(col.a, outline.a);
                
                return result;
            }
            ENDCG
        }
    }
    
    FallBack "Particles/Standard Unlit"
}

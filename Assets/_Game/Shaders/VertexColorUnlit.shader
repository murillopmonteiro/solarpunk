// Flat colour straight from the mesh's vertex colours, no lighting. The faceted
// sea bakes its shading per triangle, so lighting would only muddy it.
Shader "Solarpunk/VertexColorUnlit"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                // Vertex colours are authored in sRGB, like every other colour in the project.
                #ifndef UNITY_COLORSPACE_GAMMA
                o.color.rgb = GammaToLinearSpace(o.color.rgb);
                #endif
                return o;
            }

            // Set globally by SustainabilityMood: 0 is the clean lagoon, 1 a polluted murk.
            float _Gloom;
            fixed4 _GloomColor;

            fixed4 frag(v2f i) : SV_Target
            {
                // Keep the facets readable: the murk takes its brightness from the original colour.
                float luma = dot(i.color.rgb, float3(0.299, 0.587, 0.114));
                fixed3 murk = _GloomColor.rgb * (0.5 + luma);
                return fixed4(lerp(i.color.rgb, murk, _Gloom), i.color.a);
            }
            ENDCG
        }
    }
}

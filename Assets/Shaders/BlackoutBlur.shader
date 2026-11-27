// Desenfoque fuera de la luz del jugador durante el apagón. Lo usa un Full Screen Pass del Renderer2D.
Shader "Hidden/OnLinces/BlackoutBlur"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "BlackoutBlur"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _BlackoutCenter; // xy y zw: jugadores en viewport (iguales si es uno)
            float4 _BlackoutRadii;  // x: interior, y: exterior (en alturas de pantalla)
            float _BlackoutBlur;    // 0..1
            float _BlurSize;        // radio del desenfoque en alturas de pantalla

            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                half4 src = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);

                float aspect = _ScreenParams.x / _ScreenParams.y;
                float d = min(length((uv - _BlackoutCenter.xy) * float2(aspect, 1)), length((uv - _BlackoutCenter.zw) * float2(aspect, 1)));
                float mask = smoothstep(_BlackoutRadii.x, _BlackoutRadii.y, d) * _BlackoutBlur;
                if (mask <= 0.001) return src;

                // dos anillos de 8 muestras
                float2 step = float2(_BlurSize / aspect, _BlurSize) * mask;
                half4 sum = src;
                [unroll] for (int k = 0; k < 8; k++)
                {
                    float a = k * 0.785398 + 0.39;
                    float2 dir = float2(cos(a), sin(a));
                    sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + dir * step * 0.5);
                    sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + dir * step);
                }
                return sum / 17.0;
            }
            ENDHLSL
        }
    }
}

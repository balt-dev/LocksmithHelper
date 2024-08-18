#define DECLARE_TEXTURE(Name, index) \
    texture Name: register(t##index); \
    sampler Name##Sampler: register(s##index)

#define SAMPLE_TEXTURE(Name, texCoord) tex2D(Name##Sampler, texCoord)

DECLARE_TEXTURE(text, 0);

uniform float4x4 World;
uniform float Time;

#define LERPCOLOR0 float4(235.0 / 255.0, 221.0 / 255.0, 94.0 / 255.0, 1.0)
#define LERPCOLOR1 float4(229.0 / 255.0, 187.0 / 255.0, 19.0 / 255.0, 1.0)
#define LERPCOLOR2 float4(112.0 / 255.0, 73.0 / 255.0, 22.0 / 255.0, 1.0)

float4 SpritePixelShader(float2 uv : TEXCOORD0, float4 vertColor : COLOR0) : COLOR
{
    float4 color = lerp(SAMPLE_TEXTURE(text, uv), vertColor, vertColor.a);
    float uvSwitch = ceil(uv.y - 0.25);
    color *= (lerp(LERPCOLOR0, LERPCOLOR1, uv.y * 4.0) * (1.0 - uvSwitch))
            + (lerp(LERPCOLOR1, LERPCOLOR2, (uv.y - 0.25) * 1.33333333) * uvSwitch);

    color.rgb *= (sin(Time * 2.0) + 8.0) / 9.0;

    return color;
}

void SpriteVertexShader(inout float4 color    : COLOR0,
                        inout float2 texCoord : TEXCOORD0,
                        inout float4 position : SV_Position)
{
    position = mul(position, World);
}

technique Shader
{
    pass pass0
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader = compile ps_3_0 SpritePixelShader();
    }
}
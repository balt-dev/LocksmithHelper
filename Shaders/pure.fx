#define DECLARE_TEXTURE(Name, index) \
    texture Name: register(t##index); \
    sampler Name##Sampler: register(s##index)

#define SAMPLE_TEXTURE(Name, texCoord) tex2D(Name##Sampler, texCoord)

DECLARE_TEXTURE(text, 0);

uniform float4x4 World;
uniform float Time;

#define LERPCOLOR0 float4(227.0 / 255.0, 237.0 / 255.0, 240.0 / 255.0, 1.0)
#define LERPCOLOR1 float4(166.0 / 255.0, 179.0 / 255.0, 186.0 / 255.0, 1.0)


float4 SpritePixelShader(float2 uv : TEXCOORD0, float4 vertColor : COLOR0) : COLOR
{
    float4 color = lerp(SAMPLE_TEXTURE(text, uv), vertColor, vertColor.a);
    color *= lerp(LERPCOLOR0, LERPCOLOR1, uv.y);
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
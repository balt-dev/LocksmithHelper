#define DECLARE_TEXTURE(Name, index) \
    texture Name: register(t##index); \
    sampler Name##Sampler: register(s##index)

#define SAMPLE_TEXTURE(Name, texCoord) tex2D(Name##Sampler, texCoord)

DECLARE_TEXTURE(text, 0);

// From https://stackoverflow.com/a/10625698
float rand( float3 p )
{
    float3 K1 = float3(
        23.14069263277926, // e^pi (Gelfond's constant)
         2.665144142690225, // 2^sqrt(2) (Gelfondâ€“Schneider constant)
         1.258293659277129
    );
    return frac( cos( dot(p,K1) ) * 12345.6789 );
}

uniform float4x4 World;

uniform float Time;
uniform float4 MimicColor;
uniform bool Photosensitive;

float4 SpritePixelShader(float2 uv : TEXCOORD0, float4 vertColor : COLOR0) : COLOR
{
    float4 color = lerp(SAMPLE_TEXTURE(text, uv), vertColor, vertColor.a);

    float3 seed = Photosensitive ? float3(uv, Time / 20000.0) : float3(uv, Time);
    float mul = rand(seed);
    mul = lerp((mul + .5) / 1.5, (mul + 3.0) / 4.0, MimicColor.a);
    color.rgb = lerp(color.rgb, color.rgb * MimicColor.rgb, MimicColor.a);
    color.rgb *= mul;

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
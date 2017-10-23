matrix World;
matrix View;
matrix Projection;
Texture2D normalTexture;
float4 matColor;

SamplerState mySampler
{
    AddressU = WRAP;
    AddressV = WRAP;
};

bool tex, mat;

struct Vertexes
{
    float4 position : SV_Position;
    float2 uv : TEXCOORD;
};

Vertexes myVertexShader(Vertexes input)
{
    Vertexes output;
    float4 pos = mul(input.position, World);
    pos = mul(pos, View);
    pos = mul(pos, Projection);

    output.position = pos;
    output.uv = input.uv;
    return output;
}

float4 myPixelShader(Vertexes input) : SV_Target
{
    if (tex)
    {
        return normalTexture.Sample(mySampler, input.uv);
    }
    else if (mat)
    {
        return matColor;
    }
    else
    {
        return float4(1, 1, 1, 1);
    }
}

technique10 myTechnique
{
    pass myPass
    {
        SetVertexShader(CompileShader(vs_5_0, myVertexShader()));
        SetPixelShader(CompileShader(ps_5_0, myPixelShader()));
    }
}
#ifndef TOON_LIGHT_INCLUDED
#define TOON_LIGHT_INCLUDED

// Solución 4 — luz y sombras REALES de la escena, sin ningun #include nuestro.
// El pase real del target Lit ya incluye Lighting.hlsl + Shadows.hlsl (Pregraph).
// Los previews/aux que no los traen caen al fallback -> sin errores rojos.

#if defined(UNIVERSAL_LIGHTING_INCLUDED) && defined(UNIVERSAL_SHADOWS_INCLUDED)

void ToonMainLight_float(float3 WorldPos, float3 CameraPos, out float3 Direction, out float3 Color, out float Shadow)
{
    float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    Light luz = GetMainLight(shadowCoord);
    Direction = luz.direction;             // direccion real del sol de la escena
    Color = luz.color;                     // color real del sol
    Shadow = luz.shadowAttenuation;        // sombras proyectadas REALES sobre el toon
}

void ToonMainLight_half(float3 WorldPos, float3 CameraPos, out float3 Direction, out float3 Color, out float Shadow)
{
    float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    Light luz = GetMainLight(shadowCoord);
    Direction = luz.direction;
    Color = luz.color;
    Shadow = luz.shadowAttenuation;
}

#else

// Fallback (previews/contextos sin URP): luz dinamica detras de la camara
void ToonMainLight_float(float3 WorldPos, float3 CameraPos, out float3 Direction, out float3 Color, out float Shadow)
{
    float3 haciaCamara = normalize(CameraPos - WorldPos);
    Direction = normalize(lerp(haciaCamara, float3(0, 1, 0), 0.35));
    Color = float3(1.0, 0.97, 0.92);
    Shadow = 1.0;
}

void ToonMainLight_half(float3 WorldPos, float3 CameraPos, out float3 Direction, out float3 Color, out float Shadow)
{
    float3 haciaCamara = normalize(CameraPos - WorldPos);
    Direction = normalize(lerp(haciaCamara, float3(0, 1, 0), 0.35));
    Color = float3(1.0, 0.97, 0.92);
    Shadow = 1.0;
}

#endif
#endif
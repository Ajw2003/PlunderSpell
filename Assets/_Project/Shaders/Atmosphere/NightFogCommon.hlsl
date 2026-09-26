#ifndef PLUNDERSPELL_NIGHT_FOG_COMMON_INCLUDED
#define PLUNDERSPELL_NIGHT_FOG_COMMON_INCLUDED

// The castle's night fog, shared by the full-screen fog pass and by transparent shaders (flames,
// the portal) that draw after it and must fog themselves the same way.
//
// Model: height fog (densest at the ground, thinning upward) lit three ways: a flat ambient colour,
// the moon through a forward-scattering phase, and every nearby fire through a closed-form
// point-light in-scattering integral. The last is what makes fire glow as a halo in the fog, the
// look chosen in docs/plans/night-atmosphere.md. Globals are set by Plunderspell.Atmosphere.CastleAtmosphere.

#define NIGHT_FOG_MAX_LIGHTS 32

float4 _NF_FogColor;        // rgb: fog lit by the night's ambient
float4 _NF_Params;          // x: density at the base height (1/m), y: base height, z: height falloff (1/m), w: sky distance (m)
float4 _NF_MoonDir;         // xyz: direction towards the moon, w: moon phase anisotropy
float4 _NF_MoonColor;       // rgb: moon in-scatter colour and strength
float4 _NF_Scatter;         // x: fire scatter strength, y: fire phase anisotropy, z: minimum halo radius (m), w: light count
float4 _NF_LightPos[NIGHT_FOG_MAX_LIGHTS];    // xyz: position, w: range
float4 _NF_LightColor[NIGHT_FOG_MAX_LIGHTS];  // rgb: colour x intensity x visibility

float NightFogPhaseHG(float cosTheta, float g)
{
    float g2 = g * g;
    float denom = max(1.0 + g2 - 2.0 * g * cosTheta, 1e-4);
    return (1.0 - g2) / (4.0 * PI * denom * sqrt(denom));
}

float NightFogDensityAt(float height)
{
    return _NF_Params.x * exp(-_NF_Params.z * (height - _NF_Params.y));
}

// Optical depth of the height fog between the camera and a point `dist` metres along `dir`.
float NightFogOpticalDepth(float3 cameraPos, float3 dir, float dist)
{
    float k = _NF_Params.z;
    float dy = dir.y * dist;
    float startDensity = NightFogDensityAt(cameraPos.y);
    float kdy = k * dy;
    // (1 - e^-x) / x, with its limit of 1 as x -> 0.
    float shape = abs(kdy) > 1e-3 ? (1.0 - exp(-kdy)) / kdy : 1.0 - 0.5 * kdy;
    return startDensity * dist * shape;
}

// Light added along the ray by one point light, integrated in closed form:
// the integral of 1 / (h^2 + (t - t0)^2) is atan((t - t0) / h) / h.
float3 NightFogPointLight(float3 cameraPos, float3 dir, float dist, float4 lightPos, float3 lightColor)
{
    float range = lightPos.w;
    float3 toLight = lightPos.xyz - cameraPos;
    float t0 = dot(toLight, dir);
    float h2 = max(dot(toLight, toLight) - t0 * t0, 0.0);
    float h = max(sqrt(h2), _NF_Scatter.z);

    // Only the stretch of ray within the light's range counts.
    float a = max(0.0, t0 - range);
    float b = min(dist, t0 + range);
    if (a >= b)
        return 0.0;

    float integral = (atan((b - t0) / h) - atan((a - t0) / h)) / h;

    // Fade out as the ray's closest approach nears the edge of the light's reach.
    float edge = saturate(1.0 - h2 / (range * range));
    edge *= edge;

    float closest = clamp(t0, a, b);
    float3 closestPoint = cameraPos + dir * closest;
    float density = NightFogDensityAt(closestPoint.y);
    float transmittance = exp(-NightFogOpticalDepth(cameraPos, dir, closest));

    float cosTheta = t0 / max(sqrt(t0 * t0 + h * h), 1e-4);
    float phase = NightFogPhaseHG(cosTheta, _NF_Scatter.y);

    return lightColor * (density * integral * edge * phase * transmittance);
}

// Fog for a ray from the camera: rgb is the light the fog adds, a is how much of what lies behind
// survives. Composite as scene * a + rgb.
float4 NightFogAlongRay(float3 cameraPos, float3 dir, float dist)
{
    float transmittance = exp(-NightFogOpticalDepth(cameraPos, dir, dist));
    float scattered = 1.0 - transmittance;

    float3 light = _NF_FogColor.rgb * scattered;
    light += _NF_MoonColor.rgb * NightFogPhaseHG(dot(dir, _NF_MoonDir.xyz), _NF_MoonDir.w) * scattered;

    int count = (int)_NF_Scatter.w;
    float3 fire = 0.0;
    for (int i = 0; i < count; i++)
        fire += NightFogPointLight(cameraPos, dir, dist, _NF_LightPos[i], _NF_LightColor[i].rgb);
    light += fire * _NF_Scatter.x;

    return float4(light, transmittance);
}

// Fog applied to a surface a transparent shader is drawing.
float3 NightFogApply(float3 color, float3 positionWS)
{
    float3 toPoint = positionWS - _WorldSpaceCameraPos;
    float dist = length(toPoint);
    float4 fog = NightFogAlongRay(_WorldSpaceCameraPos, toPoint / max(dist, 1e-4), dist);
    return color * fog.a + fog.rgb;
}

#endif

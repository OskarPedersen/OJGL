R""(
#include "common/noise.fs"
#include "common/primitives.fs"
#include "common/raymarch_settings.fs"
#include "common/raymarch_utils.fs"
#include "common/utils.fs"

in vec2 fragCoord;
out vec4 fragColor;

uniform float iTime;
uniform vec2 iResolution;
uniform mat4 iCameraMatrix;
uniform vec3 iPlayerPosition;

const int playerType = 1;
const int groundType = 2;

DistanceInfo map(in vec3 p)
{
    DistanceInfo ground = { p.y + 1.0, groundType };
    DistanceInfo player = { sdSphere(p - iPlayerPosition, 0.3), playerType };
    return un(ground, player);
}

float getReflectiveIndex(int type)
{
    return type == groundType ? 0.2 : 0.0;
}

vec3 getColor(in MarchResult result)
{
    vec3 lightPosition = vec3(2.0, 4.0, -2.0);
    if (result.type != invalidType) {
        vec3 ambient = result.type == playerType ? vec3(0.2, 0.5, 1.0) : vec3(0.3, 0.3, 0.3);
        vec3 invLight = normalize(lightPosition - result.position);
        vec3 normal = normal(result.position);
        float shadow = shadowFunction(result.position, lightPosition, 32);
        float diffuse = max(0., dot(invLight, normal)) * shadow;
        return ambient * (0.1 + 0.9 * diffuse) * result.transmittance + result.scatteredLight;
    } else {
        return vec3(0.05, 0.05, 0.1);
    }
}

VolumetricResult evaluateLight(in vec3 p)
{
    return VolumetricResult(1000.0, vec3(0.0));
}

float getFogAmount(in vec3 p)
{
    return 0.0;
}

void main()
{
    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5) * iResolution.y / iResolution.x;
    vec3 rayOrigin = (iCameraMatrix * vec4(u, v, -1.0, 1.0)).xyz;
    vec3 eye = (iCameraMatrix * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
    vec3 rayDirection = normalize(rayOrigin - eye);

    vec3 color = march(rayOrigin, rayDirection);

    // Tone mapping
    color /= (color + vec3(1.0));

    fragColor = vec4(pow(color, vec3(0.4545)), 1.0);
}

)""

R""(
#include "common/noise.fs"
#include "common/primitives.fs"
#include "edison2022/raymarch_settings.fs"
#include "common/raymarch_utils.fs"
#include "common/utils.fs"

in vec2 fragCoord;
out vec4 fragColor;

uniform float iTime;
uniform vec2 iResolution;
uniform mat4 iCameraMatrix;

const int shipType = 1;
const int wallType = 2;


const float pi = 3.14159256;

DistanceInfo map(in vec3 p, bool isMarch)
{
    return DistanceInfo(length(p - vec3(sin(iTime), 0, 3)) - 0.1, shipType, vec3(1, 2, 3));
}

float getReflectiveIndex(int type)
{
    if(type == shipType)
        return 0.0;
    return 0.0;
}

vec3 getColor(in MarchResult result)
{
    if (result.type != invalidType) {
        vec3 col = vec3(1, 0, 0);

        vec3 invLight = -normalize(vec3(-0.7, -0.2, -0.5));
        vec3 normal = normal(result.position);
        float diffuse = max(0., dot(invLight, normal));
        return diffuse * col;
    } else {
        return vec3(1, 0, 1);
    }
}


void main()
{
    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5) * iResolution.y / iResolution.x;


    vec3 eye = vec3(0, 0, 0);
    vec3 tar =  eye + vec3(0, 0, 1);
    vec3 dir = normalize(tar - eye);
	vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	vec3 up = cross(dir, right);

    vec3 rd = normalize(dir + right*u + up*v);


    vec3 color = march(eye, rd);

    // Tone mapping
    color /= (color + vec3(1.0));

    fragColor = vec4(pow(color, vec3(0.5)), 1.0);
}

)""

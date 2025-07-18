R""(
const int invalidType = -1;

struct DistanceInfo {
    float distance;
    int type;
};

struct MarchResult {
    int type;
    vec3 position;
    int steps;
    float transmittance;
    vec3 scatteredLight;
    int jump;
    vec3 rayDirection;
};

struct VolumetricResult {
    float distance;
    vec3 color;
};

DistanceInfo map(in vec3 p);
VolumetricResult evaluateLight(in vec3 p);
float getFogAmount(in vec3 p);
vec3 getColor(in MarchResult result);
float getReflectiveIndex(int type);

vec3 normal(in vec3 p)
{
    vec3 n = vec3(map(vec3(p.x + S_normalEpsilon, p.y, p.z)).distance, map(vec3(p.x, p.y + S_normalEpsilon, p.z)).distance, map(vec3(p.x, p.y, p.z + S_normalEpsilon)).distance);
    return normalize(n - map(p).distance);
}

float shadowFunction(in vec3 hitPosition, in vec3 lightPosition, float k)
{
    float res = 1.0;

    float t = S_distanceEpsilon * 10.0;
    vec3 dir = lightPosition - hitPosition;
    float maxDistance = length(dir);
    dir = normalize(dir);
    while (t < maxDistance) {
        float h = map(hitPosition + dir * t).distance;

        if(h < S_distanceEpsilon)
            return 0.0;
        
        res = min( res, k*h/t );

        t += h;
    }
    return res;
}

DistanceInfo un(DistanceInfo a, DistanceInfo b) { return a.distance < b.distance ? a : b; }

vec3 march(in vec3 rayOrigin, in vec3 rayDirection)
{
    float t = 0.0;
    vec3 scatteredLight = vec3(0.0);
    float transmittance = 1.0;
    float reflectionModifier = 1.0;
    vec3 resultColor = vec3(0.0);

#if S_REFLECTIONS
    for (int jump = 0; jump < S_reflectionJumps; jump++) {
#else
        int jump = 0;
#endif
        for (int steps = 0; steps < S_maxSteps; ++steps) {
            vec3 p = rayOrigin + t * rayDirection;
            DistanceInfo info = map(p);
            float jumpDistance = info.distance * S_distanceMultiplier;

#if S_VOLUMETRIC
            float fogAmount = getFogAmount(p);
            VolumetricResult vr = evaluateLight(p);

            float volumetricJumpDistance = max(S_minVolumetricJumpDistance, vr.distance * S_volumetricDistanceMultiplier);
            jumpDistance = min(jumpDistance, volumetricJumpDistance);
            vec3 lightIntegrated = vr.color - vr.color * exp(-fogAmount * jumpDistance);
            lightIntegrated = max(vec3(0), lightIntegrated); // To fix the small black squares that could appear sometimes with bright volumetric light
            scatteredLight += transmittance * lightIntegrated;	
            transmittance *= exp(-fogAmount * jumpDistance);      
#endif

            t += jumpDistance;
            if (info.distance < (S_distanceEpsilon)) {
                vec3 color = getColor(MarchResult(info.type, p, steps, transmittance, scatteredLight, jump, rayDirection));
#if !S_REFLECTIONS
                return color;
#else
                t = 0.0;
                rayDirection = reflect(rayDirection, normal(p));
                rayOrigin = p + 0.1 * rayDirection;

                resultColor = mix(resultColor, color, reflectionModifier);
                reflectionModifier *= getReflectiveIndex(info.type);
                break;
 #endif
            }

            if (t > S_maxDistance || steps == S_maxSteps - 1) {
                vec3 color = getColor(MarchResult(invalidType, p, steps, transmittance, scatteredLight, jump, rayDirection));
                resultColor = mix(resultColor, color, reflectionModifier);
                return resultColor;
            }
        }
#if S_REFLECTIONS
    }
#endif

    return resultColor;
}
)""

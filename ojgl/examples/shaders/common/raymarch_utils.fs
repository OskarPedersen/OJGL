R""(
const float epsilon = 2e-2;
const float normalEpsilon = 1e-1;
const int maxSteps = 4000;
const float maxDistance = 400.0;
const int invalidType = -1;
const float minVolumetricDistanceJump = 0.01;
const float volumetricDistanceModifier = 0.25;

struct DistanceInfo {
    float distance;
    int type;
};

struct VolumetricResult {
    float distance;
    vec3 color;
};

struct MarchResult {
    int type;
    vec3 position;
    int steps;
	float transmittance;
	vec3 scatteredLight;
};

DistanceInfo map(in vec3 p);
VolumetricResult evaluateLight(in vec3 p);
float calcFogAmount(in vec3 p);

vec3 normal(in vec3 p)
{
    vec3 n = vec3(map(vec3(p.x + normalEpsilon, p.y, p.z)).distance, map(vec3(p.x, p.y + normalEpsilon, p.z)).distance, map(vec3(p.x, p.y, p.z + normalEpsilon)).distance);
    return normalize(n - map(p).distance);
}

DistanceInfo un(DistanceInfo a, DistanceInfo b) { return a.distance < b.distance ? a : b; }

MarchResult march(in vec3 rayOrigin, in vec3 rayDirection)
{
    float t = 0.0;

	vec3 scatteredLight = vec3(0.0);
	float transmittance = 1.0;

    MarchResult invalidResult = { invalidType, vec3(0.0), 0, 0, vec3(0.0) };
    for (int steps = 0; steps < maxSteps; ++steps) {
        vec3 p = rayOrigin + t * rayDirection;
        DistanceInfo info = map(p);

		float fogAmount = calcFogAmount(p);
		VolumetricResult vr = evaluateLight(p);
		float d = min(info.distance, max(minVolumetricDistanceJump, vr.distance * volumetricDistanceModifier));
		vec3 lightIntegrated = vr.color - vr.color * exp(-fogAmount * d);
		// d = min(d, max(0.01, lightColDis.w * 0.25));
        scatteredLight += transmittance * lightIntegrated;	
        transmittance *= exp(-fogAmount * d);

        t += d;

        if (info.distance < epsilon)
            return MarchResult(info.type, p, steps, transmittance, scatteredLight);
        if (t > maxDistance)
            return invalidResult;
    }
    return invalidResult;
}
)""

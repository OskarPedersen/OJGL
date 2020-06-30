R""(
#version 430
#include "common/noise.fs"
#include "common/primitives.fs"
#include "common/raymarch_utils.fs"
#include "common/utils.fs"

in vec2 fragCoord;
out vec4 fragColor;

uniform sampler2D inTexture0;

uniform float iTime;
uniform vec2 iResolution;
uniform mat4 iCameraMatrix;


const int WALL_TYPE = 3;
const int TOWER_TYPE = 5;
const int PILLAR = 7;

DistanceInfo map(in vec3 po)
{
	const float s = 10.0;
	const float rou = 0.5;
	float dwall = -sdRoundBox(po - vec3(0, s * (1.0 + rou) + 1.0, 0) + noise_3(po * (po.y > 1.0 ? 5.0 : 15.0)) * 0.03, vec3(s, s, s), s * rou);
	
	vec2 uv = po.xz;
	//float dwall = -sdRoundBox(po - vec3(0, s * (1.0 + rou) + 1.0, 0) + texture(inTexture0, uv).r, vec3(s, s, s), s * rou);
	//float dwall = -sdRoundBox(po - vec3(0, s * (1.0 + rou) + 1.0, 0), vec3(s, s, s), s * rou);
	//if (dwall < 0.03) {
	//	dwall = -sdRoundBox(po - vec3(0, s * (1.0 + rou) + 1.0, 0) + noise_3(po * (po.y > 1.0 ? 5.0 : 15.0)) * 0.03, vec3(s, s, s), s * rou);
	//}


	vec3 p = po;

	pModPolar(p.xz, 8.0);

	p.x = mod(p.x, 2.0) - 1.0;

	const float dcap = sdTorus(p - vec3(0, 1.5, 0), vec2(0.8, 0.1));

	const float d = smink(dwall, dcap, 0.3);
	int type = WALL_TYPE;
	if (dcap < dwall) {
		type = PILLAR;
	}
	DistanceInfo res = DistanceInfo(d, type);



	
	

	{
        vec3 p = po;

		const float s = 1.0;
		const float hmod = 1.0 - 0.3 *  max(0.0, p.y - s * 3.0);
		const vec3 b = vec3(s * hmod, s * 3.0, s * hmod);
		p.y -= s;
		float d = sdBox(p, b);


		{
			const float sideSize = s * 0.5;
			const float shift = s  + sideSize;
			const float apx = abs(p.x);
			const float apz = abs(p.z);
			const vec3 pa = vec3(apx, p.y, apz);
			vec3 shiftVec = vec3(-shift, -s, 0);
			float downShift = apx * 0.3;
			if (apx < apz) {
				shiftVec = shiftVec.zyx;
				downShift = apz * 0.3;
			}

			
			const float d2 = sdBox(pa + shiftVec, vec3(sideSize, (b.y * 0.67 - downShift ) * 0.7, sideSize));

			d = smink(d, d2, 0.3);

			res = un(res, DistanceInfo(d, TOWER_TYPE));
		}
	}

	

    return res;
}



float calcFogAmount(in vec3 p) {
	return 0.002;
}

VolumetricResult evaluateLight(in vec3 p) {

	float d3 = sdRoundBox(p - vec3(0, 4.0, 0), vec3(0.5), 0.2);
	float boom = mod(iTime * 5.0, 20.0);
	float d4 = length(p - vec3(0, 5.0, 0) + vec3(0, -boom, 0)) - 0.3;

	float d = smink(d3, d4, 4.);
	



	float c = pModPolar(p.xz, 8.0);
	if (abs(c + 3.0 - mod(floor(iTime), 8.0)) < 0.01) {
		p.x = mod(p.x, 2.0) - 1.0;
		float d5 = length(p - vec3(0, 1.5, 0)) - 0.1;
		d = min(d, d5);
		
	} 

	d = max(0.001, d);

	float strength = 100;// + 20 * 20 - boom * boom;
	vec3 col = vec3(1.0, 0.05, 0.05);
	vec3 res = col * strength / (d * d * d);
	return VolumetricResult(d, res);
}

float getReflectiveIndex(int type)
{
    //return type == WALL_TYPE ? 0.6 : 0.0;
	return 0.5;
}

vec3 getColor(in MarchResult res)
{
    //if (res.type != invalidType) {
		vec3 col = vec3(1, 0, 1);
		if (res.type == TOWER_TYPE) {
			col = vec3(1.0);
		} else  if (res.type == WALL_TYPE) {
			col = vec3(0.0);
		} else  if (res.type == PILLAR) {
			col = vec3(1.0, 0.2, 0.2);
		}

        vec3 invLight = -normalize(vec3(-0.7, -0.2, -0.5));
        vec3 normal = normal(res.position);
        float diffuse = max(0., dot(invLight, normal));
        return res.transmittance * col * diffuse + res.scatteredLight;
		//return res.scatteredLight;
    //} else {
	//	return vec3(1, 0, 1);
    //}
}

void main()
{
    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5) * iResolution.y / iResolution.x;
    vec3 rayOrigin = (iCameraMatrix * vec4(u, v, -1.0, 1.0)).xyz;
    vec3 eye = (iCameraMatrix * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
    vec3 rayDirection = normalize(rayOrigin - eye);

    MarchResult result = march(eye, rayDirection);
    vec3 color = getColor(result);
	vec3 firstPos = result.position;

	const float reflectiveIndex = 0.5;

	//float reflectiveIndex = getReflectiveIndex(result.type);
    //if (reflectiveIndex > 0.0 && result.type != invalidType) {
        rayDirection = reflect(rayDirection, normal(result.position));
        result = march(result.position + 0.1 * rayDirection, rayDirection);
        vec3 newColor = getColor(result);
        color = mix(color, newColor, reflectiveIndex);
	
	
		//reflectiveIndex = getReflectiveIndex(result.type);
		//if (reflectiveIndex > 0.0 && result.type != invalidType) {
			rayDirection = reflect(rayDirection, normal(result.position));
			result = march(result.position + 0.1 * rayDirection, rayDirection);
			newColor = getColor(result);
			color = mix(color, newColor, reflectiveIndex);
		//}
    //}
	

    float focus = abs(length(firstPos - eye) - 15.0) * 0.05 + 0.1;// - (8.0 - 7.0 * sin(iTime * 1.0))) * 0.1;
    focus = min(focus, 1.);

	color /= (color + vec3(1.0));
	fragColor = vec4(color, focus);

	//fragColor.rgb = vec3(1, 0, 1);
}

)""

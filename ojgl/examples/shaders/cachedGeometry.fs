R""(
#version 430

#include "shaders/cityCommon.fs"

in vec2 fragCoord;
layout(location = 0) out vec4 fragPos;
layout(location = 1) out vec4 fragNormal;
layout(location = 2) out vec4 fragPos2;
layout(location = 3) out vec4 fragNormal2;

uniform float iTime;
uniform vec2 iResolution;

#define PI 3.1415



DistanceInfo un(DistanceInfo res1, DistanceInfo res2) {
	if (res1.distance < res2.distance) {
		return res1;
	} else {
		return res2;
	}
}

void moda(inout vec2 p, float rep)
{
    float per = 2.*PI/rep;
    float a = atan(p.y, p.x);
    float l = length(p);
    a = mod(a-per*0.5,per)-per*0.5;
    p = vec2(cos(a),sin(a))*l;  
}

float udRoundBox( vec3 p, vec3 b) {
  float r = 0.5;
  return length(max(abs(p)-b,0.0))-r;
}

float sdBox( vec3 p, vec3 b )
{
  vec3 q = abs(p) - b;
  return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

mat2 rot(float a)
{
    return mat2(cos(a),sin(a),-sin(a),cos(a));
}

DistanceInfo city(vec3 p) {
	float s = 3.0;
	float qx = mod(p.x + s * 0.5, s) - s * 0.5;
	float qz = mod(p.z + s * 0.5, s) - s * 0.5;
	vec3 q = vec3(qx, p.y, qz);
	return DistanceInfo(sdBox(q, vec3(0.5, 8.0, 0.5)), MAT_HOUSE);
}

DistanceInfo scene(vec3 p) {
	vec3 t = p - vec3(0, 0, 50);
	float dBase = sdBox(t, vec3(20, 4, 5));
	float d = dBase;

	float dTower = sdBox(t - vec3(0, 0, 2), vec3(5, 50, 2));
	d = min(d, dTower);

	{
		vec3 q = t;
		float s = 5.0;
		q.x = mod(q.x + s/2, s) - s/2;
		float dPillars = sdBox(q, vec3(1, 10, 1));
		float dLight = length(q - vec3(0, 8, -1)) - 0.5; //sdBox(q - vec3(0, 8, -1), vec3(0.5, 0.5, 0.5));
		float dRes = max(dPillars, -dLight);
		d = min(d, dRes);
	}

	return DistanceInfo(d, MAT_SCENE);
}

DistanceInfo map(vec3 p) {
	DistanceInfo res = DistanceInfo(p.y, MAT_FLOOR);
	res = un(res, city(p));
	//res = un(res, scene(p));
	return res;
}


vec3 calcNormal(vec3 p) {
	const vec3 ep = vec3(0.01, 0, 0);
	vec3 normal;
	normal.x = map(p + ep.xyz).distance - map(p - ep.xyz).distance;
	normal.y = map(p + ep.yxz).distance - map(p - ep.yxz).distance;
	normal.z = map(p + ep.yzx).distance - map(p - ep.yzx).distance;
	normal = normalize(normal);

	return normal;
}

void main(){
	const vec2 uv = fragCoord.xy;
    vec3 ro = RO;
    vec3 rd = normalize(vec3(uv.x - 0.5, uv.y - 0.5 - 0.25, 1.0));
	// TODO: proper target etc

	const float distThresh = 0.01;
    for(int j = 0; j < 2; j++){
		float t = 0.0;
		for (int i = 0; i < 1000; i++) {
    		vec3 p = ro + rd * t;
			DistanceInfo di = map(p);
			float d = di.distance;
        
			if (d < distThresh) {
				if (j == 0) {
					vec3 normal = calcNormal(p);	
            
					fragNormal.rgb = normal;
					fragPos.rgb = p;
					fragPos.a = di.type;

					rd = reflect(rd, normal);
					ro = p + rd * distThresh * 2.0;
				} else if (j == 1) {
					fragNormal2.rgb = calcNormal(p);
					fragPos2.rgb = p;
					fragPos2.a = di.type;
				}
				break;
			}
			t += d;
		}    
	}

	fragNormal.a = 1.0;

	fragNormal2.a = 1.0;
}

)""
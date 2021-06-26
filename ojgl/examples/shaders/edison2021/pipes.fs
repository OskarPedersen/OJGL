R""(
#include "edison2021/tunnel_base.fs"

VolumetricResult evaluateLight(in vec3 p)
{
 
     // Star
    const float distance = 40.0;
    vec3 starP = p;
    starP.z = mod(starP.z +  iTime * 20, distance) - distance * 0.5;
    starP = (starP - vec3(tunnelDelta(p.z).x, 0, 0)).xzy;

    float r = 1.1 + sin(10*atan(starP.z, starP.x) + iTime * 25.0) * 0.2 + sin(iTime * 10.0) * 0.2;
    r *= 0.5;

    DistanceInfo res = {sdTorus(starP, vec2(r, 0.0)), starType};

    { // Pipes
        const float spinFrequency = 1.0;
        float pipeAdis = sdCylinder(p - vec3(tunnelDelta(p.z).x + sin(p.z * spinFrequency)        * 0.2, 0.0 +  cos(p.z * spinFrequency)        * 0.2, 0), 0.0);
        float pipeBdis = sdCylinder(p - vec3(tunnelDelta(p.z).x + sin(p.z * spinFrequency + 3.14) * 0.2, 0.0 +  cos(p.z * spinFrequency + 3.14) * 0.2, 0), 0.0);
        res = un(res, DistanceInfo(pipeAdis, pipesAType));
        res = un(res, DistanceInfo(pipeBdis, pipesBType));
    }


    float d = max(0.001, res.distance);
    vec3 col = vec3(0);
	float strength = 10;
    if (res.type == starType) {
        col = vec3(0.1, 0.1, 1.0);
        strength = 100;
    } else if (res.type == pipesAType) {
        col = vec3(1.0, 0.5, 0.1);
    } else if (res.type == pipesBType) {
        col = vec3(1.0, 1.0, 1.0);
    }

	vec3 res2 = col * strength / (d * d);
	return VolumetricResult(d, res2);
}

float getFogAmount(in vec3 p) 
{
    return 0.0005;
}

DistanceInfo map(in vec3 p)
{
    vec3 p2 = p.xzy - tunnelDelta(p.z);
    DistanceInfo cylinder = {-sdCappedCylinder(p.xzy - tunnelDelta(p.z) - vec3(0, 0, 0.02 * mod(p.z * 6.0, 1.0)), vec2(1 + 0.0*filteredLines(10*atan(p.y), 1.1) + 0.1*filteredLines(5*p2.x, 1.1), 50000)), wallType };
    DistanceInfo floorBox = {-sdBox(p - tunnelDelta(p.z) + vec3(0, -1.2, 0.0), vec3(3, 2.0 + 0.0006*sin(7*p.x + 5*p.y + 5*p.z), 50000)), floorType };
   
	DistanceInfo res =  cylinder;

    return res;
}


)""
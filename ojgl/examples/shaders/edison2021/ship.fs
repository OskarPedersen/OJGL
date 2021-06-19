R""(
#include "edison2021/tunnel_base.fs"

DistanceInfo map(in vec3 p)
{
    DistanceInfo cylinder = {-sdCappedCylinder(p.xzy - tunnelDelta(p.z), vec2(2 + 0.1*filteredLines(10*atan(p.y), 1.1) , 50000)), wallType };
    vec3 e = vec3(0, smoothstep(0, 5, abs(p.z - path(-5.5).z)) * (smoothstep(0, 1, abs(p.x  - tunnelDelta(p.z).x)) - 1.0), 0);
    DistanceInfo floorBox = {-sdBox(p - tunnelDelta(p.z) + vec3(0, -1.2, 0.0) - e, vec3(3, 2.0 + 0.0006*sin(7*p.x + 5*p.y + 5*p.z), 50000)), floorType };
    DistanceInfo res = sunk(cylinder, floorBox, 0.3);

    {

        float d = sdVerticalCapsule(p - path(-5.5) - vec3(0.0, -1.0, 0.0), 1.0, 0.1);
        res = sunk(res, DistanceInfo(d, pipesAType), 0.1);
    }


    return res;
}


)""

R""(


in vec2 fragCoord;
out vec4 fragColor;

uniform float iTime;

uniform vec2 iResolution;
uniform mat4 iCameraMatrix;
uniform sampler2D borgilaTexture;
uniform sampler2D inTexture0;
uniform sampler2D inTexture1;

uniform float C_1_S; // bass
uniform float C_6_S; // "vocals"
uniform float C_7_S; // "synth"

uniform float C_7_S_0;
uniform float C_7_S_1;
uniform float C_7_S_2;
uniform float C_7_S_3;

uniform float C_7_T; // "synth"

uniform float C_3_S; // "vocals"

uniform float scenePart;

bool willHitText = false;

const int ufoType = 5;
const int mountainType = 6;
const int runwayType = 7;
const int hangarType = 8;
const int doorsType = 9;
const int boatType = 10;

vec3 cameraPosition;
vec3 rayDirection;
vec3 firstRayDirection;

float hangarBox(in vec3 p);
float runwayBox(in vec3 p);

float ufoSpeed = 10.0;

const float ufoPD1 = 3;
const float ufoPD2 = 4;
const float ufoPD3 = 5;

const float part2flybyEndTime = 0; //7;

const float doorOpenTimePart2 = 2;
const float waitForLaserTime = 4;
const float laserPeakTime = 2.5 + part2flybyEndTime;

const float camera1 = ufoPD1 + ufoPD2 - 1;
const float camera2 = camera1 + ufoPD3 - 2;


float boatSplit(vec3 p, float dir);

float shadowFunction(in vec3 hitPosition, int type)
{
    if (scenePart != 2.0 || type != runwayType) {
        return 1.0;
    }
    float res = 1.0;
    float k = 7.0;
    float t = S_distanceEpsilon * 20.0;
    vec3 dir = vec3(0, 1, 0);
    float maxDistance = 2;
    while (t < maxDistance) {
        float h = boatSplit(hitPosition + dir * t, 1.0);

        if(h < S_distanceEpsilon * 10)
            return 0.0;
        
        res = min( res, k*h/t );

        t += max(0.5, h);
    }
    return res;
}

bool ufoV() {
    return scenePart == 1.0 || (scenePart == 2.0 && iTime < (laserPeakTime + doorOpenTimePart2 + waitForLaserTime));
}

vec3 ufoR(in vec3 p) {
    if (scenePart == 2.0) {
        return p;
    }
    float rotEnd = ufoPD1 + 0.5;
    if (iTime < rotEnd) {
        float s = 1.0 - smoothstep(rotEnd - 1.0, rotEnd, iTime);
        p.yz *= rot(sin(iTime * 1.5) * 0.1 * s);

        p.xy *= rot(0.3 * s);
    }
    return p;
}

vec3 ufoP()
{
    vec3 endPos = vec3(40, 0, 30);

    if (scenePart == 2.0) {
        return endPos;
    }

    float t = iTime;


    if (t < ufoPD1) {
        return mix(vec3(-150, 30, 0), vec3(-40, 2, 0), (t) / ufoPD1);
    } else if (t < ufoPD1 + ufoPD2) {
     return mix(vec3(-40, 2, 0), vec3(40, 1, 0), (t - ufoPD1) / ufoPD2);
    } else if (t < ufoPD1 + ufoPD2 + ufoPD3) {
     return mix(vec3(40, 1, 0), endPos, smoothstep(0, 1, (t - ufoPD1 - ufoPD2) / ufoPD3));
    }


    return endPos;
}

vec3 bp() {
    if (iTime < part2flybyEndTime) {
        return vec3(0, 100, 0);
    } else {
        float start = doorOpenTimePart2 * 0.5;
        float s = smoothstep(doorOpenTimePart2, doorOpenTimePart2 + waitForLaserTime - 0.5, iTime);

        float t = iTime * 3.0;
        

        //return vec3(10, 0, 33.75*s) + vec3(, , );
        return vec3(10, 0, 33.75*s) + vec3(0.05 * sin(t), 0.1 * sin(t + 3), 0.1 * sin(t + 5));
    }
}



)""

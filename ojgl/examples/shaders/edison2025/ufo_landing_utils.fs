R""(


in vec2 fragCoord;
out vec4 fragColor;

uniform float iTime;

uniform vec2 iResolution;
uniform mat4 iCameraMatrix;
uniform sampler2D borgilaTexture;
uniform sampler2D inTexture0;

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

const float ufoPosD1 = 3;
const float ufoPosD2 = 4;
const float ufoPosD3 = 5;

const float part2flybyEndTime = 0; //7;

const float doorOpenTimePart2 = 2;
const float waitForLaserTime = 4;
const float laserPeakTime = 2.5 + part2flybyEndTime;

const float camera1 = ufoPosD1 + ufoPosD2 - 1;
const float camera2 = camera1 + ufoPosD3 - 2;


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


)""

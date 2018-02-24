R""(
#version 430

in vec2 fragCoord;
out vec4 fragColor;

uniform float iGlobalTime;
uniform sampler2D inTexture0;
uniform sampler2D inTexture1;
uniform sampler2D inTexture2;

uniform float CHANNEL_0_TIME_SINCE[41];
uniform float CHANNEL_1_TIME_SINCE[23];
uniform float CHANNEL_11_TIME_SINCE[28];
uniform float CHANNEL_12_TIME_SINCE[2];
uniform float CHANNEL_13_TIME_SINCE[1];

uniform float CHANNEL_0_TIME_TO[41];
uniform float CHANNEL_1_TIME_TO[23];
uniform float CHANNEL_11_TIME_TO[28];
uniform float CHANNEL_12_TIME_TO[2];
uniform float CHANNEL_13_TIME_TO[1];

uniform float CHANNEL_12_TOTAL;
uniform float CHANNEL_13_TOTAL;

//////////////////////////////////////////////////////

#define REFLECTION
//#define REFRACTION // TODO: I don't think this works perfectly.

#define VOLUMETRIC_LIGHTNING

#define SHADOWS

#define TONE_MAPPING

#define MAT_MIRROR 1.0
#define MAT_BOX 2.0
#define MAT_ROOM 3.0

vec2 un(vec2 a, vec2 b)
{
	return a.x < b.x ? a : b;
}

float sdBox( vec3 p, vec3 b )
{
  vec3 d = abs(p) - b;
  return min(max(d.x,max(d.y,d.z)),0.0) + length(max(d,0.0));
}

float sdCylinder( vec3 p, float r )
{
  return length(p.xz)-r;
}

float udRoundBox( vec3 p, vec3 b, float r )
{
  return length(max(abs(p)-b,0.0))-r;
}

float sdCappedCylinder( vec3 p, vec2 h )
{
  vec2 d = abs(vec2(length(p.xz),p.y)) - h;
  return min(max(d.x,d.y),0.0) + length(max(d,0.0));
}


float specular(vec3 normal, vec3 light, vec3 viewdir, float s)
{
	float nrm = (s + 8.0) / (3.1415 * 8.0);
	float k = max(0.0, dot(viewdir, reflect(light, normal)));
    return  pow(k, s);
}

vec2 map(vec3 p, vec3 rd) 
{
	vec2 res = vec2(-sdBox(p - vec3(0, 3.0, 0), vec3(5.0, 5.0, 10.0)), MAT_ROOM);
	res = un(res, vec2(sdBox(p - vec3(0, 0, 3.0), vec3(1.0, 2.0, 0.1)), MAT_MIRROR));
	res = un(res, vec2(udRoundBox(p - vec3(0,-0.5, 0), vec3(0.4), 0.1), MAT_BOX));
	res = un(res, vec2(udRoundBox(p - vec3(0, 1.0, 0), vec3(0.25), 0.05), MAT_BOX));
	return res;
}

vec3 lightAModifyPos(vec3 p)
{
	float size = 4.0;
	p.z = mod(p.z, size) - size * 0.5;
	return p - vec3(2.0 + 3.0 * sin(iGlobalTime), 0, 0);
}

vec4 lightA(vec3 p)
{
	float dis = length(p);
	vec3 col = vec3(1.0, 0.0, 0.0);
	const float strength = 3.0;
	vec3 res = col * strength / (dis * dis * dis);
	return vec4(res, dis);
}

vec3 lightBModifyPos(vec3 p)
{
	return p - vec3(-1.0, 1.0 + 2.0 * sin(iGlobalTime), 0.0);
}

vec4 lightB(vec3 p)
{
	float dis = length(p);
	vec3 col = vec3(0.0, 0.0, 1.0);
	const float strength = 10.0;
	vec3 res = col * strength / (dis * dis * dis);
	return vec4(res, dis);
}

vec4 lightUnion(vec4 a, vec4 b)
{
	return vec4(a.rgb + b.rgb, min(a.w, b.w));
}

vec4 evaluateLight(vec3 pos)
{
	vec4 res = lightA(lightAModifyPos(pos));
	res = lightUnion(res, lightB(lightBModifyPos(pos)));
	return res;
}

#ifdef SHADOWS
float shadowFunction(in vec3 ro, in vec3 rd, float mint, float maxt)
{
    float t = 0.1;
    for(float _ = 0.0; _ == 0.0; _ += 0.0)
    {
        if (t >= maxt) {
        	return 1.0;
        }
        float h = map(ro + rd*t, rd).x;
        if( h<0.01 )
            return 0.0;
        t += h;
    }
    return 1.0;
}
#else
#define shadowFunction(ro, rd, mint, maxt) 1.0
#endif

void addLight(inout vec3 diffRes, inout float specRes, vec3 normal, vec3 eye, vec3 lightPos, vec3 lightCol, float shadow, vec3 pos)
{
	vec3 col = vec3(0.0);
	vec3 invLight = normalize(lightPos - pos);
	float diffuse = max(0.0, dot(invLight, normal));
	float spec = specular(normal, -invLight, normalize(eye - pos), 80.0);
	float dis = length(lightPos);
	float str = 1.0/(0.5 + 0.01*dis + 0.1*dis*dis); 
	diffRes += diffuse * lightCol * str * shadow;
	specRes += spec * str * shadow;
}

void addLightning(inout vec3 color, vec3 normal, vec3 eye, vec3 pos) {
	vec3 diffuse = vec3(0.0);
	float specular = 0.0;
	const float ambient = 0.3;

	{
		// Lights without shadow
		vec3 posLightOrigo = lightAModifyPos(pos);
		addLight(diffuse, specular, normal, eye, pos-posLightOrigo, lightA(posLightOrigo).rgb, 1.0, pos);
	}
	{	
		// Light with shadow
		vec3 posLightOrigo = lightBModifyPos(pos);
		float shadow = shadowFunction(pos, normalize(-posLightOrigo), 0.1, length(posLightOrigo));
		//if (shadow != 0.0) // TODO: Test if this gives better performance
		addLight(diffuse, specular, normal, eye, pos-posLightOrigo, lightB(posLightOrigo).rgb, shadow, pos);
	}
	color = color * (ambient + diffuse) + specular;
}

vec3 getNormal(vec3 p, vec3 rd)
{
	vec3 normal;
    vec3 ep = vec3(0.01, 0, 0);
    normal.x = map(p + ep.xyz, rd).x - map(p - ep.xyz, rd).x;
    normal.y = map(p + ep.yxz, rd).x - map(p - ep.yxz, rd).x;
    normal.z = map(p + ep.yzx, rd).x - map(p - ep.yzx, rd).x;
    return normalize(normal);
}

float occlusion(vec3 p, vec3 normal, vec3 rd)
{
	float o = clamp(2*map(p + normal * 0.5, rd).x, 0, 1);
	return 0.8 + 0.2*o;
}


vec3 raymarch(vec3 ro, vec3 rd, vec3 eye) 
{
	const int maxIter = 90;
	const float maxDis = 200.0;
	const int jumps = 4;

	vec3 col = vec3(0);	
	float ref = 1.0;

	vec3 scatteredLight = vec3(0.0);
	float transmittance = 1.0;
	for (int j = 0; j < jumps; j++) {
		float t = 0.0;
		for (int i = 0; i < maxIter; i++) {
			vec3 p = ro + rd * t;
			vec2 res = map(p, rd);
			float d = res.x;
			float m = res.y;
#ifdef VOLUMETRIC_LIGHTNING
			float fogAmount = 0.005;
			vec4 lightColDis = evaluateLight(p);
			vec3 light = lightColDis.rgb;
			d = min(d, lightColDis.w);

			vec3 lightIntegrated = light - light * exp(-fogAmount * d);
			scatteredLight += transmittance * lightIntegrated;	
			transmittance *= exp(-fogAmount * d);
#endif
			t += d;		
			bool end = i == maxIter - 1 || t > maxDis;
			if (d < 0.01 || end) {
				vec3 c = vec3(1, 0, 1);
				vec3 normal = getNormal(p, rd);

				if (m == MAT_MIRROR) {
					c = vec3(0.0);
				} else if (m == MAT_BOX) {
					c = vec3(1.0, 0.0, 0.0);
				} else if (m == MAT_ROOM) {
					c = vec3(0.5);
				}

				c *= occlusion(p, normal, rd);
				addLightning(c, normal, eye, p);
				
				if (end) {
					transmittance = 0;
				}
				col = mix(col, transmittance * c + scatteredLight, ref);

				if (m == MAT_ROOM) {
					return col;
				} else if (m == MAT_MIRROR) {
					ref *= 0.9;
				} else if (m == MAT_BOX) {
					ref *= 0.5;
				}

#ifdef REFLECTION
				rd = reflect(rd, getNormal(p, rd));
#endif
#ifdef REFRACTION
				rd = refract(rd, getNormal(p, rd), 1/1.2);
#endif
				ro = p + rd*0.5;
				t = 0;
				break;
			}
		}
	}
	return col;
}

void main()
{
    float u = fragCoord.x * 2.0 - 1.0;
	float v = fragCoord.y * 2.0 - 1.0;

    vec3 eye = vec3(2 * sin(iGlobalTime), 1, 2 * cos(iGlobalTime));
	vec3 tar = vec3(0 ,0, 0); 

	vec3 dir = normalize(tar - eye);
	vec3 right = normalize(cross(vec3(0, 1, 0), dir));
	vec3 up = cross(dir, right);

	vec3 ro = eye;
	vec3 rd = normalize(dir + right*u + up*v);
	
	vec3 color = raymarch(ro, rd, eye);
#ifdef TONE_MAPPING
	color /= (color + vec3(1.0));
#endif
    fragColor = vec4(color, 1.0);
} 

)""  
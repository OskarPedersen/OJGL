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



#define MAT_GRAVE 1.0
#define MAT_GROUND 2.0
#define MAT_PATH 3.0
#define MAT_POLE 4.0

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


float hash( in vec2 p ) {
	float h = dot(p,vec2(127.1,311.7));	
    return fract(sin(h)*43758.5453123);
}

float noise( in vec2 p ) {
    vec2 i = floor( p );
    vec2 f = fract( p );	
	vec2 u = f*f*(3.0-2.0*f);
    return mix( mix( hash( i + vec2(0.0,0.0) ), 
                     hash( i + vec2(1.0,0.0) ), u.x),
                mix( hash( i + vec2(0.0,1.0) ), 
                     hash( i + vec2(1.0,1.0) ), u.x), u.y);
}

float noiseOctave(in vec2 p, int octaves, float persistence)
{
	float n = 0.;
	float amplitude = 1.;
	float frequency = 1.;
	float maxValue = 0.;

	for(int i = 0; i < octaves; i++)
	{
		n += noise((p+float(i)) * frequency) * amplitude;
		maxValue += amplitude;
		amplitude *= persistence;
		frequency *= 2.0;
	}
	return n / maxValue; 
}

float HexagonalGrid (in vec2 position         
	                ,in float gridSize
	                ,in float gridThickness) 
{
  vec2 pos = position / gridSize; 
  pos.x *= 0.57735 * 2.0;
  pos.y += 0.5 * mod(floor(pos.x), 2.0);
  pos = abs(fract(pos) - 0.5);
  float d = abs(max(pos.x*1.5 + pos.y, pos.y*2.0) - 1.0);
  return smoothstep(0.0, gridThickness, d);
}

float pathPattern(in vec2 p) {
	return HexagonalGrid(p, 0.3, 0.1);
}


float specular(vec3 normal, vec3 light, vec3 viewdir, float s)
{
	float nrm = (s + 8.0) / (3.1415 * 8.0);
	float k = max(0.0, dot(viewdir, reflect(light, normal)));
    return  pow(k, s);
}

#define LIGHT_WIDTH 1.5
#define LIGHT_SPACING 3.0
#define LIGHT_HEIGHT 2.0

vec2 map(vec3 p, vec3 rd) 
{
	const float pathWidth = 1.5;
	vec2 res = vec2(99999999, 0);
	if (abs(p.z) > pathWidth) {
		float s = 2.0;
		vec3 q = mod(p + s*0.5, s) - s * 0.5;
		q.y = p.y;
		float d = sdBox(q - vec3(0, 0.5, 0), vec3(0.1, 0.5, 0.05));
		float d2 = sdBox(q - vec3(0, 0.7, 0), vec3(0.4, 0.1, 0.05));
		res = vec2(min(d, d2), MAT_GRAVE);
	}

	{
		float d = p.y;
		if (abs(p.z) > pathWidth) {
			 d -= 0.1*noiseOctave(p.xz*10.0, 3, 0.7);
			 res = un(res, vec2(d, MAT_GROUND));
		} else {
			d -= 0.02*pathPattern(p.xz);
			res = un(res, vec2(d, MAT_PATH));
		}
		
	}

	{
		vec3 q = p;
		q.z = abs(p.z) - LIGHT_WIDTH;
		float s = LIGHT_SPACING;
		q.x = mod(p.x + s * 0.5, s) - s * 0.5;
		float d = sdCappedCylinder(q, vec2(0.1, LIGHT_HEIGHT));
		res = un(res, vec2(d, MAT_POLE));
	
	}
	return res;
}

vec3 lightAModifyPos(vec3 p)
{
	float size = 3.0;
	//p.x = mod(p.x + size * 0.5, size) - size * 0.5;
	//p.z = mod(p.z + size * 0.5, size) - size * 0.5;
	return p - vec3(2.0, 2.0, 2.0);
}

vec4 lightA(vec3 p)
{
	float dis = length(p);
	vec3 col = vec3(1.0);
	const float strength = 50.0;
	vec3 res = col * strength / (dis * dis * dis);
	return vec4(res, dis);
}


vec3 lightPolesModifyPos(vec3 p) {
	p.z = abs(p.z) - LIGHT_WIDTH;
	p.y -= LIGHT_HEIGHT + 0.2;
	p.x = mod(p.x + LIGHT_SPACING * 0.5, LIGHT_SPACING) - LIGHT_SPACING * 0.5;
	return p;
}

vec4 lightPoles(vec3 p) {
	float dis = length(p);
	vec3 col = vec3(1.0, 0, 1.0);
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
	//res = lightUnion(res, lightB(lightBModifyPos(pos)));
	res = lightUnion(res, lightPoles(lightPolesModifyPos(pos)));
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

void addLight(inout vec3 diffRes, inout float specRes, vec3 normal, vec3 eye, vec3 lightPos, vec3 lightCol, float shadow, vec3 pos, float matSpec)
{
	vec3 col = vec3(0.0);
	vec3 invLight = normalize(lightPos - pos);
	float diffuse = max(0.0, dot(invLight, normal));
	float spec = specular(normal, -invLight, normalize(eye - pos), 80.0);
	//float dis = length(lightPos);
	float dis = length(lightPos - pos);
	//float str = 1.0/(0.5 + 0.01*dis + 0.1*dis*dis); 
	//diffRes += diffuse * lightCol * str * shadow;
	//specRes += spec * str * shadow;

	diffRes += diffuse * lightCol * shadow;
	specRes += spec  *  shadow  * 1.0 * length(lightCol) * matSpec;
}

void addLightning(inout vec3 color, vec3 normal, vec3 eye, vec3 pos, float mat) {
	vec3 diffuse = vec3(0.0);
	float specular = 0.0;
	const float ambient = 0.0;
	float matSpec = 1.0;
	if (mat == MAT_GROUND) {
		matSpec = 0.0;
	}

	{
		vec3 posLightOrigo = lightAModifyPos(pos);
		addLight(diffuse, specular, normal, eye, pos-posLightOrigo, lightA(posLightOrigo).rgb, 1.0, pos, matSpec);
	}
	{
		vec3 posLightOrigo = lightPolesModifyPos(pos);
		addLight(diffuse, specular, normal, eye, pos-posLightOrigo, lightPoles(posLightOrigo).rgb, 1.0, pos, matSpec);
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

				if (m == MAT_GRAVE) {
					c = vec3(1.0);
				} else if (m == MAT_GROUND) {
					
					c = vec3(0, abs(p.y), 0);
				} else if (m == MAT_PATH) {
					float n = noiseOctave(p.xz, 10, 0.8);
					float p = pathPattern(p.xz);
					vec3 brick = vec3(0.05, 0.1, 0.3);
					vec3 mortar = vec3(0.5);
					c = mix(mortar, brick, p);
					c *= n;
				} else if (m == MAT_POLE) {
					c = vec3(1,0,0);
				}

				c *= occlusion(p, normal, rd);
				addLightning(c, normal, eye, p, m);
				
				if (end) {
					transmittance = 0;
				}
				col = mix(col, transmittance * c + scatteredLight, ref);

				if (m == -1) {
					ref *= 0.5;
				} else {
					return col;
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
	u *= 16.0 / 9.0;

    vec3 eye = vec3(3 * sin(iGlobalTime), 2, 3 * cos(iGlobalTime));
	vec3 tar = vec3(0 ,1, 0); 

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
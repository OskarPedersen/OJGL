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
#define MAT_WATER 5.0

// Smooth min. k determines smoothness
float smink( float a, float b, float k )
{
    float h = clamp( 0.5+0.5*(b-a)/k, 0.0, 1.0 );
    return mix( b, a, h ) - k*h*(1.0-h);
}


vec2 un(vec2 a, vec2 b)
{
	return a.x < b.x ? a : b;
}

// Union with smink
vec2 sunk(vec2 a, vec2 b, float k)
{
	float sm = smink(a.x,b.x, k);
	float m = min(a.x, b.x);
	float ca = abs(sm -a.x);
	float cb = abs(sm -b.x);
	return ca < cb ? vec2(sm, a.y) : vec2(sm, b.y);
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

vec2 water(vec3 p, vec3 rd)
{
	if (rd.y > 0) {
		return vec2(99999, MAT_WATER);
	}
	//p -= - 3.5;
	p.y += 0.5;
	float t = iGlobalTime * 1.5;

	float d = (sin(p.x + t*0.5) + sin(p.z  + t*0.5)) * 0.1 +
		noiseOctave(p.xz*10.0 + vec2(0, t*0.1), 3, 0.7)*0.1 +
		noiseOctave(p.xz*10.0 + vec2(t*0.13, 0), 3, 0.7)*0.1;
			//length(texture(inTexture0, p.xz*0.8 + vec2(0, t*0.1)))*0.1 +
			//length(texture(inTexture0, p.xz*0.8 + vec2(t*0.13, 0)))*0.1;
	d *= 0.5;

	float h = p.y - d * 0.1;

	float dis = (0.1 -p.y)/rd.y;

	return vec2(max(h, dis), MAT_WATER);
}

#define LIGHT_WIDTH(p) (1.5) 
#define LIGHT_SPACING 3.0
#define LIGHT_HEIGHT 1.8

vec2 map(vec3 p, vec3 rd) 
{
	const float pathWidth = 1.5;
	vec2 res = vec2(99999999, 0);

	{
		float d = p.y;


		d -= 0.08*noiseOctave(p.xz*10.0, 3, 0.7);


		
		res = un(res, vec2(d, MAT_GROUND)); 
		
	}
	{
		
		//res = un(res, water(p, rd));
	}

	//if (abs(p.z) > pathWidth) {
	//if (p.x > 0.5){	
		float s = 2.0;
		vec3 q = mod(vec3(p.x, p.y, p.z) + s*0.5, s) - s * 0.5;
		q.y = p.y;
		float d = sdBox(q - vec3(0, 0.7, 0), vec3(0.1, 0.5, 0.02));
		float d2 = sdBox(q - vec3(0, 0.85, 0), vec3(0.4, 0.1, 0.02));
		res = sunk(vec2(min(d, d2) + max(0.0, 1.0 - abs(p.z)), MAT_GRAVE), res, 0.5);
	//}
	//}

	

	{
		vec3 q = p;
		q.z = abs(p.z) - LIGHT_WIDTH(p);
		float s = LIGHT_SPACING;
		q.x = mod(p.x + s * 0.5, s) - s * 0.5;
		float w = 0.02;// + max(0.0, -p.y*0.2 + 0.1);
		float d = sdCappedCylinder(q, vec2(w, LIGHT_HEIGHT));
		res = sunk(vec2(d, MAT_POLE), res, 0.5);
	
	}
	return res;
}

vec3 lightAModifyPos(vec3 p)
{
	//float size = 3.0;
	//p.x = mod(p.x + size * 0.5, size) - size * 0.5;
	//p.z = mod(p.z + size * 0.5, size) - size * 0.5;
	return p - vec3(-20.0, 4.0, 10.0);
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
	p.z = abs(p.z) - LIGHT_WIDTH(p);
	p.y -= LIGHT_HEIGHT + 0.1;
	p.x = mod(p.x + LIGHT_SPACING * 0.5, LIGHT_SPACING) - LIGHT_SPACING * 0.5;
	return p;
}

vec4 lightPoles(vec3 p) {
	float dis = length(p);
	vec3 col = vec3(1.0, 0.9, 0.6);
	const float strength = 5.0;
	vec3 res = col * strength / (dis * dis * dis);
	return vec4(res, dis);
}

vec3 lightShipModifyPos(vec3 p) {
	return p - vec3(sin(iGlobalTime)*5.0, 1, cos(iGlobalTime)*5.0);
}

vec4 lightShip(vec3 p) {
	float dis = sdCylinder(p, 0.0);
	vec3 col = vec3(1.0, 1.0, 1.0);
	float strength = 20.0;

	vec3 res = col * strength / (dis * dis * dis);
	return vec4(res, dis);
}

vec4 lightUnion(vec4 a, vec4 b)
{
	return vec4(a.rgb + b.rgb, min(a.w, b.w));
}

vec4 evaluateLight(vec3 pos)
{
	//vec4 res = lightA(lightAModifyPos(pos));
	//res = lightUnion(res, lightPoles(lightPolesModifyPos(pos)));
	vec4 res = lightPoles(lightPolesModifyPos(pos));
	res = lightUnion(res, lightShip(lightShipModifyPos(pos)));
	res = lightShip(lightShipModifyPos(pos));
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
		matSpec = 0.5;
	}

	{
		//vec3 posLightOrigo = lightAModifyPos(pos);
		//addLight(diffuse, specular, normal, eye, pos-posLightOrigo, lightA(posLightOrigo).rgb, 1.0, pos, matSpec);
	}
	{
		vec3 posLightOrigo = lightShipModifyPos(pos);
		addLight(diffuse, specular, normal, eye, pos-posLightOrigo, lightShip(posLightOrigo).rgb, 1.0, pos, matSpec);
	}
	{

		/*int qx = int(round(pos.x / LIGHT_SPACING));
		vec3 lightPos = vec3(qx*LIGHT_SPACING, LIGHT_HEIGHT + 0.1, sign(pos.z)*LIGHT_WIDTH(pos)*0.9); 
		vec3 dir = lightPos - pos;
		float shadow = shadowFunction(pos, normalize(dir), 0.1, length(dir));

		vec3 posLightOrigo = lightPolesModifyPos(pos);
		addLight(diffuse, specular, normal, eye, lightPos, lightPoles(posLightOrigo).rgb, shadow*0.5 + 0.5, pos, matSpec);*/
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
	const float maxDis = 100.0;
	const int jumps = 1;

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
			float fogAmount = 0.5;
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
					
					c = vec3(1.0);
				} else if (m == MAT_PATH) {
					float n = noiseOctave(p.xz, 10, 0.8);
					float p = pathPattern(p.xz);
					vec3 brick = vec3(0.05, 0.1, 0.3);
					vec3 mortar = vec3(0.5);
					c = mix(mortar, brick, p);
					c *= n;
				} else if (m == MAT_POLE) {
					c = vec3(1.0);
				} else if (m == MAT_WATER) {
					c = vec3(0);
				}

				c *= occlusion(p, normal, rd);
				addLightning(c, normal, eye, p, m);
				
				if (end) {
					transmittance = 0;
				}
				col = mix(col, transmittance * c + scatteredLight, ref);

				if (m == MAT_WATER) {
					ref *= 0.9;
				} else {
					//return vec3(float(i)/float(maxIter));
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
	//

	float r = length(vec2(u,v));
 float k1 = 0;
 float k2 = 0.5;
 float k3 = 0.1;
 u = u * (1 + 0.001 * r * r + k2 * r * r * r * r + k3 * r * r * r * r * r * r);
 v = v * (1 + 0.001 * r * r + k2 * r * r * r * r + k3 * r * r * r * r * r * r); 
 u *= 16.0 / 9.0;
   // vec3 eye = vec3(6 * sin(iGlobalTime) - 20.0, 3, 6 * cos(iGlobalTime));
	//vec3 tar = vec3(-20 ,1, 0); 

	 vec3 eye = vec3(4 * sin(iGlobalTime*0), 2, 4 * cos(iGlobalTime*0));
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
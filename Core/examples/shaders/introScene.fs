R""(#version 440

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


#define PART_TWIST 15.0

#define PI 3.141592



vec2 un(vec2 a, vec2 b)
{
	return a.x < b.x ? a : b;
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




#define REFLECTION

#define VOLUMETRIC_LIGHTNING

#define SHADOWS

#define TONE_MAPPING



#define MAT_FLOOR 1.0
#define MAT_ROOF 2.0
#define MAT_CORRIDOR 3.0


float sdBox( vec3 p, vec3 b )
{
  vec3 d = abs(p) - b;
  return min(max(d.x,max(d.y,d.z)),0.0) + length(max(d,0.0));
}

float sdCylinder( vec3 p, float r )
{
  return length(p.xz)-r;
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




float BrickPattern(in vec2 p) 
{
  p *= vec2 (1.0, 2.8);  // scale
  vec2 f = floor (p);
  if (2. * floor (f.y * 0.5) != f.y) 
    p.x += 0.5;  // brick shift
  p = smoothstep (0.03, 0.08, abs (fract (p + 0.5) - 0.5));
  return 1. - 0.9 * p.x * p.y;
}





float GridPattern(in vec2 uv)
{
  return 0.5*clamp(10.*sin(PI*uv.x) + 10.5, 0.0, 1.0)
       / 0.5*clamp(10.*sin(PI*uv.y) + 10.5, 0.0, 1.0);
}

float SquareHolePattern(in vec2 uv)
{
  float thickness = 0.4;
  float t = cos(uv.x*2.0) * cos(uv.y*2.0) / thickness;
  return smoothstep(0.1, 0.0, t*t);
}



float floorPattern(vec2 p) {
	 return SquareHolePattern(p*7.2 - vec2(0.0, 0.0));
}

float roofPattern(vec2 p) {
	return GridPattern(p*5.0);
}

float corrNoise(vec3 p){
	return 0.4*noiseOctave(vec2(p.z, abs(p.y) > 0.95 ? p.x : p.y) * 25.0, 3, 1.0); // Use same noise for walls and floor
	//return noiseOctave(vec2(p.z, abs(p.y) > 0.95 ? p.x : p.y) * 5.0, 10, 0.7); // Use same noise for walls and floor
}

vec3 distort(vec3 p) {

	if (iGlobalTime > PART_TWIST) {
		float a = atan(p.y, p.x);
		float l = length(p.xy);
		a += (p.z - PART_TWIST)*0.2*smoothstep(PART_TWIST + 5, PART_TWIST + 10, iGlobalTime);
		a -= (p.z - PART_TWIST)*0.2*smoothstep(PART_TWIST + 10, PART_TWIST + 15, iGlobalTime);
		return vec3(cos(a) * l, sin(a) * l, p.z);
	}

	return p;
	/*float a = atan(p.y, p.x);
	float l = length(p.xy);
	a += 1.2*sin(p.z*0.4 + iGlobalTime*0.3);
	return vec3(cos(a) * l, sin(a) * l, p.z);*/
}

// Smooth min
float smin( float a, float b)
{
	float k = 0.1;
    float h = clamp( 0.5+0.5*(b-a)/k, 0.0, 1.0 );
    return mix( b, a, h ) - k*h*(1.0-h);
}

// Smooth min. k determines smoothness
float smink( float a, float b, float k, inout float h)
{
    h = clamp( 0.5+0.5*(b-a)/k, 0.0, 1.0 );

    return mix( b, a, h ) - k*h*(1.0-h);
}

vec2 map(vec3 p, vec3 rd) 
{
	p = distort(p);
	



	float dc = -sdBox(p, vec3(1000.0));
	vec2 res = vec2(dc, MAT_CORRIDOR);

	
	return res;
}

float sdTorus( vec3 p, vec2 t )
{
	p.y *= 0.7;
  vec2 q = vec2(length(p.xy)-t.x,p.z);
  return length(q)-t.y;
}

float sdTorusJ( vec3 p, vec2 t )
{
	
  vec2 q = vec2(length(p.xy)-t.x,p.z);
  float d = length(q)-t.y;

	if (p.y > 0) {
		d = max(d, p.y);
	}
	return d;
}

vec3 lightAModifyPos(vec3 p)
{
	float s = 2.0;
	//p.z = mod(p.z + s*0.5, s) - s*0.5;
	return p;
}


vec4 lightA(vec3 p, vec3 realp)
{
	float dis = sdCappedCylinder(p - vec3(0,-0.025,0), vec2(0.0, 0.325));
	 dis = min(dis, sdCappedCylinder(p.yxz - vec3(0.3, -0.2, 0.0), vec2(0.0, 0.2)));
	 dis = min(dis, sdTorusJ(p - vec3(-0.2, -0.35, 0.0), vec2(0.2, 0.0)));


	 dis = min(dis, sdTorus(p - vec3(-0.9, -0.1, 0.0), vec2(0.3, 0.0)));

	vec3 col = vec3(1.0, 1.0, 1.0);
	float strength = 1.0;
	int q = int((realp.z + 2.5) / 5.0);
	if (iGlobalTime*4.0 +  q *5.0 > 60 && iGlobalTime <= PART_TWIST) {

			col = vec3(0.0, 1.0, 0.2);
			strength = smoothstep(0 + 1, 0 + 2 + 1, iGlobalTime);

		
	}
	vec3 res = col * strength / (dis * dis * dis);
	return vec4(res, dis);
}

vec4 lightUnion(vec4 a, vec4 b)
{
	return vec4(a.rgb + b.rgb, min(a.w, b.w));
}

vec4 evaluateLight(vec3 pos)
{
	pos = distort(pos);
	vec4 res = lightA(lightAModifyPos(pos), pos);
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
	float spec = specular(normal, -invLight, normalize(eye - pos), 100.0);
	float dis = length(lightPos - pos);
	float str = 1.0/(0.5 + 0.01*dis + 0.1*dis*dis);
	float specStr = 1.0/(0.0 + 0.00*dis + dis*dis*dis);
	diffRes += diffuse * lightCol * shadow;
	
	specRes += spec  *  shadow  * 1.0 * length(lightCol);
}

void addLightning(inout vec3 color, vec3 normal, vec3 eye, vec3 pos) {
	vec3 diffuse = vec3(0.0);
	float specular = 0.0;
	const float ambient = 0.0;

	{
		vec3 dp = distort(pos);
		vec3 posLightOrigo = lightAModifyPos(dp);
		
		float s = 5.0;
		int q = int(round(dp.z / s));
		vec3 lightPos = distort(vec3(0.0, 0.8, q*s)); 
		vec3 dir = lightPos - pos;
		float shadow = 1.0;//shadowFunction(pos, normalize(dir), 0.1, length(dir));
		addLight(diffuse, specular, normal, eye, lightPos, lightA(posLightOrigo, pos).rgb, shadow, pos);
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
	int maxIter = 300;
	const float maxDis = 200.0;
	const int jumps = 1;

	vec3 col = vec3(0);	
	float ref = 1.0;

	vec3 scatteredLight = vec3(0.0);
	float transmittance = 1.0;
	for (int j = 0; j < jumps; j++) {
		if (j == 1) {
			maxIter = 10;
		}
		float t = 0.0;
		for (int i = 0; i < maxIter; i++) {
			vec3 p = ro + rd * t;
			//p.x += sin(p.z*0.5);
			vec2 res = map(p, rd);
			float d = res.x;
			float m = res.y;
#ifdef VOLUMETRIC_LIGHTNING
			float fogAmount = 0.001;

			int q = int((p.z + 2.5) / 5.0);
	if (iGlobalTime < PART_TWIST) {
		fogAmount = mix(fogAmount, 0.5, smoothstep(70, 80, iGlobalTime*4.0 +  p.z));
	}
			

			

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

				float fm = floor(m);
				if (fm == MAT_CORRIDOR) {
					vec3 dp = distort(p);
					float pattern = BrickPattern(dp.zy * 2.1 + vec2(0.0, 0.0));
					float n = noiseOctave(vec2(dp.z, dp.y) * 5.0, 10, 0.7);
					vec3 brick = vec3(1.0, 0.6, 0.35)*(0.1 + 0.9 * n);
					vec3 mortar = vec3(1.0);
					c = mix(brick, mortar, pattern);
					//c = vec3(n);
				} else if (fm == MAT_ROOF) {
					vec3 dp = distort(p);
					float pattern = roofPattern(dp.xz);
					c = mix(vec3(0.5), vec3(0.85, 0.75, 0.45), pattern);
				} else if (fm == MAT_FLOOR) {
					vec3 dp = distort(p);
					float n = corrNoise(dp*4.0);
					float pattern = floorPattern(dp.xz);
					vec3 mortar = vec3(0.5);
					vec3 tile = mix(vec3(0.4)*1.0, vec3(1.0), n);
					/*vec3 tile = vec3(0.45, 0.55, 0.5)*0.8;
					if (n > 0.5) {
						tile = vec3(0.8);
					}*/
					c = mix(tile,mortar, pattern);
					//c = tile;
					//c = vec3(n);
				}

				float ms = mod(m, 1.0) * 2.0;

				vec3 dp = distort(p);
				vec3 pc = vec3(1.0, 0.0, 0.0);
				c = mix(c, pc, ms);
				//c = vec3(ms);

				c *= occlusion(p, normal, rd);
				addLightning(c, normal, eye, p);
				
				if (end) {
					transmittance = 0;
				}
				col = mix(col, transmittance * c + scatteredLight, ref);
				
				if (ms > 0.05 ) {
						ref *= ms * 0.2;
				//} else if(floor(m) == MAT_ROOF) {
				//	ref *= 0.3;
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

	float y = max(iGlobalTime - 3.0, 0.0);
    vec3 eye = vec3( 0.5, y, -3); 

	vec3 ed = distort(eye);
	vec3 td = distort(eye + vec3(0, 0, 1));
	td.x = -td.x;
	eye.x = -ed.x;
	
	vec3 tar = td;

	vec3 dir = normalize(tar - eye);
	vec3 right = normalize(cross(vec3(0, 1, 0), dir));
	vec3 up = cross(dir, right);

	vec3 ro = eye;
	vec3 rd = normalize(dir + right*u*1.0 + up*v*1.0);
	
	vec3 color = raymarch(ro, rd, eye);
	
#ifdef TONE_MAPPING
	color /= (color + vec3(1.0));
#endif
	float t = 0.5;
	float a = abs(iGlobalTime - 0.0);

		color = mix(color, vec3(0), clamp(1.0 - a / t + length(vec2(u,v)), 0, 1));

	float b = abs(iGlobalTime - PART_TWIST);
	color = mix(color, vec3(0), clamp(1.0 - b / t + length(vec2(u,v)), 0, 1));
    fragColor = vec4(color, 1.0);
} 

)""
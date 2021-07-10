R""(

in vec2 fragCoord;
out vec4 fragColor;

uniform float iTime;
uniform sampler2D inTexture0;
uniform sampler2D inTexture1;
uniform sampler2D inTexture2;


uniform float CHANNEL_11_SINCE;
uniform float CHANNEL_11_TOTAL;

uniform float CHANNEL_4_SINCE[2];










#define PART_FLY 22.5
#define PART_WALK (PART_FLY + 15)
#define PART_DRUM (PART_WALK + 15)

#define MAT_GRAVE 1.0
#define MAT_GROUND 2.0
#define MAT_PATH 3.0
#define MAT_POLE 4.0


float smink( float a, float b, float k )
{
    float h = clamp( 0.5+0.5*(b-a)/k, 0.0, 1.0 );
    return mix( b, a, h ) - k*h*(1.0-h);
}


vec2 un(vec2 a, vec2 b)
{
	return a.x < b.x ? a : b;
}

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



#define LIGHT_WIDTH(p) (1.5) 
#define LIGHT_SPACING 3.0
#define LIGHT_HEIGHT 1.8

vec2 map(vec3 p, vec3 rd) 
{
	const float pathWidth = 1.5;
	vec2 res = vec2(99999999, 0);

	if (p.y < 0.5) {
		float d = p.y;


		d -= 0.08*noiseOctave(p.xz*10.0, 3, 0.7);


		
		res = un(res, vec2(d, MAT_GROUND)); 
		
	} else {
		res = un(res, vec2(p.y, MAT_GROUND)); 
	}



		float s = 2.0;
		vec3 q = mod(vec3(p.x, p.y, p.z) + s*0.5, s) - s * 0.5;
		q.y = p.y;
		float d = sdBox(q - vec3(0, 0.7, 0), vec3(0.1, 0.5, 0.02));
		float d2 = sdBox(q - vec3(0, 0.85, 0), vec3(0.4, 0.1, 0.02));
		res = sunk(vec2(min(d, d2) + max(0.0, 1.0 - abs(p.z)), MAT_GRAVE), res, 0.5);

	

	{
		vec3 q = p;
		q.z = abs(p.z) - LIGHT_WIDTH(p);
		float s = LIGHT_SPACING;
		q.x = mod(p.x + s * 0.5, s) - s * 0.5;
		float w = 0.02;
		float d = sdCappedCylinder(q, vec2(w, LIGHT_HEIGHT));
		res = sunk(vec2(d, MAT_POLE), res, 0.5);
	
	}
	return res;
}

vec3 lightAModifyPos(vec3 p)
{
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
	return p - vec3( sin(iTime)*5.0, 2,10 +  cos(iTime)*5.0);
}

vec4 lightShip(vec3 p) {
	float dis = sdCylinder(p, 0.0);
	vec3 col = vec3(1.0, 1.0, 1.0);
	float strength = 20.0;

	vec3 res = col * strength / (dis * dis * dis);
	return vec4(res, dis);
}

vec3 lightDrumModifyPos(vec3 p, float side) {
	return p- vec3(sign(side)*3.0, 2.0, 8.5);
}

vec4 lightDrum(vec3 p, vec3 rp, float c) {
	float dis = sdCylinder(p, 0.0);
	vec3 col =  vec3(1.0, 0.5, 0.1);

	float strength = 20.0;
	/*float c = CHANNEL_4_SINCE[0];
	if (rp.x > 0.0) {
		c = CHANNEL_4_SINCE[1];
	}*/
	strength *= max(0.0, 1.0 - c*2.0);

	vec3 res = col * strength / (dis * dis * dis);
	return vec4(res, dis);
}

vec4 lightUnion(vec4 a, vec4 b)
{
	return vec4(a.rgb + b.rgb, min(a.w, b.w));
}

vec4 evaluateLight(vec3 pos)
{

	vec4 res = vec4(0);
	if (iTime < PART_WALK) {
		return  lightPoles(lightPolesModifyPos(pos));
	} else if (iTime < PART_DRUM ) {
		vec4 a = lightDrum(lightDrumModifyPos(pos, -1.0), pos, CHANNEL_4_SINCE[0]);
		vec4 b = lightDrum(lightDrumModifyPos(pos, 1.0), pos, CHANNEL_4_SINCE[1]);
		return lightUnion(a, b);
	} else {
		return lightShip(lightShipModifyPos(pos));
	}
}


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


void addLight(inout vec3 diffRes, inout float specRes, vec3 normal, vec3 eye, vec3 lightPos, vec3 lightCol, float shadow, vec3 pos, float matSpec)
{
	vec3 col = vec3(0.0);
	vec3 invLight = normalize(lightPos - pos);
	float diffuse = max(0.0, dot(invLight, normal));
	float spec = specular(normal, -invLight, normalize(eye - pos), 80.0);

	float dis = length(lightPos - pos);

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


	if (iTime < PART_WALK){

		int qx = int(round(pos.x / LIGHT_SPACING));
		vec3 lightPos = vec3(qx*LIGHT_SPACING, LIGHT_HEIGHT + 0.1, sign(pos.z)*LIGHT_WIDTH(pos)*0.9); 
		vec3 dir = lightPos - pos;
		float shadow = shadowFunction(pos, normalize(dir), 0.1, length(dir));

		vec3 posLightOrigo = lightPolesModifyPos(pos);
		addLight(diffuse, specular, normal, eye, lightPos, lightPoles(posLightOrigo).rgb, shadow*0.5 + 0.5, pos, matSpec);
	} else if (iTime < PART_DRUM) {
		{
			vec3 posLightOrigo = lightDrumModifyPos(pos, -1.0);
			vec3 lightPos = pos - posLightOrigo;
			vec3 dir = lightPos - pos;
			float shadow = shadowFunction(pos, normalize(dir), 0.1, length(dir));
			addLight(diffuse, specular, normal, eye, pos-posLightOrigo, lightDrum(posLightOrigo, pos, CHANNEL_4_SINCE[0]).rgb, shadow, pos, matSpec);
		}
		{
			vec3 posLightOrigo = lightDrumModifyPos(pos, 1.0);
			vec3 lightPos = pos - posLightOrigo;
			vec3 dir = lightPos - pos;
			float shadow = shadowFunction(pos, normalize(dir), 0.1, length(dir));
			addLight(diffuse, specular, normal, eye, pos-posLightOrigo, lightDrum(posLightOrigo, pos, CHANNEL_4_SINCE[1]).rgb, shadow, pos, matSpec);
		}

		
	} else {
		vec3 posLightOrigo = lightShipModifyPos(pos);
		vec3 lightPos = pos - posLightOrigo;
		vec3 dir = lightPos - pos;
		float shadow = shadowFunction(pos, normalize(dir), 0.1, length(dir));
		addLight(diffuse, specular, normal, eye, pos-posLightOrigo, lightShip(posLightOrigo).rgb, shadow, pos, matSpec);
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
	if (iTime > PART_WALK) {
		maxIter = 30;
	}
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

			float fogAmount = 0.5;
			if (iTime < PART_WALK) {
				fogAmount = 0.005;
			}
			vec4 lightColDis = evaluateLight(p);
			vec3 light = lightColDis.rgb;
			d = min(d, lightColDis.w);

			vec3 lightIntegrated = light - light * exp(-fogAmount * d);
			scatteredLight += transmittance * lightIntegrated;	
			transmittance *= exp(-fogAmount * d);

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
				} 

				c *= occlusion(p, normal, rd);
				addLightning(c, normal, eye, p, m);
				
				if (end) {
					transmittance = 0;
				}
				col = mix(col, transmittance * c + scatteredLight, ref);

					return col;

			}
		}
	}
	return col;
}

void main()
{
    float u = fragCoord.x * 2.0 - 1.0;
	float v = fragCoord.y * 2.0 - 1.0;
	float f = 1.0;
	if (iTime < PART_FLY) {
		f = 1.2;
	}
	u *= f;
	v *= f;
	//

	float r = length(vec2(u,v));
	float k1 = 0.001;
	float k2 = 0.5;
	 float k3 = 0.1;
	if (iTime < PART_FLY) {
		k1 = 0.0;
		k2 = 0.0;
		k3 = 0.0;
	}

 u = u * (1 + k1 * r * r + k2 * r * r * r * r + k3 * r * r * r * r * r * r);
 v = v * (1 + k1 * r * r + k2 * r * r * r * r + k3 * r * r * r * r * r * r); 
 u *= 16.0 / 9.0;

	 vec3 eye = vec3(4 * sin(iTime*0), 2, 4 * cos(iTime*0));
	vec3 tar = vec3(0 ,1, 0); 
	if (iTime < PART_FLY) {
		float t = iTime - 0.0;
		eye = vec3(0, 8, t-15.0);
		float a = -3.1415 / 2;
		float c = 0;
		if (CHANNEL_11_TOTAL == 1) {
			c =  min(1.0, CHANNEL_11_SINCE);
		} else if (CHANNEL_11_TOTAL > 1) {
			c = 1.0;
		}
		a += 3.1415* 0.5 * c;
		tar = eye + vec3(0, sin(a), cos(a));
	} else if (iTime < PART_WALK) {
		float t = iTime - PART_FLY;
			float bob = min(t, 10.0);
			eye = vec3(bob, 1.2 + sin(bob*8.0)*0.05, 0);
			float look = max(0.0, t - 10.0);
			tar = eye + vec3(1, 0, sin(look));
	} else if (iTime < PART_DRUM) {
		float t = iTime - PART_WALK;
		eye = vec3(0, 2, 0 + t);
		tar = eye + vec3(0, -0.5, 1); 
	} else {
		eye = vec3(0, 2, 4);
		tar = eye + vec3(0, -0.5, 1); 
	}

	vec3 dir = normalize(tar - eye);
	vec3 right = normalize(cross(vec3(0, 1, 0), dir));
	vec3 up = cross(dir, right);

	vec3 ro = eye;
	vec3 rd = normalize(dir + right*u + up*v);
	
	vec3 color = raymarch(ro, rd, eye);

	float trans = 0.5;
	float a = abs(iTime - PART_FLY);
	color = mix(color, vec3(0), clamp(1.0 - a / trans + length(vec2(u,v)), 0, 1));


	color /= (color + vec3(1.0));

    fragColor = vec4(color, 1.0);
	


} 

)""  
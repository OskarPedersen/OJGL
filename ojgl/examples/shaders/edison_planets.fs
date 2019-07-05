R""(
#version 430

in vec2 fragCoord;
out vec4 fragColor;

uniform float iTime;
uniform float iTotalTime;
uniform vec2 iResolution;
uniform float DEBUG_D1;
uniform float DEBUG_D2;
uniform float DEBUG_D3;


uniform float marsScale;
uniform vec3 planets[10];
float[] planetScales = float[10](2.0,	// Earth
								100.0,	// Sun
								1.0,	// Mercury
								1.5,	// Venus
								1.2,	// Mars
								5.0,	// Jupiter
								4.0,	// Saturnus
								3.0,	// Uranus
								3.0,	// Neptunus
								0.1);	// Moon


vec3[] planetColors = vec3[10](vec3(0.0, 1.0, 0.0),	// Earth
								vec3(1.0, 0.8, 0.0),	// Sun
								vec3(0.6, 0.5, 0.5),	// Mercury
								vec3(38.0, 100.0, 92.0) / 255.0,	// Venus
								vec3(196.0, 35.0, 0.0) / 255.0,	// Mars
								vec3(255.0, 234.0, 165.0) / 255.0,	// Jupiter
								vec3(198.0, 182.0, 129.0) / 255.0,	// Saturnus
								vec3(83.0, 92.0, 137.0) / 255.0,	// Uranus
								vec3(38.0, 53.0, 137.0) / 255.0,	// Neptunus
								vec3(0.5));	// Moon
/// start of boiler-plate
#define NUM_SCENES 3
float[] sceneLengths = float[NUM_SCENES](15., 15., 20.);

#define fTime mod(iTime, 50.)

int currentScene() 
{
    float s = fTime;
	for(int i = 0; i < NUM_SCENES; i++) {
		s-= sceneLengths[i];
		if (s < 0) return i;
	}
	return NUM_SCENES;
}

float localTime() {
	float s = fTime;
	for(int i = 0; i < NUM_SCENES; i++) {
		if (s - sceneLengths[i] < 0) return s;
		s-= sceneLengths[i];
	}
	return s;
}

float localTimeLeft() {
	float s = fTime;
	for(int i = 0; i < NUM_SCENES; i++) {
		if (s - sceneLengths[i] < 0) return sceneLengths[i] - s;
		s-= sceneLengths[i];
	}
	return 99999999999.;
}

#define lTime localTime()
#define cScene currentScene()
#define lTimeLeft localTimeLeft()

#define PI 3.14159265

vec3 palette( in float t, in vec3 a, in vec3 b, in vec3 c, in vec3 d )
{
    return a + b*cos( 6.28318*(c*t+d) );
}

mat2 rot(float a)
{
    return mat2(cos(a),sin(a),-sin(a),cos(a));
}

float pMod1(inout float p, float size) {
	float halfsize = size*0.5;
	float c = floor((p + halfsize)/size);
	p = mod(p + halfsize, size) - halfsize;
	return c;
}

void moda (inout vec2 p, float rep)
{
    float per = 2.*PI/rep;
    float a = atan(p.y, p.x);
    float l = length(p);
    a = mod(a-per*0.5,per)-per*0.5;
    p = vec2(cos(a),sin(a))*l;  
}

void mo (inout vec2 p, vec2 d)
{
    p = abs(p)-d;
    if(p.y >p.x) p = p.yx;
}

float stmin (float a, float b, float k, float n)
{
    float st = k/n;
    float u = b-k;
    return min(min(a,b), 0.5*(u+a+abs(mod(u-a+st,2.*st)-st)));
}


float smoothspike(float left, float right, float value) 
{
	float mid = (left + right) / 2.;
    return value < mid ? smoothstep(left, mid, value) : 1. - smoothstep(mid, right, value);
}



mat3 rotateAngle(vec3 v, float a )
{
    float si = sin( a );
    float co = cos( a );
    float ic = 1.0f - co;

    return mat3( v.x*v.x*ic + co,       v.y*v.x*ic - si*v.z,    v.z*v.x*ic + si*v.y,
                   v.x*v.y*ic + si*v.z,   v.y*v.y*ic + co,        v.z*v.y*ic - si*v.x,
                   v.x*v.z*ic - si*v.y,   v.y*v.z*ic + si*v.x,    v.z*v.z*ic + co );
}



const float EPS = 1e-3;
const int MAX_STEPS = 100;

const float T_INF = -1.0;
const float T_SPHERE = 0.0;
const float T_WALL = 1.0;
const float T_BOX = 2.0;
const float T_ARROW = 3.0;
const float T_PLANET = 4.0;
const float T_RINGS = 5.0;
const float T_TUNNEL = 6.0;
    
float psin(float v) 
{
    return (1.0 + sin(v)) * 0.5;
}

float reflectiveIndex(float type) 
{
    if (type == T_WALL)
        return 0.4 + 0.*smoothspike(3., 5., mod(iTime, 5.0));
	if (type == T_SPHERE)
        return 0.2;
	if (type == T_BOX)
        return 0.5;
	if(type == T_PLANET) {
		return 1.0;	
	}
	if (type == T_TUNNEL) {
		return 0.5;
	}
	return 0.0;
}

float specularFactor(float type) 
{
    if (type == T_WALL)
        return 0.0;
	return 1.0;
}


vec3 color(float type, vec3 p) 
{
    if (type == T_WALL)
		 return vec3(0.1);
       //return palette(fract(-p.x*p.y*0.015), vec3(0.5), vec3(0.5), vec3(1.0, 1.0, 1.0), vec3(0.00, 0.10, 0.20	) );
    else if (type == T_SPHERE)
        return vec3(0.2, 0.1, 0.6);
    else if (type == T_BOX) {
        
        vec3 white = vec3(0., 1.0, DEBUG_D1);
        vec3 red = vec3(1.0, 0, 0);
		float m = mod(floor((iTime+1.9) * 1. / 2.), 2.);
        red = mix(white, red, 1);
        
        vec3 yellow = vec3(1.0, 1.0, 0.0);
        //return palette(fract(p.x*p.y*0.02), vec3(0.5), vec3(0.5), vec3(1.0, 1.0, 0.5), vec3(0.80, psin(iTime), 0.30	) );

		//return palette(fract(-p.x*p.y*0.05), vec3(0.5), vec3(0.5), vec3(	2.0, 1.0, 0.0), vec3(0.50, 0.20, 0.25) );
        //return red;
		//return mix(red, yellow, 0.3*noise_3(2.0*sin(iTime*0.5)*p + iTime*0.5));
     }
    else if (type == T_ARROW)
        return vec3(0.1, 0.1, 0.8);
	else if (type == T_PLANET){
		return vec3(1.0, 0.0, 1.0);
	}
	else if (type == T_RINGS){
		return vec3(0.0, 1.0, 0.0);
	}
	else if (type == T_TUNNEL) {
		return vec3(0.7, 0.1, 0.0);
	}
    return vec3(0.0);
}

float sdPlane( vec3 p, vec4 n )
{
  // n must be normalized
  return dot(p,n.xyz) + n.w;
}


float sdCappedCylinder( vec3 p, vec2 h )
{
  vec2 d = abs(vec2(length(p.xz),p.y)) - h;
  return min(max(d.x,d.y),0.0) + length(max(d,0.0));
}


float sdSphere( vec3 p, float s )
{
  return length(p)-s;
}

float sdBox(vec3 p, vec3 b)
{
  vec3 d = abs(p) - b;
  return length(max(d,0.0)) + min(max(d.x,max(d.y,d.z)),0.0);
}

float udRoundBox(vec3 p, vec3 b, float r)
{
  return length(max(abs(p)-b,0.0)) - r;
}

// O
//float sdTorus(vec3 p, vec2 t)
//{
//  p.y *= 0.7;
//  p.zy = p.yz;
//  vec2 q = vec2(length(p.xy)-t.x,p.z);
//  return length(q)-t.y;
//}

float sdCappedCylinder( vec3 p, float h, float r )
{
  vec2 d = abs(vec2(length(p.xz),p.y)) - vec2(h,r);
  return min(max(d.x,d.y),0.0) + length(max(d,0.0));
}

float sdTorus( vec3 p, vec2 t )
{
  vec2 q = vec2(length(p.xz)-t.x,p.y);
  return length(q)-t.y;
}

float sdTorusJ(vec3 p, vec2 t)
{
    p.y -= 2.0;
  //p.zy = p.yz;
  vec2 q = vec2(length(p.xy)-t.x,p.z);
  float d = length(q)-t.y;

	if (p.y > 0.0) {
		d = max(d, p.y);
	}
    
   	 d = min(d, sdCappedCylinder(p, vec2(0, 1)));

	return d;
}


float smink( float a, float b, float k )
{
    float h = clamp( 0.5+0.5*(b-a)/k, 0.0, 1.0 );
    return mix( b, a, h ) - k*h*(1.0-h);
}

vec2 sun(vec2 a, vec2 b)
{
    float sm = smink(a.x,b.x, 0.1);
	float m = min(a.x, b.x);
	float ca = abs(sm -a.x);
	float cb = abs(sm -b.x);
	
	return ca < cb ? vec2(sm, a.y) : vec2(m, b.y);
}

vec2 un(vec2 a, vec2 b) { return a.x < b.x ? a : b; }




//vec2 flooring(vec3 p) {
//return vec2(p.y + 0.5 + 0.02*noise_2(2.*p.xz + 0.5*iTime), T_WALL);
//  //return vec2(p.y +1.7 + 0.02*noise_2(2.*p.xz + 0.5*iTime), T_WALL);
//}


vec2 wall(vec3 pos, vec3 rd) {
	//pos += vec3(100);
	vec3 d = vec3(1.0);
	vec3 q = mod(pos, d) - 0.5 * d;

	vec3 s = (d * 0.5 -  sign(rd)* q) / abs(rd);
	vec3 part = floor(pos / d);
//	pos = mod(pos, d) - d * 0.5;

	float dis = 0;
//	float partDis = -sdHexPrism(part, vec2(3,99999));
	//float r = floor(1 + mod(part.z * 0.25, 1) * 3);
	//float partDis = -udBox(part,vec3(r, r, 999999));
	float partDis = -sdBox(part, vec3(5.0));
	if (partDis < 0) {
		//dis = udRoundBox(q, vec3(0.45 * d.x), 0.1 * d.x);
		dis = udRoundBox(q, vec3(0.42* d.x), 0.00 * d.x);
	} else {
		float b = min(s.x, min(s.y, s.z));
		dis = max(0.02 , b);
	}
//		dis = udRoundBox(q, vec3(0.4), 0.1);
//	dis = -sdHexPrism(pos, vec2(8,99999));
//	dis = -sdCylinder(pos.xzy, 10);
	return vec2(dis, T_TUNNEL);

}

vec2 map(in vec3 p, vec3 rd) 
{
	vec2 res = vec2(999999, T_PLANET);
	//for (int i = 0; i < planets.length(); i++) {
	//	if (i != 1) {
	//		continue;
	//	}
	//	float d = length(p - planets[i]) - 0.05 * planetScales[i];
	//	float m = T_PLANET;
	//	res = un(res, vec2(d, m));
	//	
	//}
	//{
	//	//float d = sdTorus(p - planets[6], vec2(0.1, 0.01)* planetScales[6]);
	//	float d1 = sdCappedCylinder(p - planets[6], 0.12 * planetScales[6], 0.001 * planetScales[6]);
	//	float d2 = sdCappedCylinder(p - planets[6], 0.09 * planetScales[6], 0.001 * planetScales[6]);
	//	float d = max(-d1,d2);
	//	//float d = sdTorus(, vec2(0.1, 0.01)* planetScales[6]);
	//	float m = T_RINGS;
	//	res = un(res, vec2(d, m));
	//}
	

	//vec2 w = flooring(p);

	float d = -udRoundBox(p, vec3(10), 1.0);

	//return vec2(d, T_TUNNEL); //res;//sun(res, w);
	return wall(p, rd);
}

vec3 normal(vec3 p, vec3 rd) 
{
    float eps = EPS;
    vec3 n = vec3(map(vec3(p.x + eps, p.y, p.z), rd).x, map(vec3(p.x, p.y + eps, p.z), rd).x, map(vec3(p.x, p.y, p.z + eps), rd).x);
    return normalize(n - map(p, rd).x);
}

vec2 march(vec3 ro, vec3 rd, out vec3 p, out int steps)
{
    float t = 0.0;
   	vec2 res = vec2(99999.0, -1.0);
    for(steps = 0; steps < MAX_STEPS; ++steps) {
    	p = ro + t * rd;   
        vec2 tres = map(p, rd);
        t += tres.x;
        if (tres.x < EPS) {
			res = tres;
            break;
        }
        if (t > 400.0) {
            break;
        }
    }
    return res;
}

float shadow(vec3 ro, vec3 dir) 
{
    float t = 0.01;
 	float sf = 1.0;
    for(int i = 0; i < MAX_STEPS; ++i) {
		vec3 p = ro + t * dir;    	
        vec2 res = map(p, dir);
        t += clamp(res.x, 0.02, 0.1);
        if (res.x < 0.001)
            return 0.5;
       sf = min(sf, 8.0 * abs(res.x) / t);
    }
 	return min(1.0, 0.5 + 0.5*sf);
}

float ambientOcclusion(vec3 p, vec3 n, vec3 rd) 
{
	float as = 0.0;
    float sl = 60.0 * 1e-3;
    int ns = 6;
    for(int i = 0; i < ns; i++) {
    	vec3 ap = p + float(i) * sl * n;    
    	as += map(ap, rd).x;
    }
    return mix(1.0, smoothstep(0.0, float(ns *(ns - 1) / 2) * sl, as), 0.6);
}
                          
vec3 colorize(vec2 res, vec3 p, vec3 dir, int steps, vec3 ro, inout vec3 lightAura) 
{
    vec3 light = normalize(vec3(1.0, -1,  -1.));
    vec3 lightPos = planets[1]; //ballPos();
	//lightPos.y = max(1., lightPos.y);
    //light = normalize(p - lightPos);
    
    vec3 n = normal(p, dir);
    float lf = 1.0;//min(2.5, 3.0 / (0.02 + 0.1*pow(length(p - lightPos), 3)));
    
    // Material properties
    float diffuse1 = 0.0*max(0.,dot(-light, n));
    float diffuse2 = max(0.,dot(-normalize(p - lightPos), n));
    float diffuse = max(diffuse1, 2.*diffuse2);
    float k = max(0.0, dot(dir, reflect(-normalize(p - lightPos), n)));
    float spec = specularFactor(res.y) * pow(k, 100.0);
    
    vec3 col = color(res.y, p);
	float ao = 1.0;//ambientOcclusion(p, n);
   //	float sh = shadow(p, light);
    if (res.x < EPS) {
        col =  (lf) * (ao * col *(0.00+diffuse) + spec);
		//col = vec3(1.0, 0.0, 0.0);
	} else {
		col = vec3(0.0);
		//col = vec3(noise_3(100.0 * vec3(uv.x, uv.y, 0)));
		//return vec3(0.0);
	}
    
	const vec3 eye = ro;
	vec3 rd = dir;
	//vec3 lightAura = vec3(0.0);
	//int pl = 1;
    for (int pl = 0; pl < planets.length(); pl++)
	{
		//if (pl == 1) continue;
        float plf = float(pl);
        float lightInvSize = 5000.0 / planetScales[pl];//52.1 + 20.0*sin(float(pl));
		if (pl == 4) {
			lightInvSize /= marsScale;
		}
        float speed = 1.0 * iTime/float(pl);
        vec3 light = planets[pl]; //0.5*vec3(float(pl) * 2.0 * sin(speed), 0.0, float(pl) * 2.0 * cos(speed));
        vec3 x0 = light;
        vec3 x1 = ro;
        vec3 x2 = ro + rd;
        float ldis = pow(length(cross(x2 - x1, x1 - x0)),2.0) / pow( distance(x2, x1), 2.0);
                    
                    
        float tl = -dot(x1 - x0, x2 - x1)/pow(distance(x2,x1),2.0);
        const bool lightCollision = true;

        if(tl > 0.0 && ((lightCollision && distance(eye, light) < distance(eye, p)) || !lightCollision)){
            //lightAura = max(lightAura, 1.0/(0.01 + lightInvSize*ldis));
            //const vec3 auraCol = vec3(0.5 + 0.5*sin(plf), 0.5 + 0.5*sin(plf*2.0), 0.5 + 0.5*sin(plf*0.5));
            //const vec3 auraCol = vec3(1.0, 0.8, 0.1);
			const vec3 auraCol = normalize(planetColors[pl]);
			//if (ldis < 0.5) {
				float fade = 0.5 - ldis * 10.0;
				fade = max(0.0, fade);
				lightAura += auraCol*vec3(1.0/(0.01 + lightInvSize*ldis)) * fade;
			//}
			
			//col = lightAura;
        }
    }
	//}
	//col += lightAura;

    //float ns = float(steps) / 100.;
    //return pow(col * ns * 20., vec3(0.4545));
	return pow(col, vec3(0.4545));
}

void main()
{

	int cs = cScene;
	float tl = lTimeLeft;
	float lt = lTime;
	float ft = tl + lt;

    vec2 uv = fragCoord - 0.5;
	uv.y *= iResolution.y / iResolution.x;
    
	vec3 ro = vec3(11.0, 5., 11.0);

	//if (cs == 1) {
	//	ro = vec3(14.0, 14., 14.0);
	//}if (cs == 2) {
	//	ro = vec3(13.0, 11., 13.0);
	//}


	//ro = planets[1] + vec3(0.0, 80.0, 0.1); // top down all planets
	//ro = planets[1] + vec3(0.1, 5.0, 0.1); // top down inner planets
	ro = planets[1] + vec3(3.0, 1.0, 2.0);

    vec3 tar = planets[1]; //vec3(0.0, 1.0, 0.0);

	//ro = planets[6] + vec3(5.0, 0.0, 5.0);
	//vec3 tar = planets[6];

	vec3 lightAura = vec3(0.0);
    //ro = mix(tar, ro, 1.0);
    vec3 dir = normalize(tar - ro);
	vec3 right = normalize(cross(vec3(0.0, 1.0, 0.0), dir));
	vec3 up = cross(dir, right);
	vec3 rd = normalize(dir + right * uv.x + up * uv.y);
    
    vec3 p;
    int steps;
    vec2 res = march(ro, rd, p, steps);
    vec3 col = colorize(res, p, rd, steps, ro, lightAura);

    //float ri = reflectiveIndex(res.y);
    //if (ri > 0.0) { 
    //    vec3 p2;
   	//	rd = reflect(rd, normal(p, rd));
    //	res = march(p + 0.1 * rd, rd, p2, steps);
    //	vec3 newCol = colorize(res, p2, rd, steps, p + 0.1 * rd, lightAura);
    //	col = mix(col, newCol, ri);
	//
	//
	//	float ri = reflectiveIndex(res.y);
	//	if (ri > 0.0) { 
	//		vec3 p3;
   	//		rd = reflect(rd, normal(p2, rd));
    //		res = march(p2 + 0.1 * rd, rd, p3, steps);
    //		vec3 newCol = colorize(res, p3, rd, steps, p2 + 0.1 * rd, lightAura);
    //		col = mix(col, newCol, ri);
	//	}
    //}

	//vec3 col = vec3(0.0);
	float ref = 1.0;
	int jumps = 5;
	for (int j = 0; j < jumps; j++) {
		vec3 p;
		int steps;
		vec2 res = march(ro, rd, p, steps);
		vec3 c = colorize(res, p, rd, steps, ro, lightAura);

		ref *= reflectiveIndex(res.y);
		col = mix(col, c, ref);
		 if (ref < 0.001) {
			break;
		 }
		 
		//vec3 p2;
   		rd = reflect(rd, normal(p, rd));
		ro = p + 0.1 * rd;


    	//res = march(p + 0.1 * rd, rd, p2, steps);
    	//vec3 newCol = colorize(res, p2, rd, steps, p + 0.1 * rd, lightAura);
    	
	}


	col += lightAura;

	float f = 1.0;
	//if (cs == 0) {
	//	f = smoothstep(0., 5., lt);
	//	f *= 1. - smoothstep(1., 0., tl);
	//} else if (cs == 1) {
	//	f = smoothstep(0., 1., lt);
	//	f *= 1. - smoothstep(1., 0., tl);
	//} else if (cs == 2) {
	//	f = smoothstep(0., 1., lt);
	//	f *= 1. - smoothstep(1., 0., tl);
	//}

	float dis = min(iTime, 25 - iTime);
	if (dis < 1.0) {
		col *= dis;
	}

    fragColor = vec4(f * col, 1.0);
}

)""

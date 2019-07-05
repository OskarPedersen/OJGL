R""(
#version 430

in vec2 fragCoord;
out vec4 fragColor;

uniform float iTime;
uniform vec2 iResolution;
uniform float DEBUG_D1;
uniform float DEBUG_D2;
uniform float DEBUG_D3;

// Globals
vec3 lightPosition = vec3(4.0, 0, 4);

/// start of boiler-plate

#define PI 3.14159265

#define NUM_SCENES 3
float[] sceneLengths = float[NUM_SCENES](14.5, 29., 14.5);
uniform vec3 planets[10];
#define fTime iTime

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


float hash11(float p) {
    return fract(sin(p * 727.1)*435.545);
}
float hash12(vec2 p) {
	float h = dot(p,vec2(127.1,311.7));	
    return fract(sin(h)*437.545);
}
vec3 hash31(float p) {
	vec3 h = vec3(127.231,491.7,718.423) * p;	
    return fract(sin(h)*435.543);
}

float noise_2( in vec2 p ) {
    vec2 i = floor( p );
    vec2 f = fract( p );	
	vec2 u = f*f*(3.0-2.0*f);
    return mix( mix( hash12( i + vec2(0.0,0.0) ), 
                     hash12( i + vec2(1.0,0.0) ), u.x),
                mix( hash12( i + vec2(0.0,1.0) ), 
                     hash12( i + vec2(1.0,1.0) ), u.x), u.y);
}
// 3d noise
float noise_3(in vec3 p) {
    vec3 i = floor(p);
    vec3 f = fract(p);	
	vec3 u = f*f*(3.0-2.0*f);
    
    vec2 ii = i.xy + i.z * vec2(5.0);
    float a = hash12( ii + vec2(0.0,0.0) );
	float b = hash12( ii + vec2(1.0,0.0) );    
    float c = hash12( ii + vec2(0.0,1.0) );
	float d = hash12( ii + vec2(1.0,1.0) ); 
    float v1 = mix(mix(a,b,u.x), mix(c,d,u.x), u.y);
    
    ii += vec2(5.0);
    a = hash12( ii + vec2(0.0,0.0) );
	b = hash12( ii + vec2(1.0,0.0) );    
    c = hash12( ii + vec2(0.0,1.0) );
	d = hash12( ii + vec2(1.0,1.0) );
    float v2 = mix(mix(a,b,u.x), mix(c,d,u.x), u.y);
        
    return max(mix(v1,v2,u.z),0.0);
}

// fBm
float fbm3(vec3 p, float a, float f) {
    return noise_3(p);
}

float fbm3_high(vec3 p, float a, float f) {
    float ret = 0.0;    
    float amp = 1.0;
    float frq = 1.0;
    for(int i = 0; i < 4; i++) {
        float n = pow(noise_3(p * frq),2.0);
        ret += n * amp;
        frq *= f;
        amp *= a * (pow(n,0.2));
    }
    return ret;
}

float rfb(float f, float bps, float l, float h) {
	return l + (h - l) * hash11(floor(f * bps));
}

uint[] text = uint[18](
0xffffffffu, 0x5fffffffu, 0xffffffffu, 0xff7fffffu, 0xffff8e3fu, 0xff9c58c6u, 0x3e39d977u, 0xbfff6b57u, 0xbadfd696u, 0x77bf8f1bu, 0x59badf36u, 0xd875bfffu, 0x7b5ebadeu, 0xf6de8e7fu, 0xff9c51c6u, 0xde19d9ffu, 0xffffffffu, 0xffffffffu);

// 49, 32
uint[] OJ_Roman = uint[49](0xffffffffu, 0xffffffffu, 0xffffffffu, 0xfffe01ffu, 0xf0007ff8u, 0x003fff83u, 0xfff07e07u, 0xffe3fff1u, 0xffe1fff1u, 0xfff1fff8u, 0x3ff8fff1u, 0xfffe1ffcu, 0x7ff1ffffu, 0x87fe3ff0u, 0xffffe1ffu, 0x1ff8ffffu, 0xf8ff8ffcu, 0x7ffffc7fu, 0xc7fc7fffu, 0xfe1fe3feu, 0x3fffff8fu, 0xf1ff1fffu, 0xffc7f8ffu, 0x8fffffe3u, 0xfc7fc7ffu, 0xfff1fe3fu, 0xe3fffff8u, 0xff1ff0ffu, 0xfffc7f8fu, 0xfc7ffffcu, 0x7fc7fe3fu, 0xfffe3fe3u, 0xff0fffffu, 0x1ff1ffc3u, 0xffff1ff8u, 0xfff1ffffu, 0x1ffc7ffcu, 0x3fff1ffeu, 0x3fff0fffu, 0x1ffe1fffu, 0xc1fc1fffu, 0x1ffff000u, 0x3fff1fffu, 0xff00ffffu, 0x1fffffffu, 0xfffc3fffu, 0xffffffffu, 0xffffffffu, 0xffffffffu);

// 184, 13
uint[] shoutouts3 = uint[75](0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xfffffff5u, 0xfffffdefu, 0xebffeffbu, 0xffffffbfu, 0xf7ffffffu, 0xffffeffeu, 0xfffff7ffu, 0xfffdffefu, 0xffdffbffu, 0xffffbfffu, 0xffffffffu, 0xffeffeffu, 0xf9c58c63u, 0xfc698b3fu, 0xdcc8ccdbu, 0x19aff63fu, 0x81cc0e32u, 0xace6dff6u, 0xb57badfdu, 0xab6adfcfu, 0x5b5f5af6u, 0x97f5bfb6u, 0xb5b5d6abu, 0x5abff1b5u, 0x9badfdabu, 0x6a3fdc5bu, 0x5c5b37b7u, 0xf5bfb68du, 0xb5d6a8c6u, 0x3ff7b5ebu, 0xadfdab6au, 0xffdb5b5bu, 0x5bd6b7f5u, 0xbfb6bdb5u, 0xd6abdebfu, 0xf9c51c6du, 0xbc6b8b3bu, 0xdc58dc62u, 0x39b6f637u, 0xb6cdb637u, 0x6ce6dfffu, 0xffffff7fu, 0xfffff7ffu, 0xffffffffu, 0xfdffafffu, 0xffffff7fu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffbfffffu, 0xfffeffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffff00u);
const float EPS = 1e-2;
const int MAX_STEPS = 200;

const float T_INF = -1.0;
const float T_SPHERE = 0.0;
const float T_WALL = 1.0;
const float T_BOX = 2.0;
const float T_ARROW = 3.0;
const float T_BOX2 = 4.0;

    
float reflectiveIndex(float type) 
{
    if (type == T_WALL)
        return 0.3;
	if (type == T_SPHERE)
        return 0.2;
	if (type == T_BOX)
        return 0.5;
	if (type == T_BOX2)
        return 0.0;
	return 0.0;
}

float specularFactor(float type) 
{
    if (type == T_WALL)
        return 0.0;
	return 1.0;
}

float psin(float v) 
{
    return (1.0 + sin(v)) * 0.5;
}

vec3 color(float type, vec3 p) 
{
    int cs = cScene;
	float lt = lTime;

	#define br(o) rfb(iTime + 60 / 140 * o, 140 / 60., 0., 0.2)

    if (type == T_WALL)
        return vec3(0.03);
        //return 0.1*palette(fract(p.x*0.05), vec3(0.5), vec3(0.5), vec3(1.0, 1.0, 1.0), vec3(0.0, 0.1, 0.2) );
    else if (type == T_SPHERE)
        return vec3(0.2, 0.1, 0.6);
    else if (type == T_BOX) {
        vec3 red = vec3(0., 1.0, 0.8);
        vec3 yellow = vec3(1.0, 1.0, 1.0);

        return vec3(0.02);//1.*mix(red, yellow, 0.3*noise_3(2.0*sin(iTime*0.5)*p + iTime*0.5));
    }
    else if (type == T_BOX2) {
        vec3 red = vec3(1., 0.0, 0.0); //+psin(iTime + p.x * 10.)
        vec3 yellow = vec3(1.0, 1.0, 0.0);

		if (cs == 0) {
			vec3 red1 = vec3(1., 1.0, 1.0);
			vec3 red2 = vec3(1., 0.0, 0.0);
			red = red2;
			yellow = vec3(1.0, 1.0, 0.0);
		} else if(cs == 1) {
		    vec3 col1 = vec3(1., 0., 1.0);
			vec3 col2 = vec3(0., 1., 1.);
			red = mix(col1, col2, smoothstep(8., 9., lt));
			yellow = vec3(1.0, 1.0, 0.0);
		} else if(cs == 2) {
			red = vec3(0., 1.0, 1.0);
			yellow = vec3(1.0, 1.0, 0.0);
		}
        
        // return palette(noise_3(0.2*sin(iTime*0.5)*p + iTime*0.5), vec3(0.5), vec3(0.5), vec3(1.0, 0.7, 0.4), vec3(0.00, 0.15, 0.20) );
        return 2.*mix(red, yellow, 0.3*noise_3(2.0*sin(iTime*0.5)*p + iTime*0.5));
   // return palette(0.1, vec3(0.5), vec3(0.5), vec3(1.0, 1.0, 1.0), vec3(0.00, 0.10, 0.20	) );
    }
    else if (type == T_ARROW)
        return vec3(0.1, 0.1, 0.8);
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

float smink( float a, float b, float k )
{
    float h = clamp( 0.5+0.5*(b-a)/k, 0.0, 1.0 );
    return mix( b, a, h ) - k*h*(1.0-h);
}

vec2 sun(vec2 a, vec2 b)
{
    float sm = smink(a.x,b.x, 0.2);
	float m = min(a.x, b.x);
	float ca = abs(sm -a.x);
	float cb = abs(sm -b.x);
	
	return ca < cb ? vec2(sm, a.y) : vec2(m, b.y);
}

vec2 un(vec2 a, vec2 b) { return a.x < b.x ? a : b; }

float rv(float low, float high, float p)
{
    return low + (high - low) * hash11(p);
}


vec2 walls(vec3 p) {
    
 	float d = -sdBox( p - vec3(0.0, 10.0, 0.0), vec3(60.0));
    return vec2(d, T_WALL);
}

vec3 ballPos() {
 	return vec3(0.0, 0.2+sin(0.5*iTime), 0.0);    
}

vec2 grid(in vec3 p, in vec3 dir) {

	int cs = cScene;
	float lt = lTime;
	float tl = lTimeLeft;

    //p.xz *= rot(-0.4+0.07*sin(iTime));
    p.xz *= rot(-0.4+0.07*sin(iTime));

	//moda(p.xz, 5.0);

    vec2 prev = p.xz;
    
	float mixer = cScene == 1 ? 1 + 2.*sin(iTime) : 0.3;
	float rem = sdBox(p, vec3(3., 2.0, 2.2));
    
    float c = 0.37 / 3.;
	// I varje qq
    float sc = 0.045; // 0.078
	float h = 0.1 + 1.*psin(p.x+ 1*iTime);

	if (cs == 0) {
		c = 0.37 / 3 * 0.3;
		sc = 0.045*0.36;
		//rem = sdBox(p, vec3(1.5,  2.0, 1.5));
		rem = sdSphere(p, 1.5);    

	} else if (cs == 1) {
		c = 0.37 / 2.;
		sc = 0.073*1.1;
		rem = mix(sdBox(p, vec3(2., 10.0, 2.0)), sdSphere(p, 2.5), 0.3);    
	} else if (cs == 2) {
		c = 0.37 / 2.;
		sc = 0.073*1.1;
		rem = mix(sdBox(p, vec3(3.0, 2.0, 3.0)), sdSphere(p, 2.5), 0.0);   
	}

    vec3 q = p;
    q.xz = mod(p.xz, c) - 0.5 * c;
    vec3 qq = floor(p/c);
    qq.y = 0.0;
    
	if (cs == 0) {
		h = 0.2 + 0.8*psin(p.x+ 0.5*iTime);
	} else if (cs == 1) {
	    float f2 = smoothstep(20, 25, lt);
		float f = smoothstep(9., 10., lt) * (1. - f2);
		h = 0.1+f*2.*psin(0.6*iTime + qq.z*qq.x*0.1) + f2;
	}else if (cs == 2) {
		h = 1.1;
	}
    
    // TODO: Boundschecking med pDis för optimering.
    //float pDis = sdBox(q, vec3(0.1));
    
    vec3 d = (c * 0.5 -  sign(dir)* q) / abs(dir);
	float b = min(d.x, min(d.y, d.z));
	float a = b + EPS*2.;// max(pDis - 1.73, b + EPS); // TODO 1.73 kan vara for mycket
    
  	float qz = qq.z +15.;
  //  float qx = -qq.x +4. + mod(2.*iTime * 2.0, imDim.x);
    float qx = -qq.x + 23.;

    vec2 imDim;// = vec2(49, 32);
	uint bit;// = uint(qz) * uint(imDim.x) + uint(qx);
    uint val;// = OJ_Roman[bit / 32u] & (1u << (31u - bit % (32u)));
	if (cs == 0) {
		imDim = vec2(49, 32);
		bit = uint(qz) * uint(imDim.x) + uint(qx);
		val = OJ_Roman[bit / 32u] & (1u << (31u - bit % (32u)));
	} else if (cs == 1) {

		bit = uint(qz) * uint(imDim.x) + uint(qx);
		float ojval = OJ_Roman[bit / 32u] & (1u << (31u - bit % (32u)));


	    //qx = -qq.x +4. + mod(2.*lTime * 2.0, imDim.x);
		imDim = vec2(49, 32);
		//bit = uint(qz) * uint(imDim.x) + uint(qx);
		//val = text[bit / 32u] & (1u << (31u - bit % (32u)));
	    //val = noise_2(vec2(qq.x, qq.z) * 0.3 + vec2(iTime, lt < 8. ? iTime * sign(p.x) : 0.)) < 0.8 ? 1u : 0u;
		val = 1u;
		//float x = floor(sin(iTime) * 5.0);
		//float z = floor(cos(iTime) * 5.0);
		//if (qq.x == x && qq.z == z) {
		//	val = 0u;
		//}
		//
		//float x2 = floor(sin(iTime * 2.0) * 7.0);
		//float z2 = floor(cos(iTime * 2.0) * 6.0);
		//if (qq.x == x2 && qq.z == z2) {
		//	val = 0u;
		//}
		//
		//if (qq.x == 0 && qq.z == 0) {
		//	val = 0u;
		//}
		for (int i = 0; i < planets.length(); i++) {
			float x = floor(planets[i].x * 8.0);
			float z = floor(planets[i].z * 8.0);
			if (qq.x == x && qq.z == z) {
				val = 0u;
			}
		}
	} else if (cs == 2) {
		imDim = vec2(184, 13);
	    qx = -qq.x +0.+ mod(4.8*lTime* 2.5, imDim.x);
		qz = qq.z + 6.;
		bit = uint(qz) * uint(imDim.x) + uint(qx);
		val = shoutouts3[bit / 32u] & (1u << (31u - bit % (32u)));
	}



    float t = mod(iTime, 10.);
    vec2 sb;
    //0.1*noise_3(0.5*qq + mod(0.4*iTime, 40.0));//1.1 + (cScene == 2 ? 1.2*noise_3(0.5*qq + mod(0.4*iTime, 40.0)) : 0.);

    //float h = 1.;
    bool cond = qz >=0. && qz <= (imDim.y - 1.) && qx >= 0. && qx <= (imDim.x - 1.);
    if (val != 0u || !cond) {
    	sb.x = max(EPS, a);
    	sb.y = T_BOX;
		if (cs == 0) {
			//h-=0.25*noise_2(qq.xz * 0.5 + iTime*sign(p.x));
		}
    } else {
     	sb.y = T_BOX2;
		if (cs == 0) {
			h-=0.05;
		}
		//h += 0.2*smoothspike(0., 0.3, mod(iTime, 2.));
    }
    
    sb.x = sdBox(q, vec3(sc, h, sc));
	sb.x = min(sb.x, max(EPS, a)); 
    sb.x = max(rem, sb.x);

	return sb;

}

vec2 map(in vec3 p, in vec3 dir)
{

	vec2 w = vec2(p.y - 0.03*noise_2(3.*p.xz + iTime*0.3) +0.3, T_WALL);
	vec2 sb = grid(p, dir);
    return un(w, sb);
}

vec3 normal(vec3 p, vec3 dir) 
{
    float eps = EPS;
    vec3 n = vec3(map(vec3(p.x + eps, p.y, p.z), dir).x, map(vec3(p.x, p.y + eps, p.z), dir).x, map(vec3(p.x, p.y, p.z + eps), dir).x);
    return normalize(n - map(p, dir).x);
}

vec2 march(vec3 ro, vec3 rd, out vec3 p, out int steps)
{
    float t = 0.0;
   	vec2 res = vec2(0.0, -1.0);
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

float ambientOcclusion(vec3 p, vec3 n, vec3 dir) 
{
	float as = 0.0;
    float sl = 60.0 * 1e-3;
    int ns = 6;
    for(int i = 0; i < ns; i++) {
    	vec3 ap = p + float(i) * sl * n;
        vec2 res = map(ap, dir); 
    	if (res.y == T_INF)
            as += sl * float(ns);
        else 
        	as += res.x;
    }
    return mix(1.0, smoothstep(0.0, float(ns *(ns - 1) / 2) * sl, as), 0.6);
}
       
vec3 colorize(vec2 res, vec3 p, vec3 dir, float steps) 
{
	int cs = cScene;
	#define fg(o) rfb(iTime + 60 / 140 * o, 140 / 60., -1., 1)

    vec3 light = normalize(vec3(1.4, -0.8,  1.));

	if (cs == 2) {
	//	light = normalize(vec3(1., -0.7,  1.));
	}

    vec3 lightPos = vec3(-1, 1.5, 0);
    //light = normalize(p - lightPos);
    
    vec3 n = normal(p, dir);
    float lf = min(2.5, 3.0 / (0.1 + 0.1*pow(length(p - lightPos), 3.0)));
    
    // Material properties
    float diffuse1 = max(0.,dot(-light, n));
    float diffuse2 = max(0.,dot(-normalize(p - lightPos), n));
    float diffuse = max(diffuse1, diffuse2);
    float k = max(0.0, dot(dir, reflect(-light, n)));
    float spec = specularFactor(res.y) * pow(k, 100.0);
    
    vec3 col = color(res.y, p);
	float ao = ambientOcclusion(p, n, dir);
   //	float sh = shadow(p, light);
    if (res.x < EPS)
        col =  (lf) * (ao * col *(0.02+diffuse) + spec);
    
    float ns = steps / float(MAX_STEPS);
    return pow(col, vec3(0.4545));
}

void main()
{
    vec2 uv = fragCoord - 0.5;
	uv.y *= iResolution.y / iResolution.x;

    vec3 ro = vec3(4.0, 10.0, 5.0);
    
	float tl = lTimeLeft;
	float lt = lTime;
	float ft = tl + lt;
	int cs = cScene;
	if (cs == 0) {
		float ttt = smoothstep(0, 0.9*ft, lt);
		float cx = mix(20.f, 7.f, ttt);
		ro = vec3(cx, 10.0, 8.);
	} else if (cs == 1) {
		ro = vec3(6., 12. - 3.*smoothstep(8., 9., lt), 6.);
	} else if (cs == 2) {
		ro = vec3(3., 15.0, 5.);
	}
	
	vec3 tar = vec3(0.0, 0.1, 0.0);
    ro = mix(tar, ro, 1.0);
    vec3 dir = normalize(tar - ro);
	vec3 right = normalize(cross(vec3(0.0, 1.0, 0.0), dir));
	vec3 up = cross(dir, right);
	vec3 rd = normalize(dir + right * uv.x + up * uv.y);
    
    vec3 p;
    int steps;
    vec2 res = march(ro, rd, p, steps);
    vec3 col = colorize(res, p, rd, float(steps));

    float ri = reflectiveIndex(res.y);
    if (ri > 0.0) { 
        vec3 p2;
   		rd = reflect(rd, normal(p, rd));
    	res = march(p + 0.1 * rd, rd, p2, steps);
    	vec3 newCol = colorize(res, p2, rd, float(steps));
    	col = mix(col, newCol, ri);
    }

	float f = 1.0;

	if (cs == 0) {
		f = smoothstep(0., 5., lt);
		f *= 1. - smoothstep(1.5, 0., tl);
	} else if (cs == 1) {
		f = smoothstep(0., 1.5, lt);
		f *= 1. - smoothstep(0.5, 0., tl);
	} else if (cs == 2) {
		f = smoothstep(0., 0.5, lt);
		f *= 1. - smoothstep(1.5, 0., tl);
	}

    fragColor = vec4(f * col, 1.0);



}

)""  
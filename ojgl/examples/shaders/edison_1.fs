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
float[] sceneLengths = float[NUM_SCENES](5., 5., 5.);

#define fTime mod(iTime, 15.f)

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


mat3 rotateAngle(vec3 v, float a )
{
    float si = sin( a );
    float co = cos( a );
    float ic = 1.0f - co;

    return mat3( v.x*v.x*ic + co,       v.y*v.x*ic - si*v.z,    v.z*v.x*ic + si*v.y,
                   v.x*v.y*ic + si*v.z,   v.y*v.y*ic + co,        v.z*v.y*ic - si*v.x,
                   v.x*v.z*ic - si*v.y,   v.y*v.z*ic + si*v.x,    v.z*v.z*ic + co );
}


uint[] text = uint[18](
0xffffffffu, 0x5fffffffu, 0xffffffffu, 0xff7fffffu, 0xffff8e3fu, 0xff9c58c6u, 0x3e39d977u, 0xbfff6b57u, 0xbadfd696u, 0x77bf8f1bu, 0x59badf36u, 0xd875bfffu, 0x7b5ebadeu, 0xf6de8e7fu, 0xff9c51c6u, 0xde19d9ffu, 0xffffffffu, 0xffffffffu);

uint[] edison_logo = uint[25](
0xffff000fu, 0xfffc0000u, 0x0fff8000u, 0x000fe000u, 0x00003f00u, 0x000000f8u, 0x003ff003u, 0x000fff80u, 0x109dffffu, 0x0079dfffu, 0xf007bfffu, 0xfff03bffu, 0xffff81bfu, 0xfffffc0fu, 0xffffffe0u, 0xffffffffu, 0x0fffe07fu, 0xf0fffe3fu, 0xfe0fffe3u, 0xffe1fffeu, 0x0ff81fffu, 0xe80007ffu, 0xffa003ffu, 0xffff81ffu, 0xff800000u);

uint[] edison_logo_2 = uint[63](0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffc003ffu, 0xfffffff0u, 0x00003fffu, 0xffffe000u, 0x0003ffffu, 0xff800000u, 0x00ffffffu, 0xc0000000u, 0x3fffffe0u, 0x00ffc00fu, 0xffffc003u, 0xffe007ffu, 0xffc277ffu, 0xfc03ffffu, 0xde77fffcu, 0x01fffffeu, 0xffffffc0u, 0xfffffeffu, 0xffffe07fu, 0xfffeffffu, 0xfff03fffu, 0xffffffffu, 0xf83fffffu, 0xfffffffcu, 0x3fffffffu, 0xf81ffc3fu, 0xfffffff8u, 0xfff83fffu, 0xfffff8ffu, 0xf87fffffu, 0xfff83fe0u, 0x7fffffffu, 0xfa0001ffu, 0xfffffffeu, 0x800fffffu, 0xffffffe0u, 0x7fffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xfe635f18u, 0xe31718feu, 0xd6afb5aeu, 0xbbdbbf0bu, 0x6edad74du, 0xeddfb46bu, 0xe31baef6u, 0xefdad5f7u, 0xb631118fu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffe00000u);

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
    if (type == T_WALL)
        //return vec3(0.03);
        return 0.1*palette(fract(p.x*0.05), vec3(0.5), vec3(0.5), vec3(1.0, 1.0, 1.0), vec3(0.00, 0.10, 0.20	) );
    else if (type == T_SPHERE)
        return vec3(0.2, 0.1, 0.6);
    else if (type == T_BOX) {
        vec3 red = vec3(0., 1.0, 0.8);
        vec3 yellow = vec3(1.0, 1.0, 1.0);

        return vec3(0.02);//1.*mix(red, yellow, 0.3*noise_3(2.0*sin(iTime*0.5)*p + iTime*0.5));
    }
    else if (type == T_BOX2) {
        vec3 red = vec3(0.2, 0.0, psin(iTime + p.x * 10.));
        vec3 yellow = vec3(1.0, 1.0, 1.0);
        
        // return palette(noise_3(0.2*sin(iTime*0.5)*p + iTime*0.5), vec3(0.5), vec3(0.5), vec3(1.0, 0.7, 0.4), vec3(0.00, 0.15, 0.20) );
       // return mix(red, yellow, 0.3*noise_3(2.0*sin(iTime*0.5)*p + iTime*0.5));
    return palette(0.1, vec3(0.5), vec3(0.5), vec3(1.0, 1.0, 1.0), vec3(0.00, 0.10, 0.20	) );
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


// O
float sdTorus(vec3 p, vec2 t)
{
  p.y *= 0.7;
  p.zy = p.yz;
  vec2 q = vec2(length(p.xy)-t.x,p.z);
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

float note() {
	return mod(iTime, 5.0);     
}


/*vec2 map(vec3 p)
{
    vec2 sphere = vec2(length(p - vec3(7.5 - 3.5*psin(iTime) + noise_3(p), 0.0, 0.0)) - 0.5, T_SPHERE);
    
    float ns = 0.2*noise_3(4.0 *( p - vec3(0.5*iTime, 0.1 * iTime, 0.2 * iTime)));
    vec2 box = vec2(sdBox(    p - vec3(3.0, 0.0, 0.0), vec3(1.5, 0.5, 0.5) + ns), T_BOX);
    vec2 walls = vec2(-sdBox( p - vec3(0.0, 0.0, 0.0), vec3(8.0)), T_WALL);
	vec2 arr = vec2(arrows(p), T_ARROW);
    return un(arr, un(box, un(sphere, walls)));
}*/


vec2 fractalBox(in vec3 p)
{
   float d = sdBox(p,vec3(1.0)) - 0.0;
   //d = mix(sdSphere(p, 1.0),d, 0.5 + 0.6*psin(iTime));
    
   vec2 res = vec2( d, T_BOX);

   float s = 0.34 + 0.66*psin(iTime*0.5);
   for( int m=0; m<3; m++ )
   {
      vec3 newp = p;
      vec3 a = mod( newp * s, 2.0 ) - 1.0;
      s *= 3.0;
      vec3 r = abs(1.0 - 3.0*abs(a));

      float da = max(r.x,r.y);
      float db = max(r.y,r.z);
      float dc = max(r.z,r.x);
      float c = (min(da,min(db,dc))-1.0)/s;

      if( c>d )
      {
          d = c;
          res = vec2( d, T_BOX);
       }
   }

   return res;
}

vec2 walls(vec3 p) {
    
 	float d = -sdBox( p - vec3(0.0, 10.0, 0.0), vec3(60.0));
    return vec2(d, T_WALL);
}

vec3 ballPos() {
 	return vec3(0.0, 0.2+sin(0.5*iTime), 0.0);    
}

vec2 map(in vec3 p, in vec3 dir)
{
    vec2 w = vec2(p.y - 0.03*noise_2(3.*p.xz + iTime*0.3) +0.3, T_WALL);
    float bd = sdBox(p, vec3(1., 2., 1.));
    vec2 s = vec2(sdSphere(p - ballPos(), 1.0), T_BOX);
    //vec2 w = vec2(sdPlane(p, vec4(0.0, 1.0, 0.0, 1.0)) + 0.1*noise_3(p + vec3(20, 0.0, 20)), T_WALL);
    // + 0.01*noise_2(10.0*p.xz + vec2(iTime, iTime*1.5)) Ripple effect.
   // vec2 fs = fractalBox(   (p - ballPos()));
    p.xz *= rot(-0.3+0.1*sin(iTime));
    vec2 prev = p.xz;
    //float m = mod(floor((iTime+1.9) * 1. / 2.), 2.);

    //moda(p.xz, m == 0.0 ? 1.0 : 10.0);
    //p.xz = mod(p.xz, vec2(2.0)) - vec2(1.0);
    //p.x -= 0.0;
    //p.x -= 3.0;
    //mo(p.xy, vec2(sin(iTime), 2.*cos(iTime)));
    //float oj = sdTorus(p - vec3(sin(iTime), 0.18, 0.0), vec2(1.0, 0.02));
   // oj = min(99999999999.9, sdTorusJ(p, vec2(1.0, 0.02)));
   // vec2 fs = vec2(oj, T_BOX);
    
    //fs.x = mix(sdBox(p - ballPos(), vec3(1.0)), fs.x, 0.2+0.8*psin(0.5*iTime));
    
    
    //vec2 box = vec2(sdBox( p - ballPos(), vec3(0.5, 0.5, 0.5) + ns), T_BOX);
    
    
	float rem = mix(sdBox(p, vec3(2., 10.0, 2.0)), sdCappedCylinder(p, vec2(2.5, 10.5)), 1 + 2.*sin(iTime));    
    
    float c = 0.37 / 2. / (1. + 1.);
    vec3 q = p;
    q.xz = mod(p.xz, c) - 0.5 * c;
    vec3 qq = floor(p/c);
    qq.y = 0.0;
    
   // vec2 sb = vec2(p.y - 0.6*noise_3(0.5*qq), T_BOX);
    
    // I varje qq
    float sc = 0.073; // 0.078
    
   //  float t = floor(p.x / c);

    //if (mod(t, 2.0) == 0. && sb.x < EPS)
      //  	sb.x = 999.0;
    
    
    // TODO: Boundschecking med pDis f�r optimering.
    //float pDis = sdBox(q, vec3(0.1));
    
    vec2 imDim = vec2(57, 35);
    vec3 d = (c * 0.5 -  sign(dir)* q) / abs(dir);
	float b = min(d.x, min(d.y, d.z));
	float a = b + EPS;// max(pDis - 1.73, b + EPS); // TODO 1.73 kan vara for mycket
    
  	float qz = qq.z +15.;
    float qx = -qq.x +4. + mod(2.*iTime * 2.0, imDim.x);
    uint bit = uint(qz) * uint(imDim.x) + uint(qx);
    uint val = edison_logo_2[bit / 32u] & (1u << (31u - bit % (32u)));
   
    float t = mod(iTime, 10.);
    vec2 sb;
    float h = 0.1; +  0.2*psin(0.6*iTime + qq.z*qq.x*0.1);// + 0.0*noise_3(0.5*qq + mod(0.4*iTime, 40.0));
    
    //float h = 1.;
    bool cond = qz >=0. && qz <= (imDim.y - 1.) && qx >= 0. && qx <= (imDim.x - 1.);
    if (val != 0u || !cond) {
    	sb.x = max(EPS, a);
    	sb.y = T_BOX;
    } else {
     	sb.y = T_BOX2;
        h += 0.2*smoothspike(0., 0.3, mod(iTime, 2.));
    }
    
    

    sb.x = sdBox(q, vec3(sc, h, sc));
    
    //sb.x = mod(qq.x, 2.0) == 0. ? sb.x : max(EPS, a);
	sb.x = min(sb.x, max(EPS, a));

        
    sb.x = max(rem, sb.x);
    
    
    /*
	vec3 s = (d * 0.5 -  sign(dir)* q) / abs(dir);
    
    float b = min(s.x, min(s.y, s.z));
	if(val > 0u || part.x >= offsets[row+1] || originalPart.x < 0.0 || originalPart.y < 0.0){
		dis = max(b + EPSILON * 2.0, dis);
	}*/
    
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
       

float ao(vec3 p, vec3 n, vec3 dir) {
 
	float ao;
	float totao = 0.0;
	float sca = 1.0;
    for( int aoi=0; aoi<5; aoi++ ) {
    	float hr = 0.01 + 0.02*float(aoi*aoi);
    	vec3 aopos = n * hr + p;
        float dd = map( aopos, dir ).x;
        ao = -(dd-hr);
        totao += ao*sca;
        sca *= 0.75;
        }
     ao = 1.0 - clamp( totao, 0.0, 1.0 );

     return ao;
}

float rfb(float f, float bps, float l, float h) {
	return l + (h - l) * hash11(floor(f * bps));
}

vec3 colorize(vec2 res, vec3 p, vec3 dir, float steps) 
{
	#define fg(o) rfb(iTime + 60 / 140 * o, 140 / 60., -1., 1)

    vec3 light = normalize(vec3(1., -1.,  1.));
    vec3 lightPos = vec3(-1, 1.5, 0.);
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
    
	if (cScene == 1) {
		ro.x = 10.;
	}
	
	vec3 tar = vec3(0.0, 0.7, 0.0);
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

    fragColor = vec4(col, 1.0);
}

)""  
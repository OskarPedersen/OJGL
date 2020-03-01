R""(
#version 430

in vec2 fragCoord;
layout(location = 0) out vec4 fragPos;
layout(location = 1) out vec4 fragNormal;

uniform float iTime;
uniform vec2 iResolution;

#define PI 3.1415

#define MAT_FLOOR 1.0
#define MAT_AUDIENCE 2.0
#define MAT_SCENE 3.0

vec2 un(vec2 res1, vec2 res2) {
	if (res1.x < res2.x) {
		return res1;
	} else {
		return res2;
	}
}

void moda (inout vec2 p, float rep)
{
    float per = 2.*PI/rep;
    float a = atan(p.y, p.x);
    float l = length(p);
    a = mod(a-per*0.5,per)-per*0.5;
    p = vec2(cos(a),sin(a))*l;  
}

float udRoundBox( vec3 p, vec3 b) {
  float r = 0.5;
  return length(max(abs(p)-b,0.0))-r;
}

float sdBox( vec3 p, vec3 b )
{
  vec3 q = abs(p) - b;
  return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

mat2 rot(float a)
{
    return mat2(cos(a),sin(a),-sin(a),cos(a));
}

float fractalBox(vec3 p) {
  float d = sdBox(p, vec3(1.0));

  moda(p.xz, 10.0);
  moda(p.xy, 10.0);
  p.yz = rot(iTime)*p.yz;

   float s = 1.0;
   for( int m=0; m<10; m++ )
   {
      vec3 a = mod( p*s, 2.0 )-1.0;
      s *= 3.0;
      vec3 r = abs(1.0 - 3.0*abs(a));

      float da = max(r.x,r.y);
      float db = max(r.y,r.z);
      float dc = max(r.z,r.x);
      float c = (min(da,min(db,dc))-1.0)/s;

      d = max(d,c);
   }
   return d;
}

vec2 audience(vec3 p) {
	float s = 2.0;
	float qx = mod(p.x + s * 0.5, s) - s * 0.5;
	float qz = mod(p.z + s * 0.5, s) - s * 0.5;
	vec3 q = vec3(qx, p.y, qz);
	return vec2(sdBox(q, vec3(0.5, 2.0, 0.5)), MAT_AUDIENCE);
}

vec2 scene(vec3 p) {
	vec3 t = p - vec3(0, 0, 50);
	float dBase = sdBox(t, vec3(20, 4, 5));
	float d = dBase;

	float dTower = sdBox(t - vec3(0, 0, 2), vec3(5, 50, 2));
	d = min(d, dTower);

	{
		vec3 q = t;
		float s = 5.0;
		q.x = mod(q.x + s/2, s) - s/2;
		float dPillars = sdBox(q, vec3(1, 10, 1));
		float dLight = sdBox(q - vec3(0, 8, -1), vec3(0.5, 0.5, 0.5));
		float dRes = max(dPillars, -dLight);
		d = min(d, dRes);
	}

	return vec2(d, MAT_SCENE);
}

vec2 map(vec3 p) {
	vec2 res = vec2(p.y, MAT_FLOOR);
	res = un(res, audience(p));
	res = un(res, scene(p));
	return res;
}

void main(){
	const vec2 uv = fragCoord.xy;
    const vec3 ro = vec3(0.0, 10.0, 2.5);
    const vec3 rd = normalize(vec3(uv.x - 0.5, uv.y - 0.5 - 0.25, 1.0));
	// TODO: proper target etc
    float t = 0.0;
    vec3 color = vec3(0.0);
    
    for (int i = 0; i < 1000; i++) {
    	vec3 p = ro + rd * t;
        float d = map(p).x;
        
        if (d < 0.01) {
            color = vec3(1.0, 0.0, 0.0);
            
            vec3 normal;
            const vec3 ep = vec3(0.01, 0, 0);
            normal.x = map(p + ep.xyz).x - map(p - ep.xyz).x;
            normal.y = map(p + ep.yxz).x - map(p - ep.yxz).x;
            normal.z = map(p + ep.yzx).x - map(p - ep.yzx).x;
            normal = normalize(normal);
            
			fragNormal.rgb = normal;
			fragPos.rgb = p;
            break;
        }
        t += d;
    }    

	fragPos.a = 1.0;
	fragNormal.a = 1.0;
}

)""
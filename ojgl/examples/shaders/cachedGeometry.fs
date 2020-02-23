R""(
#version 430

in vec2 fragCoord;
layout(location = 0) out vec4 fragPos0;
layout(location = 1) out vec4 fragNormal0;
layout(location = 2) out vec4 fragPos1;
layout(location = 3) out vec4 fragNormal1;
layout(location = 4) out vec4 fragPos2;
layout(location = 5) out vec4 fragNormal2;
layout(location = 6) out vec4 fragPos3;
layout(location = 7) out vec4 fragNormal3;


uniform float iTime;
uniform vec2 iResolution;

#define PI 3.1415

void moda (inout vec2 p, float rep)
{
    float per = 2.*PI/rep;
    float a = atan(p.y, p.x);
    float l = length(p);
    a = mod(a-per*0.5,per)-per*0.5;
    p = vec2(cos(a),sin(a))*l;  
}

float udRoundBox( vec3 p, vec3 b) {
  float r = 0.0;
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

// Repeat in two dimensions
vec2 pMod2(inout vec2 p, vec2 size) {
	vec2 c = floor((p + size*0.5)/size);
	p = mod(p + size*0.5,size) - size*0.5;
	return c;
}

// Repeat in three dimensions
vec3 pMod3(inout vec3 p, vec3 size) {
	vec3 c = floor((p + size*0.5)/size);
	p = mod(p + size*0.5, size) - size*0.5;
	return c;
}

float map (vec3 p) {
    vec3 q = p;
	pMod2(q.xz, vec2(4.0));
	return udRoundBox(q, vec3(1.0));
}

vec3 normal(vec3 p) {
	vec3 normal;
    const vec3 ep = vec3(0.01, 0, 0);
    normal.x = map(p + ep.xyz) - map(p - ep.xyz);
    normal.y = map(p + ep.yxz) - map(p - ep.yxz);
    normal.z = map(p + ep.yzx) - map(p - ep.yzx);
    return normalize(normal);
}

void main(){
	for (int xx = 0; xx < 2; xx++) {
		for (int yy = 0; yy < 2; yy++) {
			float u = (fragCoord.x + (xx - 0.5) / iResolution.x) * 2.0 - 1.0;
			float v = (fragCoord.y + (yy - 0.5) / iResolution.y) * 2.0 - 1.0;

			vec3 eye = vec3( 0.0, 3.0, 0.0); 
			vec3 tar = eye + vec3(1.0, -1.0, 0.0);
    
			vec3 lol = vec3(0, 1, 0);

			vec3 dir = normalize(tar - eye);
			vec3 right = normalize(cross(lol, dir));
			vec3 up = cross(dir, right);

			vec3 ro = eye;
			vec3 rd = normalize(dir + right*u + up*v);


			float t = 0.0;
			vec3 color = vec3(0.0);
    
			for (int i = 0; i < 1000; i++) {
    			vec3 p = ro + rd * t;
				float d = map(p);
        
				if (d < 0.01) {
            
					const vec3 lpos = ro + vec3(-1.0, 0, 0);
					const float dis = length(lpos - p);
					const vec3 invLight = normalize(lpos - p);

					if (xx == 0 && yy == 0) {
						fragNormal0.rgb = normal(p);
						fragPos0.rgb = p;
					} else if (xx == 1 && yy == 0) {
						fragNormal1.rgb = normal(p);
						fragPos1.rgb = p;
					} else if (xx == 0 && yy == 1) {
						fragNormal2.rgb = normal(p);
						fragPos2.rgb = p;
					} else if (xx == 1 && yy == 1) {
						fragNormal3.rgb = normal(p);
						fragPos3.rgb = p;
					}
					break;
				}
				t += d;
			}    
		}
	}

	fragPos0.a = 1.0;
	fragPos1.a = 1.0;
	fragPos2.a = 1.0;
	fragPos3.a = 1.0;
	fragNormal0.a = 1.0;
	fragNormal1.a = 1.0;
	fragNormal2.a = 1.0;
	fragNormal3.a = 1.0;
	
}

)""
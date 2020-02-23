R""(
#version 430

in vec2 fragCoord;
out vec4 fragColor;

uniform sampler2D inTexture0;
uniform sampler2D inTexture1;
uniform sampler2D inTexture2;
uniform sampler2D inTexture3;
uniform sampler2D inTexture4;
uniform sampler2D inTexture5;
uniform sampler2D inTexture6;
uniform sampler2D inTexture7;
uniform float iTime;
uniform vec2 iResolution;


float udRoundBox( vec3 p) {
  float r = 0.0;
  vec3 b = vec3(0.3);
  return length(max(abs(p)-b,0.0))-r;
}

float psin(float x) {
	return (1.0 + sin(x)) * 0.5;
}

// Repeat in two dimensions
vec2 pMod2(inout vec2 p, vec2 size) {
	vec2 c = floor((p + size*0.5)/size);
	p = mod(p + size*0.5,size) - size*0.5;
	return c;
}

void main()
{
	vec3 color = vec3(0.0);
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



			vec3 pos; 
			vec3 normal;
			if (xx == 0 && yy == 0) {
				pos = texture(inTexture0, fragCoord.xy).rgb;
				normal = texture(inTexture1, fragCoord.xy).rgb;
			} else if (xx == 1 && yy == 0) {
				pos = texture(inTexture2, fragCoord.xy).rgb;
				normal = texture(inTexture3, fragCoord.xy).rgb;
			} else if (xx == 0 && yy == 1) {
				pos = texture(inTexture4, fragCoord.xy).rgb;
				normal = texture(inTexture5, fragCoord.xy).rgb;
			} else if (xx == 1 && yy == 1) {
				pos = texture(inTexture6, fragCoord.xy).rgb;
				normal = texture(inTexture7, fragCoord.xy).rgb;
			}

			vec3 lpos = vec3(0.0, 10.0, 0.0);
			{
				const float r = 0.0;
				const float speed = 1.0;
				lpos += vec3(cos(iTime * speed) * r, 0.1, sin(iTime * speed) * r);
			}

			vec3 qpos = pos;
			pMod2(qpos.xz, vec2(100.0));
			const float dis = length(lpos - qpos);
			const vec3 invLight = normalize(lpos - qpos);


			const float diffuse = max(0.0, dot(invLight, normal));
			const float s = 10.0;
			const float k = max(0.0, dot(rd, reflect(invLight, normal)));
			const float spec =  pow(k, s);
			const float str = 10000.0/(dis*dis*dis);
			//vec3(0.5, psin(iTime),psin(iTime + 1.6));
			color += vec3(0.0 + 1.0*diffuse*str);// + vec3(spec*str);
		}
	}
	

	// Final color
	fragColor.rgb = color / 4.0;
	fragColor.a = 1.0;
}
)""
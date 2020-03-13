R""(
#version 430

in vec2 fragCoord;
out vec4 fragColor;

uniform sampler2D inTexture0;
uniform sampler2D inTexture1;
uniform sampler2D inTexture2;
uniform sampler2D inTexture3;
uniform float iTime;


float udRoundBox( vec3 p) {
  float r = 0.0;
  vec3 b = vec3(0.3);
  return length(max(abs(p)-b,0.0))-r;
}

float sdCylinder( vec3 p, vec3 c )
{
  return length(p.xz-c.xy)-c.z;
}

float psin(float x) {
	return (1.0 + sin(x)) * 0.5;
}
void main()
{
	vec2 uv = fragCoord.xy;

	vec3 pos =  texture(inTexture0, uv).rgb;
	vec3 normal =  texture(inTexture1, uv).rgb;

	vec3 pos2 =  texture(inTexture2, uv).rgb;
	vec3 normal2 =  texture(inTexture3, uv).rgb;

	const vec3 ro = vec3(0.0, 10.0, 2.5);
    const vec3 rd = normalize(vec3(uv.x - 0.5, uv.y - 0.5 - 0.25, 1.0));

	// Additional geometry
    //vec3 ro = vec3(iTime, 0.0, 2.5);
    //vec3 rd = normalize(vec3(1.0, uv.y - 0.5, uv.x - 0.5));
    //float t = 0.0;
	//
    //for (int i = 0; i < 100; i++) {
    //	const vec3 p = ro + rd * t;
    //    const vec3 q = mod(p, 5.0) - 2.5;
    //    const vec3 r = p / 8.0;
    //    const  float d = udRoundBox(q);
    //    
	//	// The "length(pos) > t" check determines that we hit this geometry instead of the precalculated
    //    if (d < 0.01 && length(pos) > t) {
	//
    //        
    //        vec3 lpos = ro + vec3(-1.0, 0, 0);
    //        float dis = length(lpos - p);
    //        vec3 invLight = normalize(lpos - p);
    //        
    //        vec3 n;
    //        const  vec3 ep = vec3(0.01, 0, 0);
    //        n.x = udRoundBox(q + ep.xyz) - udRoundBox(q - ep.xyz);
    //        n.y = udRoundBox(q + ep.yxz) - udRoundBox(q - ep.yxz);
    //        n.z = udRoundBox(q + ep.yzx) - udRoundBox(q - ep.yzx);
    //        n= normalize(n);
    //        
	//		normal = n;
	//		pos = p;
    //        break;
    //    }
    //    t += d;
    //}


	// Lightning
	//vec3 lpos = vec3(0.0, 6.0, 40.0);
	//{
	//	float s = 5.0;
	//	pos.x = mod(pos.x + s * 0.5, s) - s * 0.5;
	//}
	//
    //const float dis = length(lpos - pos);
    //const vec3 invLight = normalize(lpos - pos);
	//
	//const float diffuse = max(0.0, dot(invLight, normal));
    //const float s = 10.0;
    //const float k = max(0.0, dot(rd, reflect(invLight, normal)));
    //const float spec =  pow(k, s);
    //float str = 20.0/(0.1 + 0.1*dis + 0.1*dis*dis);
	//{
	//	float cycle = 0.5;
	//	str *= 1.2 - mod(iTime, cycle) / cycle;
	//}
	//vec3 color = vec3(1.0, 0.0, 0.0);
    //color = color * (0.0 + 1.0*diffuse*str) + vec3(spec*str);

	struct Light {
		vec3 pos;
		vec3 col;
		float str;
	};

	Light lights[] =  Light[](
		Light(vec3(0.0,   8.0, 40.0),   vec3(1.0, 0.0, 0.0), 200.0),
		Light(vec3(5.0,   8.0, 40.0),   vec3(0.0, 0.0, 1.0),  200.0),
		Light(vec3(-5.0,  8.0, 40.0),  vec3(0.0, 1.0, 0.0),  200.0),
		Light(vec3(15.0,  8.0, 40.0),  vec3(0.0, 0.0, 1.0), 200.0),
		Light(vec3(-15.0, 8.0, 40.0), vec3(0.0, 1.0, 0.0), 200.0)
	);

	vec3 normals[] = vec3[](normal, normal2);
	vec3 poss[] = vec3[](pos, pos2);

	vec3 colorSum = vec3(0.0);



	for (int jump = 0; jump < 2; jump++) {
		const vec3 cn = normals[jump];
		const vec3 cp = poss[jump];
		float refStr = 0.8;
		if (jump == 1) {
			refStr = 0.2;
		}

		for (int i = 0; i < lights.length(); i++) {
			Light l = lights[i];


			const float dis = length(l.pos - cp);
			const vec3 invLight = normalize(l.pos - cp);
		
			const float diffuse = max(0.0, dot(invLight, cn));
			const float s = 10.0;
			const float k = max(0.0, dot(rd, reflect(invLight, cn)));
			const float spec =  pow(k, s);
			float str = l.str/(dis * dis * dis);
			{
				float cycle = 0.5;
				str *= 1.2 - mod(iTime, cycle) / cycle;
			}

			
			colorSum += (l.col * (0.0 + 1.0*diffuse*str) + vec3(spec*str)) * refStr;
		}
	
		float t = 0.0;
	
		vec3 scatteredLight = vec3(0.0);
		float transmittance = 1.0;
		const int maxIter = 100;
		for (int i = 0; i < maxIter; i++) {
			const vec3 p = ro + rd * t;

			
			 // *** evaluateLight ***
			 //float lightDis = length(p - vec3(0, 5, 50));
			 vec3 ptrans = p - vec3(10, 8, 50);
			 float lightDis = length(ptrans.xy);//sdCylinder(ptrans.yzx, vec3(0, 10, 0));
			 float sourceDis = length(ptrans);
			 float str = 100000.0 /  (sourceDis * sourceDis * sourceDis);
			 if (lightDis > 0.1) {
				str *= 1.0 - smoothstep(0.1, 0.2, lightDis);
			 }
			 if ( p.z > 50) {
				str = 0.0;
			 }
			 vec3 light = str * vec3(1.0, 0.0, 0.0);// / (lightDis * lightDis * lightDis);

			 float fogAmount = 0.01;

			 float d = max(0.1, lightDis * 0.25);

			 vec3 lightIntegrated = light - light * exp(-fogAmount * d);
			 scatteredLight += transmittance * lightIntegrated;	
			 transmittance *= exp(-fogAmount * d);

			if (t > length(cp) || i == maxIter - 1) {
				colorSum  = transmittance * colorSum + scatteredLight;
				//colorSum = vec3(1.0, 0.0, 0.0);
				break;
			}

			t += d;
		}
	}

	{
	}

	// Final color
	fragColor.rgb = colorSum;
	fragColor.a = 1.0;
}
)""
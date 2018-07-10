R""(
#version 430

in vec2 fragCoord;
out vec4 fragColor;

uniform sampler2D inTexture0;
uniform float iGlobalTime;

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








vec2 un(vec2 a, vec2 b)
{
	if(a.x < b.x) 
	{
		return a;
	}
	else
	{
		return b;
	}
}

float pyrField(vec2 p, float base, float h )
{
	vec2 pyr = min(mod(p, base), base -  mod(p, base));
	return min(pyr.x, pyr.y) * h;
}

vec2 mountains(vec2 p)
{
	p *= 0.5;
	float t = 5.0 * texture(inTexture0, p.xy/50.0).x + 
				50.0 * texture(inTexture0, p.xy/300.0).x + 
				200.0 * texture(inTexture0, p.xy/6000.0).x;
    /*float t = 5.0 * noiseOctave(p.xy / 50.0, 5, 0.7) +
        		50.0 * noiseOctave(p.xy / 300.0, 5, 0.7) + 
        		200.0 * noiseOctave(p.xy / 6000.0, 5, 0.7);*/

	float pyr = max(pyrField(p, 100.0, 1.3), pyrField(p + 10.0, 90.0, 1.3));
	pyr = max(pyr, pyrField(p + 60.0, 110.0, 1.3));
	float h = t + pyr;
	
	
	return vec2(h, 1.0);
}



vec2 scene(vec3 p, float t, vec3 rd)
{  
	vec2 res = vec2(99999999.0, -1.0);
	if (p.y > 275.0 || rd.y > 0.2) { 
		return res;
	}
	vec2 m = mountains(p.xz);
	res = un(res, vec2(p.y -m.x, m.y));
    return res;
}


vec3 getNormal(vec3 p, float t, vec3 rd)
{
    vec3 normal;
    vec3 ep = vec3(0.1,0,0);
    normal.x = scene(p + ep.xyz, t, rd).x - scene(p - ep.xyz, t, rd).x;
    normal.y = scene(p + ep.yxz, t, rd).x - scene(p - ep.yxz, t, rd).x;
    normal.z = scene(p + ep.yzx, t, rd).x - scene(p - ep.yzx, t, rd).x;
    return normalize(normal);
}

vec3 getNormalEps(vec3 p, float t, vec3 rd, float eps)
{
    vec3 normal;
    vec3 ep = vec3(eps,0,0);
    normal.x = scene(p + ep.xyz, t, rd).x - scene(p - ep.xyz, t, rd).x;
    normal.y = scene(p + ep.yxz, t, rd).x - scene(p - ep.yxz, t, rd).x;
    normal.z = scene(p + ep.yzx, t, rd).x - scene(p - ep.yzx, t, rd).x;
    return normalize(normal);
}



vec3 camdir(float iGlobalTime)
{
	return vec3(0,0,-1);
}

vec3 applyFog(vec3 rgb, float dis, vec3 rayDir, vec3 sunDir, vec3 p)
{
	float disFog = 1.0 - exp(-dis*0.003);
	float heightFog = 1.0 - smoothstep(150.0, 200.0, p.y);
	float fogAmount = max(disFog, heightFog);
	float sunAmount = max(0.0, dot(rayDir, sunDir));
	vec3 fogColor = mix(vec3(0.8), vec3(1.0,0.9,0.7), pow(sunAmount, 8.0)); // 12.0
	return mix(rgb, fogColor, fogAmount);
}

float specular(vec3 normal, vec3 light, vec3 viewdir, float s)
{
	float nrm = (s + 8.0) / (3.1415 * 8.0);
	float k = max(0.0, dot(viewdir, reflect(light, normal)));
    //return pow(max(dot(reflect(eye,normal),light), 0.0), 8.0);
    return pow(k, s);
}

vec4 evaluateLight(vec3 p)
{
	float dis = length(p + vec3(-300.0, -150.0 + sin(iGlobalTime) * 50.0, 0.0)) - 1.0;
	vec3 col = vec3(1.0, 0.6, 0.6);
	float strength = 10000.0;

	vec3 res = col * strength / (dis * dis * dis);
	//return vec4(res, dis);
    
	vec3 col2 = vec3(0.8);
	float strength2 = 1.0;

	vec3 res2 = col2 * strength2;
	return vec4(res + res2, dis);
}

#define PI 3.1415
void main()
{

    
    vec2 iMouse = vec2(160.5, 0.0);
	vec3 eye = vec3(100.0*sin(iMouse.x * 0.01), 250.0, 100.0*cos(iMouse.x  * 0.01));
	vec3 tar = vec3(eye.x * 1.1, eye.y - 2.0, eye.z * 1.1);
	 vec3 dir = normalize(tar - eye);
	vec3 right = normalize(cross(vec3(0, 1, 0), dir)); 
 	vec3 up = cross(dir, right);

    
    float f = 1.5;

    /*float u = (fragCoord.x / iResolution.x) * 2.0 - 1.0;
    float v = ((fragCoord.y / iResolution.y) * 2.0 - 1.0) * (iResolution.y/iResolution.x);*/
	float u = fragCoord.x * 2.0 - 1.0;
	float v = fragCoord.y * 2.0 - 1.0;
    


    vec3 color = vec3(0.8); 
   	vec3 ambient = vec3(0.2, 0.5,0.1);
    vec3 invLight = -normalize(vec3(1, -0.2, 0)); 
    bool sky = true;
           
    float t = 0.0;
    
    vec3 ro = eye;	
    vec3 rd = normalize(dir + right*u + up*v);

    vec3 scatteredLight = vec3(0.0);
	float transmittance = 1.0;
    
	float ref = 0.0;

	 for(int i = 0; i < 1000 && t < 1000.0; ++i) 
	 {
	   	vec3 p = ro + rd * t;
	    vec2 dm = scene(p, iGlobalTime, rd);
	    float d = dm.x;
	    float m = dm.y;
         
         float fogAmount = 0.0008 + 0.03 * (1.0 - smoothstep(100.0, 150.0, p.y));
         
         vec4 lightColDis = evaluateLight(p);
         vec3 light = lightColDis.rgb;
         d = min(d, lightColDis.w);
         
         vec3 lightIntegrated = light - light * exp(-fogAmount * d);
         scatteredLight += transmittance * lightIntegrated;	
         transmittance *= exp(-fogAmount * d);
         
		
         
	    if(d < 0.01) 
	    {
	    	float spec = 1.0;
	    	vec3 normal = getNormal(p, iGlobalTime, rd);
	    	
	    	if (m == 1.0) //mountain
	    	{
	    		
	    		vec3 n = getNormalEps(p, iGlobalTime, rd, 1.0);		
	    		//color = mix(vec3(normal.y > 0.38 ? 0.9 : 0), vec3(n.y), 0.5);
	    		
	    		color =  mix(vec3(smoothstep(0.6, 0.9, normal.y) * 0.7), vec3(n.y), 0.2);
	    		spec = normal.y;
	
            }
			
            
            
			float diffuse = max(0., dot(invLight, normal)); 
			color = 0.7 * color * (1.0 + diffuse);
	    	color += spec * specular(normal, -invLight, normalize(eye - p), 70.0);
		   	//color =  applyFog(color, distance(eye, p), rd, invLight, p);

            color = transmittance * color + scatteredLight;
	    	
	        sky = false;
	       	break;
	    }
	    t += max(d*0.2, 0.01);
	 }
	
	//if (sky) {
		//t = (2000 - ro.y)/rd.y;
		//if(t > 0) {
			/*float px = ro.x + t * rd.x;
			float pz = ro.z + t * rd.z;
			float realTex = texture(iChannel0, vec2(px, pz)*0.0001).x;
			float dis = 0.02*sqrt(px*px + pz*pz);
			realTex = smoothstep(0.6, 0.8, realTex);
			color = vec3(realTex);
			
			color = mix(color, vec3(0.4, 0.4, 1), 0.8)*2;
			color = applyFog(color, dis, rd, invLight, ro + t*rd);*/
			
			// Northen lights
			/*float a = atan(rd.z, rd.x) + PI;
			a /= 2.0 *  PI;
			float b = rd.y;
			float timeMul = 0.03;
			float spaceMul = 3.0;
			a *= spaceMul;
			b *= spaceMul;
			float raw = texture(iChannel0, vec2(a + iGlobalTime * timeMul, b + iGlobalTime * timeMul)).x 
				+ texture(iChannel0, vec2(a - iGlobalTime * timeMul, b - iGlobalTime * timeMul)).x;
			raw = 2 - raw;
			raw /= 2.0;
			raw = pow(raw, 8.0);
			color = vec3(0.0, raw * 1.5, 0.0);*/
		//} 
	//}
	//color =  applyFog(color, distance(eye, ro + rd * t), rd, invLight, ro + rd * t);
    //color = vec3(texture(iChannel0, vec2(u, v) * 1.0).x);
    
    color /= (color + vec3(1.0));
    fragColor = vec4(color, 0.0);
}

)""
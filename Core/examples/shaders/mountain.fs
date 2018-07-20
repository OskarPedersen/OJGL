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



float smink( float a, float b, float k )
{
    float h = clamp( 0.5+0.5*(b-a)/k, 0.0, 1.0 );
    return mix( b, a, h ) - k*h*(1.0-h);
}

// Union with smink
vec2 sunk(vec2 a, vec2 b, float k)
{
	float sm = smink(a.x,b.x, k);
	float m = min(a.x, b.x);
	float ca = abs(sm -a.x);
	float cb = abs(sm -b.x);
	return ca < cb ? vec2(sm, a.y) : vec2(sm, b.y);
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

float sdBox( vec3 p, vec3 b )
{
  vec3 d = abs(p) - b;
  return min(max(d.x,max(d.y,d.z)),0.0) + length(max(d,0.0));
}


float sdCylinder( vec3 p, vec3 c )
{
  return length(p.xz-c.xy)-c.z;
}
#define PI 3.14159265

// Repeat around the origin by a fixed angle.
// For easier use, num of repetitions is use to specify the angle.
float pModPolar(inout vec2 p, float repetitions) {
	float angle = 2*PI/repetitions;
	float a = atan(p.y, p.x) + angle/2.;
	float r = length(p);
	float c = floor(a/angle);
	a = mod(a,angle) - angle/2.;
	p = vec2(cos(a), sin(a))*r;
	// For an odd number of repetitions, fix cell index of the cell in -x direction
	// (cell index would be e.g. -5 and 5 in the two halves of the cell):
	if (abs(c) >= (repetitions/2)) c = abs(c);
	return c;
}

#define MAT_MOUNTAIN 1.0
#define MAT_TOWER 2.0

#define PART_FLY 10.0
#define PART_BOTTOM (PART_FLY + 8.0)
#define PART_SPIN (PART_BOTTOM + 10.0)
#define PART_LIGHT_UP ( PART_SPIN + 15.0)
#define PART_FAR (PART_LIGHT_UP + 15.0)
#define PART_TRAVEL (PART_FAR + 30.0)


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

	//t *= 1.25;
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
	/*if (p.y > 275.0 || rd.y > 0.2) { 
		return res;
	}*/
	vec2 m = mountains(p.xz);
	res = un(res, vec2(p.y -m.x, m.y));
	res.x *= 0.5;

	
	vec3 o = p;
	float s = 400.0;
	p.x = mod(p.x + s * 0.5, s) - s * 0.5;
	float bound = sdBox(p - vec3(0.0, 150.0, 0.0), vec3(15.0, 70.0, 15.0));
	if (bound < 1) {
		
		//p.z += sin(o.x * 0.001) * 200.0 + 30.0;
		//p.y -= mountains(floor((o.xz +10.0) / 40.0) * 40.0).x;
		p.y -= mountains(round((o.xz) / 50.0) * 50.0).x;
		//p.y -= 150.0;

		float d = sdCappedCylinder(p, vec2(7.0, 30.0));
		float top = max(sdCappedCylinder(p - vec3(0, 31, 0), vec2(8.0, 3.0)), -sdCappedCylinder(p - vec3(0, 30, 0), vec2(6.0, 20.0)));
		

		vec2 q = p.xz;
		pModPolar(q, 8.0);
		float b = sdBox(vec3(q.x, p.y, q.y) - vec3(10, 33, 0), vec3(5.0, 2.0, 1.0));
		d = min(d, max(top, -b));
		
		//res.x = smink(res.x, d, 10.0);
		//res = un(res, vec2(d, MAT_MOUNTAIN));
		res = sunk(res, vec2(d, MAT_MOUNTAIN), 10.0);
	}
    return res;
}

vec4 evaluateLight(vec3 p)
{
	vec3 o = p;
	float s = 400.0;
	p.x = mod(p.x + s * 0.5, s) - s * 0.5;
	//p.z += sin(o.x * 0.001) * 200.0;
	//p.y -= 150.0;
	p.y -= mountains(floor((o.xz +10.0) / 40.0) * 40.0).x + 33.0;
	float dis = length(p) - 0.0;
	float strength = 0.0;
	

	//float dis = length(p + vec3(-300.0, -150.0 + sin(iGlobalTime) * 50.0, 0.0)) - 1.0;
	//float dis = length(p - vec3(300.0, 185.0, 90.0)) - 1.0;
	vec3 col = vec3(1.0, 0.1, 0.0);
	

	strength = 0.0;
	if (iGlobalTime > PART_SPIN) {
		float p = iGlobalTime - PART_SPIN;
		float c = floor((o.x - 350.0) / 400.0);
		strength = 20000.0 * smoothstep(2.0, 5.0, p +  c * 5.0 );
	}

	if (iGlobalTime > PART_FAR && o.x < -2500.0) {
		float t = iGlobalTime - PART_TRAVEL;
		float s = smoothstep(14.0, 16.0, t);
		dis = min(dis, sdCylinder(p, vec3(2.0, 2.0, 0.0)));
		strength *= 10.0;// * s;
	}
	if (o.x < -2800.0) {
		strength = 0.0;
	}

	vec3 res = col * strength / (dis * dis * dis);
	//return vec4(res, dis);
    
	vec3 col2 = vec3(0.8);
	float strength2 = 1.0;
	if (iGlobalTime < PART_FLY) {
		strength2 = smoothstep(2.0, 8.0, iGlobalTime);
	}

	vec3 res2 = col2 * strength2;
	return vec4(res + res2, dis);
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



float BrickPattern(in vec2 p) 
{
  p *= vec2 (1.0, 2.8);  // scale
  vec2 f = floor (p);
  if (2. * floor (f.y * 0.5) != f.y) 
    p.x += 0.5;  // brick shift
  p = smoothstep (0.03, 0.08, abs (fract (p + 0.5) - 0.5));
  return 1. - 0.9 * p.x * p.y;
}

void main()
{

    
    vec2 iMouse = vec2(160.5, 0.0);
	vec3 eye = vec3(110.0*sin(iMouse.x * 0.01), 250.0, 110.0*cos(iMouse.x  * 0.01));
	vec3 tar = vec3(eye.x * 1.1, eye.y - 2.0, eye.z * 1.1);
	//eye = vec3(220.0, 200.0, 50.0);

	eye = vec3(300.0, 200.0, 90.0) + vec3(60.0 * cos(iGlobalTime * 0.1), 0.0, 60.0 * sin(iGlobalTime * 0.1));
	tar = vec3(300.0, 180.0, 90.0);

	if (iGlobalTime < PART_FLY) {
		eye = vec3(700.0 + iGlobalTime * 10.0, 300.0, 1000.0);
		tar = eye + vec3(1.0, -0.5, 0.0);
	} else if (iGlobalTime < PART_BOTTOM) {
		float p = iGlobalTime - PART_FLY;
		eye = vec3(400.0, 140.0 + p * 5.0, 150.0);
		tar = vec3(400.0, 200.0, 0.0);
	} else if (iGlobalTime < PART_SPIN) {
		float p = iGlobalTime - PART_BOTTOM;
		eye = vec3(400.0, 230.0, 0.0);
		eye += vec3(120.0 * cos(iGlobalTime * 0.1), 0.0, 120.0 * sin(iGlobalTime * 0.1));
		tar = vec3(400.0, 200.0, 0.0);
	} else if (iGlobalTime < PART_LIGHT_UP) {
		float p = iGlobalTime - PART_SPIN;
		eye = vec3(450.0 + p, 230.0, 50.0 + p);
		tar = vec3(400.0 - max(0.0, (p - 4.0) * 10.0), 200.0, 0.0);
	} else  if (iGlobalTime < PART_FAR) {
		float p = iGlobalTime - PART_SPIN;
		eye = vec3(-1800.0 - p * 2.0, 280.0, 200.0 - p * 2.0);
		tar = vec3(-1500.0 , 200.0, 0.0);
	} else if (iGlobalTime < PART_TRAVEL) {
		float p = iGlobalTime - PART_FAR;
		eye = vec3(-1800.0 - p*50.0, 280.0, 30.0);
		tar = eye + vec3(-1.0, -0.5, 0.0);
		//tar = vec3(-1500.0 , 200.0, 0.0);
		tar = mix(tar, vec3(-2800.0, 300.0, 0.0), smoothstep(12.0, 35.0,p));
		tar += + vec3(sin(p * 3.0) * 0.005, sin(p * 5.0) * 0.004, sin(p * 7.0) * 0.003);
	}

	 vec3 dir = normalize(tar - eye);
	vec3 right = normalize(cross(vec3(0, 1, 0), dir));  
 	vec3 up = cross(dir, right);

    
    float f = 1.5;

    /*float u = (fragCoord.x / iResolution.x) * 2.0 - 1.0;
    float v = ((fragCoord.y / iResolution.y) * 2.0 - 1.0) * (iResolution.y/iResolution.x);*/
	float u = fragCoord.x * 2.0 - 1.0;
	float v = fragCoord.y * 2.0 - 1.0;
	v *= 9.0 / 16.0;
    


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
	
	const float maxDis = 1000.0;
	const int maxIter = 1000;

	 for(int i = 0; i < maxIter && t < maxDis; ++i) 
	 {
	   	vec3 p = ro + rd * t;
	    vec2 dm = scene(p, iGlobalTime, rd);
	    float d = dm.x;
	    float m = dm.y;
         
         float fogAmount = 0.004 + 0.07 * (1.0 - smoothstep(100.0, 160.0, p.y));
         if (iGlobalTime > PART_FAR) {
			float p = iGlobalTime - PART_FAR - 15.0;
			if (p > 0.0)
				fogAmount += p*0.02;
		 }
         vec4 lightColDis = evaluateLight(p);
         vec3 light = lightColDis.rgb;
         d = min(d, max(0.01, lightColDis.w * 0.25));
		 //d = max(d*0.2, 0.01);
		 //d *= 0.5;
         vec3 lightIntegrated = light - light * exp(-fogAmount * d);
         scatteredLight += transmittance * lightIntegrated;	
         transmittance *= exp(-fogAmount * d);
         
		//t += max(d*0.2, 0.01);
		//t +=  max(d*0.2, 0.01);
		//d = max(d*0.2, 0.01);
		t += d;

        bool end = i == maxIter - 1 || t >= maxDis;
	    if(d < 0.01 || end) 
	    {
	    	float spec = 1.0;
	    	vec3 normal = getNormal(p, iGlobalTime, rd);
	    	
	    	if (m == 1.0) //mountain
	    	{
	    		
	    		vec3 n = getNormalEps(p, iGlobalTime, rd, 1.0);		
	    		//color = mix(vec3(normal.y > 0.38 ? 0.9 : 0), vec3(n.y), 0.5);
	    		
	    		color =  mix(vec3(smoothstep(0.6, 0.9, normal.y) * 0.7), vec3(n.y), 0.2);
	    		spec = normal.y;
	
            } else if (m == MAT_TOWER) {
				float b = BrickPattern(p.xy);
				color = vec3(b);
			}
			
			if (end) {
				color = vec3(0.0);
			} else {
				float diffuse = max(0., dot(invLight, normal));
				float s = smoothstep(2.0, 8.0, iGlobalTime);
				if (iGlobalTime < PART_FLY) {
					
					diffuse *= s;
					spec *= s;
				}
				color = 0.7 * color * (s + diffuse);
	    		color += spec * specular(normal, -invLight, normalize(eye - p), 70.0);
				
			}
            
            
			
		   	//color =  applyFog(color, distance(eye, p), rd, invLight, p);


            color = transmittance * color + scatteredLight;
	    	
	        sky = false;
	       	break;
	    }
	    
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
			
		//} 
	//}
	//color =  applyFog(color, distance(eye, ro + rd * t), rd, invLight, ro + rd * t);
    //color = vec3(texture(iChannel0, vec2(u, v) * 1.0).x);
    
    color /= (color + vec3(1.0));
    fragColor = vec4(color, 0.0);
	float e = iGlobalTime - PART_FAR - 16.0;
	if (e > 0) {
		fragColor.rgb *= 1.0 -  e * 0.5;
	}
	
}

)""
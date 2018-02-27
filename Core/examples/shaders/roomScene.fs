R""(
#version 440

in vec2 fragCoord;
out vec4 fragColor;

uniform float iGlobalTime;
uniform sampler2D inTexture0;
uniform sampler2D inTexture1;
uniform sampler2D inTexture2;

uniform float CHANNEL_0_TIME_SINCE[41];
uniform float CHANNEL_1_TIME_SINCE[23];
uniform float CHANNEL_11_TIME_SINCE[28];
uniform float CHANNEL_12_TIME_SINCE[2];
uniform float CHANNEL_13_TIME_SINCE[1];

uniform float CHANNEL_0_TIME_TO[41];
uniform float CHANNEL_1_TIME_TO[23];
uniform float CHANNEL_11_TIME_TO[28];
uniform float CHANNEL_12_TIME_TO[2];
uniform float CHANNEL_13_TIME_TO[1];

uniform float CHANNEL_12_TOTAL;
uniform float CHANNEL_13_TOTAL;

//////////////////////////////////////////////////////

#define REFLECTION
//#define REFRACTION // TODO: I don't think this works perfectly.

#define VOLUMETRIC_LIGHTNING

#define SHADOWS

#define TONE_MAPPING

#define MAT_MIRROR 1.0
#define MAT_BOX 2.0
#define MAT_ROOM 3.0
#define MAT_CORRIDOR 4.0
#define MAT_FLOOR 5.0
#define MAT_ROOF 6.0

vec2 un(vec2 a, vec2 b)
{
	return a.x < b.x ? a : b;
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

#define S2(x,y) abs(fract(x))<0.8 ? 0.65 +0.35* sin(1.5707*(y-ceil(x))) : 0.0
float Basketwork2Pattern(in vec2 uv)
{
  vec2 p = uv * 4.0;
  return max (S2( p.x, p.y), S2(p.y, p.x+1.) ); 
}

float CheckerPattern(in vec2 uv)   // no AA
{
  uv = 0.5 - fract(uv);
  return 0.5 + 0.5*sign(uv.x*uv.y);
}
#define RandomSign sign(cos(1234.*cos(h.x+9.*h.y))); 
float HexagonalTruchetPattern(vec2 p) 
{
  vec2 h = p + vec2(0.58, 0.15)*p.y;
  vec2 f = fract(h);  
  h -= f;
  float v = fract((h.x + h.y) / 3.0);
  //(v < 0.6) ?   (v < 0.3) ?  h : h++ : h += step(f.yx,f) ; 
  if (v < 0.6) {
	if (v >= 0.3) {
		h++;
	} 	
  } else {
	h += step(f.yx,f);
  }
  p += vec2(0.5, 0.13)*h.y - h;          // -1/2, sqrt(3)/2
  v = RandomSign;
  return 0.1 / abs(0.5 -  min(min
    (length(p - v*vec2(-1., 0.00)  ),    // closest neighbor (even or odd set, dep. s)
     length(p - v*vec2(0.5, 0.87)) ),    // 1/2, sqrt(3)/2
     length(p - v*vec2(0.5,-0.87))));    
}

float StarPattern( vec2 g )
{
  g = abs(fract(g / 100.)-0.5);
  return max(max(g.x, g.y), min(g.x, g.y)*2.);
}

float HexagonalPattern(in vec2 p)   // no AA
{
  p.y = p.y * 0.866 + p.x*0.5;
  p = mod(p, vec2(3.0));
  return length(p);
  /*if(p.y < p.x+1.0 && p.y > 0.0 && p.x > 0.0
  && p.y > p.x-1.0 && p.x < 2.0 && p.y < 2.0)
    return 0.0;
  else if(p.y > 1.0 && (p.y < p.x || p.x < 1.0))
    return 0.5;
  return 1.0;*/

}

float lengthN(vec2 v, float n)
{
  return pow(pow(abs(v.x), n)+pow(abs(v.y), n), 0.89/n);
}
//---------------------------------------------------------
float QCirclePattern(vec2 p)
{
	p*= 0.25;
  vec2 p2 = mod(p*8.0, 4.0)-2.0;
  return sin(lengthN(p2, 4.0)*16.0);
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

float cubePattern(in vec3 p, in vec3 n, in float k )
{
	float x = QCirclePattern(p.yz);
	float y = QCirclePattern(p.zx);
	float z = QCirclePattern(p.xy);
    vec3 w = pow( abs(n), vec3(k) );
	return (x*w.x + y*w.y + z*w.z) / (w.x+w.y+w.z);
}



vec2 map(vec3 p, vec3 rd) 
{
	float pattern = BrickPattern(p.zy * 2.1 + vec2(0.0, 0.0));
	float n = noiseOctave(vec2(p.z, p.y) * 5.0, 10, 0.7);
	vec2 res = vec2(-sdBox(p - vec3(sign(p.x)*pattern * 0.02+n*0.02*sign(p.x), 0.0, 0.0), vec3(0.8, 10.0, 50.0)), MAT_CORRIDOR);

	float floorPattern = HexagonalGrid(p.xz, 0.15, 0.2);
	res = un(res, vec2(p.y + 1.0 - floorPattern*0.01, MAT_FLOOR));
	
	float roofPattern = Basketwork2Pattern(p.xz*2.5);
	res = un(res, vec2(-p.y + 1.0 -roofPattern * 0.02, MAT_ROOF));
	/*vec3 po = p;
	vec3 normal;
    vec3 ep = vec3(0.01, 0, 0);
    normal.x = udRoundBox(p + ep.xyz, vec3(5.0, 2.0, 5.0), 2.0) - udRoundBox(p - ep.xyz, vec3(5.0, 2.0, 5.0), 2.0);
    normal.y = udRoundBox(p + ep.yxz, vec3(5.0, 2.0, 5.0), 2.0) - udRoundBox(p - ep.yxz, vec3(5.0, 2.0, 5.0), 2.0);
    normal.z = udRoundBox(p + ep.yzx, vec3(5.0, 2.0, 5.0), 2.0) - udRoundBox(p - ep.yzx, vec3(5.0, 2.0, 5.0), 2.0);
	normal = normalize(normal);
	float pattern = cubePattern(p, normal, 1.0);	
	vec2 res = vec2(-udRoundBox(po - normal * pattern * 0.01, vec3(5.0, 2.0, 5.0), 2.0), MAT_ROOM);*/
	//if (abs(p.y) >= 1.9) {
		//p *= 0.2;
	
		//float n = noiseOctave(vec2(p.x, p.z) * 4., 10, 0.7);
		//float gs = 0.5 + 0.5 * sin(p.x * 50.0 + n * 60.0);
		//float n2 = noiseOctave(vec2(p.x, p.z) * 100., 10, 0.7);

		//res = vec2(-sdBox(po - vec3(0.0, sign(p.y)*gs*0.0075, 0.0), vec3(5.0, 2.0, 5.0)), MAT_ROOM);
		//vec2 res = vec2(-sdBox(po - vec3(0.0, 0.0, 0.0), vec3(5.0, 2.0, 5.0)), MAT_ROOM);
		//return res;
	//} else {
		//float basket =  QCirclePattern(vec2(p.x + p.z + iGlobalTime, p.y));
		//res = vec2(-sdBox(po - vec3(basket*0.0075, 0.0, basket*0.0075), vec3(5.0, 2.0, 5.0)), MAT_ROOM);
		//return res;

		//p *= 0.2;
	
		//float n = noiseOctave(vec2(p.x + p.z, p.y) * 4., 10, 0.7);
		//float gs = 0.5 + 0.5 * sin(p.x * 50.0 + n * 60.0);
		//float n2 = noiseOctave(vec2(p.x + p.z, p.y) * 100., 10, 0.7);

	//	vec2 res = vec2(-sdBox(po - vec3(gs*0.0075, 0.0, gs*0.0075), vec3(5.0, 2.0, 5.0)), MAT_ROOM);
	//	return res;
	//}
	//res = un(res, vec2(udRoundBox(p - vec3(1, 0, 0), vec3(0.4), 0.1), MAT_MIRROR));
	return res;
}

vec3 lightAModifyPos(vec3 p)
{
	float s = 5.0;
	p.z = mod(p.z, s) - s * 0.5;
	return p - vec3(0.0, 0.8, 0.0);
}

vec4 lightA(vec3 p)
{
	float dis = length(p);
	vec3 col = vec3(1.0, 1.0, 1.0);
	const float strength = 0.5;
	vec3 res = col * strength / (dis * dis * dis);
	return vec4(res, dis);
}

vec4 lightUnion(vec4 a, vec4 b)
{
	return vec4(a.rgb + b.rgb, min(a.w, b.w));
}

vec4 evaluateLight(vec3 pos)
{
	vec4 res = lightA(lightAModifyPos(pos));
	return res;
}

#ifdef SHADOWS
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
#else
#define shadowFunction(ro, rd, mint, maxt) 1.0
#endif

float specular(vec3 normal, vec3 light, vec3 viewdir, float s)
{
	float nrm = (s + 8.0) / (3.1415 * 8.0);
	float k = max(0.0, dot(viewdir, reflect(light, normal)));
    return  pow(k, s);
}

void addLight(inout vec3 diffRes, inout float specRes, vec3 normal, vec3 eye, vec3 lightPos, vec3 lightCol, float shadow, vec3 pos)
{
	vec3 col = vec3(0.0);
	vec3 invLight = normalize(lightPos - pos);
	float diffuse = max(0.0, dot(invLight, normal));
	float spec = specular(normal, -invLight, normalize(eye - pos), 200.0);
	float dis = length(lightPos);
	float str = 1.0/(0.5 + 0.01*dis + 0.1*dis*dis);
	float specStr = 1.0/(0.0 + 0.00*dis + dis*dis*dis);
	diffRes += diffuse * lightCol * shadow;
	
	specRes += spec * specStr * shadow * length(lightCol) * 20.0;
}

void addLightning(inout vec3 color, vec3 normal, vec3 eye, vec3 pos) {
	vec3 diffuse = vec3(0.0);
	float specular = 0.0;
	const float ambient = 0.0;

	{
		// Lights without shadow
		vec3 posLightOrigo = lightAModifyPos(pos);
		//float shadow = shadowFunction(pos, normalize(-posLightOrigo), 0.1, length(posLightOrigo));
		addLight(diffuse, specular, normal, eye, pos-posLightOrigo, lightA(posLightOrigo).rgb, 1.0, pos);
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
	const int maxIter = 90;
	const float maxDis = 200.0;
	const int jumps = 2;

	vec3 col = vec3(0);	
	float ref = 1.0;

	vec3 scatteredLight = vec3(0.0);
	float transmittance = 1.0;
	for (int j = 0; j < jumps; j++) {
		float t = 0.0;
		for (int i = 0; i < maxIter; i++) {
			vec3 p = ro + rd * t;
			p.x += sin(p.z*0.5);
			vec2 res = map(p, rd);
			float d = res.x;
			float m = res.y;
#ifdef VOLUMETRIC_LIGHTNING
			float fogAmount = 0.001;
			vec4 lightColDis = evaluateLight(p);
			vec3 light = lightColDis.rgb;
			d = min(d, lightColDis.w);

			vec3 lightIntegrated = light - light * exp(-fogAmount * d);
			scatteredLight += transmittance * lightIntegrated;	
			transmittance *= exp(-fogAmount * d);
#endif
			t += d;		
			bool end = i == maxIter - 1 || t > maxDis;
			if (d < 0.01 || end) {
				vec3 c = vec3(1, 0, 1);
				vec3 normal = getNormal(p, rd);

				if (m == MAT_MIRROR) {
					c = vec3(0.0);
				} else if (m == MAT_CORRIDOR) {				
					float pattern = BrickPattern(p.zy * 2.1 + vec2(0.0, 0.0));
					float n = noiseOctave(vec2(p.z, p.y) * 5.0, 10, 0.7);
					vec3 brick = vec3(1.0, 0.6, 0.35)*(0.2 + 0.8 * n);
					vec3 mortar = vec3(1.0);
					c = mix(brick, mortar, pattern);
					//c = vec3(n);
				} else if (m == MAT_ROOF) {
					float pattern = Basketwork2Pattern(p.xz*2.5);
					c = vec3(pattern*0.9, pattern*0.8, pattern*0.4);
				} else if (m == MAT_FLOOR) {
					float pattern = HexagonalGrid(p.xz, 0.15, 0.2);
					c = vec3(pattern);
				} else if (m == MAT_BOX) {
					c = vec3(1.0, 0.0, 0.0);
				} else if (m == MAT_ROOM) {
					vec3 po = p;
					float pattern = cubePattern(p, normal, 3.0);
					c = vec3( 0.5, 0.0, pattern);
					/*p *= 0.2;
					float n = noiseOctave(vec2(p.x + p.z, p.y) * 4., 10, 0.7);
					float gs = 0.5 + 0.5 * sin((p.x + p.z) * 50.0 + n * 60.0);
    
					vec3 blue = vec3(0.8);
					vec3 rust = vec3(0.5, 0.5, 0.5);
    
					vec3 color = mix(rust, blue, 0.8 * gs);
					float n2 = noiseOctave(vec2(p.x + p.z, p.y) * 100., 10, 0.7);
					color = mix(color, vec3(n2 * 0.5 + 0.25), 0.3);*/

					//float basket = QCirclePattern(vec2(p.x + p.z + iGlobalTime, p.y));
					//c = vec3( 0.5, 0.0, basket);

					//c = vec3(0.5);
					//c = color;
					
					//if (abs(p.y) >= 1.99) {
					//	float n = noiseOctave(vec2(p.z, p.x) * 4., 10, 0.7);
					//	float gs = 0.5 + 0.5 * sin(p.z * 50.0 + n * 60.0);
    
					//	vec3 blue = vec3(0.8);
					//	vec3 rust = vec3(0.5, 0.5, 0.5);
    
					//	vec3 color = mix(rust, blue, 0.8 * gs);
					//	float n2 = noiseOctave(vec2(p.z, p.x) * 100., 10, 0.7);
					//	color = mix(color, vec3(n2 * 0.5 + 0.25), 0.3);
					///	c = color;
					//}
					p = po;
				}

				c *= occlusion(p, normal, rd);
				addLightning(c, normal, eye, p);
				
				if (end) {
					transmittance = 0;
				}
				col = mix(col, transmittance * c + scatteredLight, ref);

				if (m == MAT_ROOM ) {
					//if (abs(p.y) <= 1.99) {
					
					//}else{
						return col;
					//}				
					
				} else if (m == MAT_MIRROR) {
					ref *= 0.9;
				} else if (m == MAT_BOX) {
					ref *= 0.5;
				}  else if (m == MAT_CORRIDOR) {
					return col;
				} else if (m == MAT_ROOF) {
					return col;
				} else if (m == MAT_FLOOR) {
					return col;
				}

#ifdef REFLECTION
				rd = reflect(rd, getNormal(p, rd));
#endif
#ifdef REFRACTION
				rd = refract(rd, getNormal(p, rd), 1/1.2);
#endif
				ro = p + rd*0.5;
				t = 0;
				break;
			}
		}
	}
	return col;
}

void main()
{
    float u = fragCoord.x * 2.0 - 1.0;
	float v = fragCoord.y * 2.0 - 1.0;

    vec3 eye = vec3(-sin(iGlobalTime*0.5), 0.0, iGlobalTime); //vec3(2 * sin(iGlobalTime), 1, 2 * cos(iGlobalTime));
	vec3 tar = vec3(-sin((iGlobalTime+1.0)*0.5), 0.0, iGlobalTime + 1.0);//eye + vec3(0.0, 0.0, 1.0); 

	vec3 dir = normalize(tar - eye);
	vec3 right = normalize(cross(vec3(0, 1, 0), dir));
	vec3 up = cross(dir, right);

	vec3 ro = eye;
	vec3 rd = normalize(dir + right*u + up*v);
	
	vec3 color = raymarch(ro, rd, eye);
#ifdef TONE_MAPPING
	color /= (color + vec3(1.0));
#endif
    fragColor = vec4(color, 1.0);
} 

)""  
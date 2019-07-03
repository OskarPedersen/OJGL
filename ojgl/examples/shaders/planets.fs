R""(
#version 430

in vec2 fragCoord;
out vec4 fragColor;

uniform sampler2D inTexture0;
uniform float iGlobalTime;
uniform float iTime;
uniform vec2 iResolution;

uniform vec3 planets[10];
float[] planetScales = float[10](2.0,	// Earth
								10.0,	// Sun
								1.0,	// Mercury
								1.5,	// Venus
								1.2,	// Mars
								5.0,	// Jupiter
								4.0,	// Saturnus
								3.0,	// Uranus
								3.0,	// Neptunus
								0.01);	// Moon

uniform float DEBUG_D1;
uniform float DEBUG_D2;
uniform float DEBUG_D3;

// Uses some iq functions and patterns from https://www.shadertoy.com/view/4dfyzf


#define PI 3.141592

#define TIME iTime

vec2 un(vec2 a, vec2 b)
{
	return a.x < b.x ? a : b;
}


float sdCappedCylinder( vec3 p, vec2 h )
{
  vec2 d = abs(vec2(length(p.xz),p.y)) - h;
  return min(max(d.x,d.y),0.0) + length(max(d,0.0));
}

float specular(vec3 normal, vec3 light, vec3 viewdir, float s)
{
	float nrm = (s + 8.0) / (3.1415 * 8.0);
	float k = max(0.0, dot(viewdir, reflect(light, normal)));
    return  pow(k, s);
}

#define MAT_BOX 1.0
#define MAT_GROUND 2.0
#define MAT_ROOM 3.0
#define MAT_PILLAR 4.0
#define MAT_LAMP 5.0
#define MAT_CORRIDOR 6.0
#define MAT_CORRIDOR_ROT 7.0
#define MAT_DOOR 8.0
#define MAT_THING 9.0
#define MAT_SCREEN 10.0
#define MAT_PLANET 11.0
#define MAT_GRID 12.0

float udRoundBox( vec3 p, vec3 b, float r )
{
  return length(max(abs(p)-b,0.0))-r;
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


float sdHexPrism( vec3 p, vec2 h )
{
    vec3 q = abs(p);
    return max(q.z-h.y,max((q.x*0.866025+q.y*0.5),q.y)-h.x);
}

float smin( float a, float b)
{
	float k = 0.1;
    float h = clamp( 0.5+0.5*(b-a)/k, 0.0, 1.0 );
    return mix( b, a, h ) - k*h*(1.0-h);
}

float smink( float a, float b, float k, inout float h)
{
    h = clamp( 0.5+0.5*(b-a)/k, 0.0, 1.0 );

    return mix( b, a, h ) - k*h*(1.0-h);
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

float height(ivec2 xz) {
	//float n = noiseOctave(vec2(xz) * 10.0, 20, 0.6);
    //return round(n*2.0) - 1.0;
    return round(0.5*(1.0 + 0.5*sin(float(xz.x)) + 0.5*sin(float(xz.y))));
}

// Repeat in two dimensions, from http://mercury.sexy/hg_sdf/
vec2 pMod2(inout vec2 p, vec2 size) {
	vec2 c = floor((p + size*0.5)/size);
	p = mod(p + size*0.5,size) - size*0.5;
	return c;
}
vec2 pMod2Index(vec2 p, vec2 size) {
	vec2 c = floor((p + size*0.5)/size);
	return c;
}
#define ROOM_GRID_SIZE 5.0
vec2 map(vec3 p, vec3 rd, vec3 eye) 
{
    vec3 po = p;
    vec2 res = vec2(0.0);

    
    {
        
        

        vec3 q = p;
        vec2 roomIndex = pMod2(q.xz, vec2(ROOM_GRID_SIZE));
        //float h = height(n);
        const vec3 ROOM_SIZE = vec3(1.5, 0.6, 1.5);
        float roomD = -udRoundBox(q -vec3(0.0, /*h*/0.0, 0.0), ROOM_SIZE, 0.1);
        //res = vec2(d, MAT_ROOM);
        
        { // Lamp
        	float d = udRoundBox(q- vec3(0.0, ROOM_SIZE.y, 0.0), vec3(0.2), 0.05);
            roomD = max(-d, roomD); // Subtraction
        }

        const float doorOffset = 0.5;
        vec2 doorRealPos = vec2(ROOM_GRID_SIZE, ROOM_GRID_SIZE * 0.5) * floor((p.xz + vec2(ROOM_GRID_SIZE, ROOM_GRID_SIZE * 0.5)*0.5)/vec2(ROOM_GRID_SIZE, ROOM_GRID_SIZE * 0.5));
       
        vec3 q2 = p + vec3(ROOM_GRID_SIZE * 0.5, 0.0, ROOM_GRID_SIZE * 0.5);
        float n2 = floor((q2.z + ROOM_GRID_SIZE * 0.25) / (ROOM_GRID_SIZE*0.5));
        float xo = mod(n2, 2.0) * ROOM_GRID_SIZE * 0.5;
        vec2 tmp = vec2(ROOM_GRID_SIZE, ROOM_GRID_SIZE * 0.5);
        q2.xz = mod(q2.xz + vec2(xo, 1.25), tmp) - tmp*0.5;
        if (xo > 0.0) {
            q2.xz = q2.zx;
            doorRealPos.x -= ROOM_GRID_SIZE * 0.5;
            doorRealPos.x += doorOffset;
        } else {
        	doorRealPos.y += doorOffset;
        }
        
        float doorUpDown = 0.1;
        float d2 = -udRoundBox(q2 - vec3(0.0, -ROOM_SIZE.y * doorUpDown, 0.0), vec3(0.5, ROOM_SIZE.y * (1.0 - doorUpDown), 1.1), 0.1);
        
        
        float finalD = max(roomD, d2);
        
		float matCorr = MAT_CORRIDOR;
        if (xo > 0.0) {
            //res = un(res, vec2(d, MAT_CORRIDOR));
            matCorr = MAT_CORRIDOR_ROT;
        } else {
            //res = un(res, vec2(d, MAT_CORRIDOR_ROT));
        }
        float roomMat = MAT_ROOM;//mod(roomIndex.x + roomIndex.y, 2.0) < 1.0 ? MAT_PILLAR : MAT_ROOM;
        float mat = roomD >= d2 ? roomMat : matCorr;
        res = vec2(finalD, mat);
        
        
        vec3 q3 = q2;
        q3.x = abs(q3.x);
        vec2 roomIndexEye = floor((eye.xz + vec2(ROOM_GRID_SIZE)*0.5)/vec2(ROOM_GRID_SIZE));
        float dis = length(eye.xz - doorRealPos);
        float open = 0.75;
        float closed = 0.32;
        float state  = open - (open - closed)*smoothstep(0.0, 1.0, dis);
        if (roomIndexEye != roomIndex) {
        //	state = closed;
        }
        float dd = udRoundBox(q3 - vec3(state, 0.0, doorOffset), vec3(0.3, 1.5, 0.05), 0.02);
        res = un(res, vec2(dd, MAT_DOOR));
        
        //float debugD = sdBox(po - vec3(doorRealPos.x, 0.0, doorRealPos.y), vec3(0.2));
        //res = un(res, vec2(debugD, MAT_PILLAR));
        //res = vec2(debugD, MAT_PILLAR);
        
        
        {
            //float d = -sdBox(p, vec3(20.0, 20.0, 20.0));
        	//res = vec2(d, MAT_ROOM);
        }

		//{
		//	float d = sdBox(p, vec3(1.0, 1.0, 0.1));
		//	res = un(res, vec2(d, MAT_GRID));
		//}
			
		//{
		//	res = vec2(9999999.0, 0.0);
		//	for (int i = 0; i < planets.length(); i++) {
		//		float d = sdBox(p - planets[i], vec3(0.1));//length(p - planets[i]) - 0.1;
		//		res = un(res, vec2(d, MAT_PLANET));
		//	}
		//}
    }
    
	return res;
}



vec4 lightUnion(vec4 a, vec4 b)
{
	return vec4(a.rgb + b.rgb, min(a.w, b.w));
}

vec4 evaluateLight(vec3 pos)
{
	/*vec4 res = lightA(lightAModifyPos(pos), pos);
    res = lightUnion(res, lightLaser(lightLaserModifyPos(pos), pos));
	return res;*/
    return vec4(0.0);
}

float shadowFunction(in vec3 ro, in vec3 rd, float mint, float maxt, vec3 eye)
{
    float t = 0.1;
    for(float _ = 0.0; _ == 0.0; _ += 0.0)
    {
        if (t >= maxt) {
        	return 1.0;
        }
        float h = map(ro + rd*t, rd, eye).x;
        if( h<0.01 )
            return 0.0;
        t += h;
    }
    return 1.0;
}

// From iq
float calcSoftshadow( in vec3 ro, in vec3 rd, in float mint, in float tmax, vec3 eye )
{
	float res = 1.0;
    float t = mint;
    //for( int i=0; i<16; i++ )
     for(float _ = 0.0; _ == 0.0; _ += 0.0)
    {
		float h = map( ro + rd*t, rd, eye ).x;
        res = min( res, 8.0*h/t );
        t += clamp( h, 0.02, 0.10 );
        if( res<0.005 || t>tmax ) break;
    }
    return clamp( res, 0.0, 1.0 );
}


float occlusion(vec3 p, vec3 normal, vec3 rd, vec3 eye)
{
	float o = clamp(2.0*map(p + normal * 0.5, rd, eye).x, 0.0, 1.0);
	return 0.8 + 0.2*o;
}

void addLight(inout vec3 diffRes, inout float specRes, vec3 normal, vec3 eye, vec3 lightPos, vec3 lightCol, float shadow, vec3 pos)
{
	vec3 col = vec3(0.0);
	vec3 invLight = normalize(lightPos - pos);
	float diffuse = max(0.0, dot(invLight, normal));
	float spec = specular(normal, -invLight, normalize(eye - pos), 200.0);
	float dis = length(lightPos - pos);
	float str = 5.0 / (dis*dis*dis);//10.0/(0.5 + 0.01*dis + 0.1*dis*dis);
	float specStr = str;//1.0/(0.0 + 0.00*dis + dis*dis*dis);
	diffRes += diffuse * lightCol * shadow*str;
    //diffRes = vec3(length(lightCol));
	
	specRes += specStr * spec  *  shadow  * 1.0 * length(lightCol);
}

void yolo(inout vec3 color, vec3 normal, vec3 eye, vec3 pos, vec3 loff, vec3 rd, inout vec3 diffuse, inout float specular){
		vec2 roomIndex = pMod2Index(pos.xz, vec2(ROOM_GRID_SIZE));
        //pMod2(eye.xz, vec2(ROOM_GRID_SIZE));
        //
        

        vec3 lp = vec3(roomIndex.x * ROOM_GRID_SIZE, 0.55, roomIndex.y * ROOM_GRID_SIZE) + loff;
        
        vec3 col = vec3(1.0);
        //col = col * 100.0 / pow(length(pos), 3.0); //???
        //float shadow = calcSoftshadow(pos.xyz, normalize(lp - pos), 0.1, length(lp - pos), eye);//calcSoftshadow(pos, normalize(lp - pos), 0.1, length(lp - pos));
		//float shadow = shadowFunction(pos.xyz, normalize(lp - pos), 0.1, length(lp - pos));
        addLight(diffuse, specular, normal, eye, lp, col, /*shadow*/ 1.0, pos);
}

void addLightning(inout vec3 color, vec3 normal, vec3 eye, vec3 pos, vec3 rd) {
	vec3 diffuse = vec3(0.0);
	float specular = 0.0;
	float ambient = 0.0 * occlusion(pos, normal, rd, eye);

    
    {
        
        //vec3 lp = vec3(sin(iTime*2.0) * 0.0, 1.0, 5.0);//vec3(sin(iTime), 0.0, iTime + sin(iTime)*5.0);
        //vec3 lp = vec3(0.0, 4.0 + sin(iMouse.y*0.05), 7.0);
        yolo(color, normal, eye, pos, vec3(0.0, 0.0, 0.0), rd, diffuse, specular);
        yolo(color, normal, eye, pos, vec3(ROOM_GRID_SIZE, 0.0, 0.0), rd, diffuse, specular);
        yolo(color, normal, eye, pos, vec3(0.0, 0.0, ROOM_GRID_SIZE), rd, diffuse, specular);
        yolo(color, normal, eye, pos, vec3(-ROOM_GRID_SIZE, 0.0, 0.0), rd, diffuse, specular);
        yolo(color, normal, eye, pos, vec3(0.0, 0.0, -ROOM_GRID_SIZE), rd, diffuse, specular);
            
    }
    float o = occlusion(pos, normal, rd, eye);
	color = color * (ambient*o + diffuse) + specular;
    //color = diffuse;
}

vec3 getNormal(vec3 p, vec3 rd, vec3 eye)
{
	vec3 normal;
    vec3 ep = vec3(0.01, 0, 0);
    normal.x = map(p + ep.xyz, rd, eye).x - map(p - ep.xyz, rd, eye).x;
    normal.y = map(p + ep.yxz, rd, eye).x - map(p - ep.yxz, rd, eye).x;
    normal.z = map(p + ep.yzx, rd, eye).x - map(p - ep.yzx, rd, eye).x;
    return normalize(normal);
}

//iq
float calcAO( in vec3 pos, in vec3 nor, vec3 rd, vec3 eye )
{
	float occ = 0.0;
    float sca = 1.0;
    for( int i=0; i<5; i++ )
    {
        float hr = 0.01 + 0.12*float(i)/4.0;
        vec3 aopos =  nor * hr + pos;
        float dd = map( aopos, rd, eye ).x;
        occ += -(dd-hr)*sca;
        sca *= 0.95;
    }
    return clamp( 1.0 - 3.0*occ, 0.0, 1.0 );    
}


vec3 raymarch(vec3 ro, vec3 rd, vec2 uv) 
{
    bool last = false;
    vec3 eye = ro;
	int maxIter = 200;
	const float maxDis = 50.0;
	const int jumps = 3;
    
   

	vec3 col = vec3(0.0, 0.0, 0.0);	
	float ref = 1.0;

	vec3 scatteredLight = vec3(0.0);
	float transmittance = 1.0;
	for (int j = 0; j < jumps; j++) {
		float t = 0.0;
		for (int i = 0; i < maxIter; i++) {
			vec3 p = ro + rd * t;
			vec2 res = map(p, rd, eye);
			float d = res.x;
			float m = res.y;


			t += d;		
			bool end = i == maxIter - 1 || t > maxDis;
			if (d < 0.01 /*|| end*/) {
				
				vec3 c = vec3(1, 0, 1);
				vec3 normal = getNormal(p, rd, eye);


				if (m == MAT_BOX) {
					c = vec3(0.5);
                } else if (m == MAT_GROUND) {
                    
                	c = vec3(mod(floor(p.x) + floor(p.z), 2.0));
                } else if (m == MAT_ROOM) {
                    if (p.y > 0.0) {
                        c = vec3(0.1);
                    } else {
                        c = vec3(255.0, 93.0, 12.0) / 255.0;
                    }
                } else if (m == MAT_PILLAR) {
                	c = vec3(0.5, 0.2, 0.5);
                } else if (m == MAT_LAMP) {
                	c = vec3(0.5);
                } else if (m == MAT_CORRIDOR) {
                    if (p.y > 0.0) {
                        c = vec3(0.1);
                    } else {
                        c = vec3(255.0, 93.0, 12.0) / 255.0;
                    }
                } else if (m == MAT_CORRIDOR_ROT) {
                    if (p.y > 0.0) {
                        c = vec3(0.1);
                    } else {
                        c = vec3(255.0, 93.0, 12.0) / 255.0;
                    }
                } else if (m == MAT_DOOR) {
                	c = vec3(255.0, 93.0, 12.0) / 255.0;
                    if (mod(p.y, 0.5) > 0.25) {
                    	c = vec3(1.0);
                    }
                    //c = vec3(0.1, 0.2, 0.6);
                    c = vec3(1.0);
                    c = vec3(255.0, 93.0, 12.0) / 255.0;
                } else if (m == MAT_THING) {
                	c = vec3(1.0, 0.5, 0.5);
                }



				//c *= occlusion(p, normal, rd);
                if (uv.x > 0.0 || true) {
					addLightning(c, normal, eye, p, rd);
                } else {

                }
				
				if (end) {
					transmittance = 0.0;
				}
                //col = vec3(1.0, 0.0, 0.0);
                float fre = pow( clamp(1.0+dot(normal,rd),0.0,1.0), 2.0 );
                //return vec3(fre);
                if (uv.x < 0.0) {
                	fre = 0.0;
                }
                
				vec3 lightAura = vec3(0.0);
               for (int pl = 0; pl < planets.length(); pl++)
				{
					//if (pl == 1) continue;
					float plf = float(pl);
					float lightInvSize = 10000.0;//52.1 + 20.0*sin(float(pl));
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
						const vec3 auraCol = vec3(0.5 + 0.5*sin(plf), 0.5 + 0.5*sin(plf*2.0), 0.5 + 0.5*sin(plf*0.5));
						//const vec3 auraCol = vec3(1.0, 0.8, 0.1);
						if (ldis < 0.5) {
							float fade = 0.5 - ldis * 10.0;
							fade = max(0.0, fade);
							lightAura += 10.0 * auraCol*vec3(1.0/(0.01 + lightInvSize*ldis)) * fade;
						}
			
						//col = lightAura;
					}
				}
                
                
                
                
				col = mix(col, c + lightAura, ref);


                	ref *= mix(0.2, 0.3, fre);

                
				
				rd = reflect(rd, getNormal(p, rd, eye));

				ro = p + rd * 0.02;
				break;
			}
		}
	}

	return col;// ;
}

#define AA 1


//void mainImage( out vec4 fragColor, in vec2 fragCoord )
void main()
{
    vec3 tot = vec3(0.0);
     for( int m=0; m<AA; m++ )
         for( int n=0; n<AA; n++ ) {
            //vec2 o = vec2(float(m),float(n)) / float(AA) - 0.5;
        	//vec2 uv = (-iResolution.xy + 2.0*(fragCoord+o))/iResolution.y;

			vec2 uv = (fragCoord - 0.5) * 2.0;
			//uv.y *= iResolution.y / iResolution.x;
    
             //vec2 uv = fragCoord.xy / iResolution.xy;
             //uv = uv * 2.0 - 1.0;

             float d = 2.5;
             //float rot = iMouse.x * 0.01;
             //float cameraY = iMouse.y * 0.01;
            
			vec3 eye = vec3(0.0, 0.0, 1.0);//vec3( cos(rot) * d, cameraY, sin(rot) * d); 
			vec3 tar = vec3(0.001);

			 eye +=  vec3(DEBUG_D1, DEBUG_D2, DEBUG_D3);
			 
			 //vec3 tar = planets[0];
			 //vec3 eye = tar - vec3(0.001, 0.001, 0.03);


             vec3 dir = normalize(tar - eye);
             vec3 right = normalize(cross(vec3(0, 1, 0), dir));
             vec3 up = cross(dir, right);

             vec3 ro = eye;
             vec3 rd = normalize(dir + right * uv.x * 1.0 + up  *uv.y * 1.0);
             
             vec3 color = raymarch(ro, rd, uv);
             tot += color;
    
	
         }
    tot /= float(AA*AA);
    tot /= (tot + vec3(1.0));
    // gamma
     tot = pow( tot, vec3(0.4545) );

    fragColor = vec4(tot, 1.0);
} 


)""
R""(
#version 430

in vec2 fragCoord;
out vec4 fragColor;

uniform sampler2D inTexture0;

uniform vec2 iResolution;

// https://www.shadertoy.com/view/XdfGDH

float normpdf(in float x, in float sigma)
{
	return 0.39894*exp(-0.5*x*x/(sigma*sigma))/sigma;
}
void main()
{
	/*vec2 uv = fragCoord.xy;
    vec4 res = vec4(0.0);
    res += texture(inTexture0, uv);
    res += texture(inTexture0, uv + vec2(1.0, 0.0) /  iResolution.x);
    res += texture(inTexture0, uv + vec2(-1.0, 0.0) /  iResolution.x);
    res += texture(inTexture0, uv + vec2(0.0, 1.0) /  iResolution.y);
    res += texture(inTexture0, uv + vec2(0.0, -1.0) /  iResolution.y);

    fragColor = res / 5;*/


    //declare stuff
	const int mSize = 15;//11;
	const int kSize = (mSize-1)/2;
	float kernel[mSize];
	vec4 final_colour = vec4(0.0);
		
	//create the 1-D kernel
	float sigma = 7.0;
	float Z = 0.0;
	for (int j = 0; j <= kSize; ++j)
	{
		kernel[kSize+j] = kernel[kSize-j] = normpdf(float(j), sigma);
	}
		
	//get the normalization factor (as the gaussian has been clamped)
	for (int j = 0; j < mSize; ++j)
	{
		Z += kernel[j];
	}
		
	//read out the texels
	for (int i=-kSize; i <= kSize; ++i)
	{
		for (int j=-kSize; j <= kSize; ++j)
		{
			final_colour += kernel[kSize+j]*kernel[kSize+i]*texture(inTexture0, fragCoord.xy+vec2(float(i),float(j)) / iResolution.xy)/*.rgb*/;
	
		}
	}
		
		
	//fragColor = vec4(final_colour/(Z*Z), 1.0);
	fragColor = final_colour/(Z*Z);

   
}

)""

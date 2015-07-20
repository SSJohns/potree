
// 
// adapted from the EDL shader code from Christian Boucheny in cloud compare:
// https://github.com/cloudcompare/trunk/tree/master/plugins/qEDL/shaders/EDL
//

#define NEIGHBOUR_COUNT 8


uniform mat4 projectionMatrix;

uniform float screenWidth;
uniform float screenHeight;
uniform float near;
uniform float far;
uniform vec2 neighbours[NEIGHBOUR_COUNT];
uniform vec3 lightDir;
uniform float zoom;
uniform float pixScale;
uniform float expScale;

uniform sampler2D depthMap;
uniform sampler2D colorMap;

varying vec2 vUv;
varying vec3 vViewRay;

/**
 * transform linear depth to [0,1] interval with 1 beeing closest to the camera.
 */
float ztransform(float linearDepth){
	return 1.0 - (linearDepth - near) / (far - near);
}

float obscurance(float z, float dist){
	return max(0.0, z) / dist;
}

float computeObscurance(float linearDepth, float scale){
	vec4 P = vec4(lightDir, -dot(lightDir, vec3(0.0, 0.0, ztransform(linearDepth)) ) );
	
	float sum = 0.0;
	
	for(int c = 0; c < NEIGHBOUR_COUNT; c++){
		vec2 N_rel_pos = scale * zoom / vec2(screenWidth, screenHeight) * neighbours[c];
		vec2 N_abs_pos = vUv + N_rel_pos;
		
		vec4 neighbourDepth = texture2D(depthMap, N_abs_pos);
		
		if(neighbourDepth.w > 0.0){
			float Zn = ztransform(neighbourDepth.r);
			float Znp = dot( vec4( N_rel_pos, Zn, 1.0), P );
			
			sum += obscurance( Znp, 0.1 * linearDepth );
		}
	}
	
	return sum;
}

void main(){
	vec4 color = texture2D(colorMap, vUv);

	float linearDepth = texture2D(depthMap, vUv).r;
	float f = computeObscurance(linearDepth, pixScale);
	f = exp(-expScale * f);
	
	if(color.a == 0.0 && f >= 1.0){
		discard;
	}
	
	gl_FragColor = vec4(color.rgb * f, 1.0);
}
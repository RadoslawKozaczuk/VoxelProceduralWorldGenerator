#if !defined(FLOW_INCLUDED)
#define FLOW_INCLUDED

// We cannot avoid resetting the progression of the distortion, but we can try to hide it.
// What we could do is fade the texture to black as we approach maximum distortion.
// W stands for the blend weight
float3 FlowUVW(float2 uv, float2 flowVector, float time) {
	float progress = frac(time); // we need to reset the animation at some point to prevent endless distortions
	float3 uvw;
	uvw.xy = uv - flowVector * progress;
	uvw.z = 1 - abs(1 - 2 * progress); // we use triangle function that reaches its maximum in halfway of the period
	return uvw;
}

#endif
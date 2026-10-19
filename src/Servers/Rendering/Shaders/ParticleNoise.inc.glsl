vec4 grad(vec4 p) {
	p = fract(vec4(
			dot(p, vec4(0.143081, 0.001724, 0.280166, 0.262771)),
			dot(p, vec4(0.645401, -0.047791, -0.146698, 0.595016)),
			dot(p, vec4(-0.499665, -0.095734, 0.425674, -0.207367)),
			dot(p, vec4(-0.013596, -0.848588, 0.423736, 0.17044))));
	return fract((p.xyzw * p.yzwx) * 2365.952041) * 2.0 - 1.0;
}

float noise(vec4 coord) {
	// Domain rotation to improve the look of XYZ slices + animation patterns.
	coord = vec4(
			coord.xyz + dot(coord, vec4(vec3(-0.1666667), -0.5)),
			dot(coord, vec4(0.5)));

	vec4 base = floor(coord), delta = coord - base;

	vec4 grad_0000 = grad(base + vec4(0.0, 0.0, 0.0, 0.0)), grad_1000 = grad(base + vec4(1.0, 0.0, 0.0, 0.0));
	vec4 grad_0100 = grad(base + vec4(0.0, 1.0, 0.0, 0.0)), grad_1100 = grad(base + vec4(1.0, 1.0, 0.0, 0.0));
	vec4 grad_0010 = grad(base + vec4(0.0, 0.0, 1.0, 0.0)), grad_1010 = grad(base + vec4(1.0, 0.0, 1.0, 0.0));
	vec4 grad_0110 = grad(base + vec4(0.0, 1.0, 1.0, 0.0)), grad_1110 = grad(base + vec4(1.0, 1.0, 1.0, 0.0));
	vec4 grad_0001 = grad(base + vec4(0.0, 0.0, 0.0, 1.0)), grad_1001 = grad(base + vec4(1.0, 0.0, 0.0, 1.0));
	vec4 grad_0101 = grad(base + vec4(0.0, 1.0, 0.0, 1.0)), grad_1101 = grad(base + vec4(1.0, 1.0, 0.0, 1.0));
	vec4 grad_0011 = grad(base + vec4(0.0, 0.0, 1.0, 1.0)), grad_1011 = grad(base + vec4(1.0, 0.0, 1.0, 1.0));
	vec4 grad_0111 = grad(base + vec4(0.0, 1.0, 1.0, 1.0)), grad_1111 = grad(base + vec4(1.0, 1.0, 1.0, 1.0));

	vec4 result_0123 = vec4(
			dot(delta - vec4(0.0, 0.0, 0.0, 0.0), grad_0000), dot(delta - vec4(1.0, 0.0, 0.0, 0.0), grad_1000),
			dot(delta - vec4(0.0, 1.0, 0.0, 0.0), grad_0100), dot(delta - vec4(1.0, 1.0, 0.0, 0.0), grad_1100));
	vec4 result_4567 = vec4(
			dot(delta - vec4(0.0, 0.0, 1.0, 0.0), grad_0010), dot(delta - vec4(1.0, 0.0, 1.0, 0.0), grad_1010),
			dot(delta - vec4(0.0, 1.0, 1.0, 0.0), grad_0110), dot(delta - vec4(1.0, 1.0, 1.0, 0.0), grad_1110));
	vec4 result_89AB = vec4(
			dot(delta - vec4(0.0, 0.0, 0.0, 1.0), grad_0001), dot(delta - vec4(1.0, 0.0, 0.0, 1.0), grad_1001),
			dot(delta - vec4(0.0, 1.0, 0.0, 1.0), grad_0101), dot(delta - vec4(1.0, 1.0, 0.0, 1.0), grad_1101));
	vec4 result_CDEF = vec4(
			dot(delta - vec4(0.0, 0.0, 1.0, 1.0), grad_0011), dot(delta - vec4(1.0, 0.0, 1.0, 1.0), grad_1011),
			dot(delta - vec4(0.0, 1.0, 1.0, 1.0), grad_0111), dot(delta - vec4(1.0, 1.0, 1.0, 1.0), grad_1111));

	vec4 fade = delta * delta * delta * (10.0 + delta * (-15.0 + delta * 6.0));
	vec4 result_W0 = mix(result_0123, result_89AB, fade.w), result_W1 = mix(result_4567, result_CDEF, fade.w);
	vec4 result_WZ = mix(result_W0, result_W1, fade.z);
	vec2 result_WZY = mix(result_WZ.xy, result_WZ.zw, fade.y);
	return mix(result_WZY.x, result_WZY.y, fade.x);
}

// Curl 3D and three-noise function with friendly permission by Isaac Cohen.
// Modified to accept 4D noise.
vec3 noise_3x(vec4 p) {
	float s = noise(p);
	float s1 = noise(p + vec4(vec3(0.0), 1.7320508 * 2048.333333));
	float s2 = noise(p - vec4(vec3(0.0), 1.7320508 * 2048.333333));
	vec3 c = vec3(s, s1, s2);
	return c;
}

vec3 curl_3d(vec4 p, float c) {
	float epsilon = 0.001 + c;
	vec4 dx = vec4(epsilon, 0.0, 0.0, 0.0);
	vec4 dy = vec4(0.0, epsilon, 0.0, 0.0);
	vec4 dz = vec4(0.0, 0.0, epsilon, 0.0);
	vec3 x0 = noise_3x(p - dx).xyz;
	vec3 x1 = noise_3x(p + dx).xyz;
	vec3 y0 = noise_3x(p - dy).xyz;
	vec3 y1 = noise_3x(p + dy).xyz;
	vec3 z0 = noise_3x(p - dz).xyz;
	vec3 z1 = noise_3x(p + dz).xyz;
	float x = (y1.z - y0.z) - (z1.y - z0.y);
	float y = (z1.x - z0.x) - (x1.z - x0.z);
	float z = (x1.y - x0.y) - (y1.x - y0.x);
	return normalize(vec3(x, y, z));
}

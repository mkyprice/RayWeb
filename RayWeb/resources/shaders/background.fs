// Adapted from https://www.shadertoy.com/view/Wt33Wf

#version 330

#define PI 3.14159265359

uniform vec2 u_resolution;
uniform float Time;

float sun(vec2 uv, float battery)
{
 	float val = smoothstep(0.3, 0.29, length(uv));
 	float bloom = smoothstep(0.7, 0.0, length(uv));
    float cut = 3.0 * sin((uv.y + Time * 0.02 * (battery + 0.02)) * 100.0) 
				+ clamp(uv.y * 14.0 + 1.0, -6.0, 6.0);
    cut = clamp(cut, 0.0, 1.0);
    return clamp(val * cut, 0.0, 1.0) + bloom * 0.6;
}

float grid(vec2 uv, float battery)
{
    vec2 size = vec2(uv.y, uv.y * uv.y * 0.2) * 0.01;
    uv += vec2(0.0, Time * 4.0 * (battery + 0.05));
    uv = abs(fract(uv) - 0.5);
 	vec2 lines = smoothstep(size, vec2(0.0), uv);
 	lines += smoothstep(size * 5.0, vec2(0.0), uv) * 0.4 * battery;
    return clamp(lines.x + lines.y, 0.0, 3.0);
}

void main()
{
    vec2 uv = (2.0 * gl_FragCoord.xy - u_resolution.xy)/u_resolution.y;
    float battery = 1.0;
    // Grid
    float fog = smoothstep(0.1, -0.02, abs(uv.y + 0.2));
    vec3 col = vec3(0.0, 0.1, 0.2);
    if (uv.y < -0.2)
    {
        uv.y = 3.0 / (abs(uv.y + 0.2) + 0.05);
        uv.x *= uv.y * 1.0;
        float gridVal = grid(uv, battery);
        col = mix(col, vec3(1.0, 0.5, 1.0), gridVal);
    }
    else
    {
        vec2 sunUV = uv;
        sunUV += vec2(0, 0.1);
        col = vec3(1.0, 0.2, 1.0);
        float sunVal = sun(sunUV, battery);
        col = mix(col, vec3(1.0, 0.4, 0.1), sunUV.y * 2.0 + 0.2);
        col = mix(vec3(0.0, 0.0, 0.0), col, sunVal);
    }

    col += fog * fog * fog;
    col = mix(vec3(col.r, col.r, col.r) * 0.5, col, battery * 0.7);

    gl_FragColor = vec4(col,1.0);
}
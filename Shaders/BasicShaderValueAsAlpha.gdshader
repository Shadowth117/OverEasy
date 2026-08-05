shader_type spatial;

// Material Properties (StandardMaterial equivalents)
uniform sampler2D albedo_texture : source_color;

void fragment() {
    vec4 tex = texture(albedo_texture, UV);
    ALBEDO = tex.rgb * 3.0f;
	float avg = tex.r + tex.g + tex.b;
	//avg = avg / 3.0f;
	ALPHA = avg; 
}

void light() {
    float dotProduct = dot(NORMAL, LIGHT);
    float diffuseIntensity = clamp(dotProduct, 0.0, 1.0);
    vec3 diffuseLight = ALBEDO * diffuseIntensity;

    DIFFUSE_LIGHT += (diffuseLight) * LIGHT_COLOR * ATTENUATION;
}
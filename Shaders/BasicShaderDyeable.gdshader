shader_type spatial;

// Material Properties (StandardMaterial equivalents)
uniform sampler2D albedoTexture : source_color;
instance uniform vec4 dyeColor : source_color = vec4(1.0);

void fragment() {
    vec4 tex = texture(albedoTexture, UV);
    ALBEDO = tex.rgb * dyeColor.rgb;
}

void light() {
    float dotProduct = dot(NORMAL, LIGHT);
    float diffuseIntensity = clamp(dotProduct, 0.0, 1.0);
    vec3 diffuseLight = ALBEDO * diffuseIntensity;

    DIFFUSE_LIGHT += (diffuseLight) * LIGHT_COLOR * ATTENUATION;
}
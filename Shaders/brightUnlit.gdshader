shader_type spatial;
render_mode unshaded;

uniform sampler2D albedoTexture : source_color;
uniform float albedoMultiplier; //Determines how much albedo gets multiplied by and brightened.

void fragment() {
    ALBEDO = texture(albedoTexture, UV).rgb * albedoMultiplier;
}
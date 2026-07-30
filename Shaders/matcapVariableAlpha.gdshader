//Taken with very slight modification from https://godotshaders.com/shader/matcap-shader/

shader_type spatial;
render_mode unshaded; // remove this if you wish to make it shaded

uniform sampler2D matcap : source_color, hint_default_black; // Sampler 2d of the matcap.
uniform float alphaValue; // Determines amount of alpha transparency this has.
uniform float albedoMultiplier; //Determines how much albedo gets multiplied by and brightened.

// Bread and butter: Generates the MatCap's UV's
vec2 generate_matcap_uv(vec3 _normal, vec3 _node_position_view) {
	vec3 cross_space = cross(normalize(_node_position_view), _normal); // cross products the normal with it's base position
	vec2 mat_space = vec2(cross_space.y * 1.0, cross_space.x); // converts to a Vec2 UV, having to switch x and y
	mat_space *= vec2(0.5, -0.5); // modifies the mat space's size to fit into uv coords, & flips it
	mat_space += vec2(0.5, 0.5); // transforms the mat space into uv coords
	return mat_space; // export the final uv's
}

void fragment(){
	vec2 matcap_uv = (generate_matcap_uv(NORMAL, NODE_POSITION_VIEW)); // Matcap UV's
	vec4 _matcap = texture(matcap, matcap_uv); // Matcap Texture
	ALBEDO = _matcap.rgb * albedoMultiplier; // Sets the albedo to the matcap texture. 
	ALPHA = alphaValue;
}


//// Additional Information: \\\\\

// NOTE: this shader isn't perfect, sadly. There's still something causing some minor distortion on the material.
// I'll try to update this with the fix if it ever gets found.
// Feel free to improve upon this! Let me know if you do, too!

// If you want to make it make it take your normalmap into account, replace the [vec3 _matcap = ...] with:
//
// vec3 _matcap = texture(matcap, matcap_uv + ((texture([INSERT NORMALMAP], UV).xy - 0.5) * [INFLUENCE])).rgb;
// 
// Replacing the [INSERT NORMALMAP] with your sampler2D normalmap 
// and [INFLUENCE] with a float. 
// 
// The [.xy - 0.5] part maps the normal from vec3 [0 : 1] range into a vec2 [-0.5 : 0.5] range. 
// You can correct it into a [-1 : 1] range by multiplying it by 2, but I found it's not really nessessary here.

// If you want to use a Metalness map or a Roughness map, or both, to influence the matcap:
// insert this to blend between the _matcap and a value like so:
//
// _matcap = mix(vec3(0.0), _matcap, clamp([metalness] + (1 - [roughness]), 0, 1);
//
// The example above is a super simple blend between the metal value, and the inverse rough value. 
// Effectively meaning that the more metallic and more shiny the object, the more matcap it'll have
// You'll want to augment it and make it more complex than that though. It's just a quick example.

// If you want to add onto the default shading, you'll want to add the _matcap 
// to your albedo or emissive layer instead of setting it like it is in this file. IE: 
//
// ALBEDO = texture(albedo, UV)
// ALBEDO += _matcap
// EMISSION += _matcap

// Or multiply it if you want it to be more of a 'flashlight'/'shadow' kind of effect instead of a 
// light.
// ALBEDO = texture(albedo, UV)
// ALBEDO *= _matcap

// or do both at the same time! Mix n Match! It's all up to you now :3
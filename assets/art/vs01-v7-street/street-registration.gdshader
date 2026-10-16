shader_type canvas_item;

// Register the generated artwork to the unchanged 1600x720 navigation.
// These are texture coordinates only: physics, anchors and saves are untouched.
float image_x(float x) {
    if (x < 24.0) return mix(0.0,30.0,x/24.0);
    if (x < 340.0) return mix(30.0,415.0,(x-24.0)/316.0);
    if (x < 450.0) return mix(415.0,535.0,(x-340.0)/110.0);
    if (x < 580.0) return mix(535.0,687.0,(x-450.0)/130.0);
    if (x < 680.0) return mix(687.0,797.0,(x-580.0)/100.0);
    if (x < 940.0) return mix(797.0,1097.0,(x-680.0)/260.0);
    if (x < 1040.0) return mix(1097.0,1195.0,(x-940.0)/100.0);
    if (x < 1150.0) return mix(1195.0,1330.0,(x-1040.0)/110.0);
    if (x < 1260.0) return mix(1330.0,1460.0,(x-1150.0)/110.0);
    if (x < 1340.0) return mix(1460.0,1530.0,(x-1260.0)/80.0);
    if (x < 1576.0) return mix(1530.0,1835.0,(x-1340.0)/236.0);
    return mix(1835.0,1870.0,(x-1576.0)/24.0);
}

float image_y(float y) {
    if (y < 170.0) return mix(0.0,125.0,y/170.0);
    if (y < 300.0) return mix(125.0,225.0,(y-170.0)/130.0);
    if (y < 440.0) return mix(225.0,385.0,(y-300.0)/140.0);
    if (y < 570.0) return mix(385.0,575.0,(y-440.0)/130.0);
    if (y < 620.0) return mix(575.0,685.0,(y-570.0)/50.0);
    if (y < 688.0) return mix(685.0,755.0,(y-620.0)/68.0);
    return mix(755.0,841.0,(y-688.0)/32.0);
}

void fragment() {
    vec2 world_pixel = UV * vec2(1600.0,720.0);
    float registered_y = image_y(world_pixel.y);
    // The NE obstacle ends at y=360 (the middle yard ends at y=440).
    // Preserve that authored shorter island and blend within its curb margin.
    float east_y = registered_y;
    if (world_pixel.y >= 170.0 && world_pixel.y < 360.0) {
        east_y = mix(125.0,385.0,(world_pixel.y-170.0)/190.0);
    } else if (world_pixel.y >= 360.0 && world_pixel.y < 570.0) {
        east_y = mix(385.0,575.0,(world_pixel.y-360.0)/210.0);
    }
    registered_y = mix(registered_y,east_y,smoothstep(1245.0,1260.0,world_pixel.x));
    vec2 image_pixel = vec2(image_x(world_pixel.x),registered_y);
    COLOR = texture(TEXTURE,image_pixel/vec2(1870.0,841.0));
}

extends SceneTree

var results = []

func capture_image(image, settings):
 settings["format"] = image.get_format()
 settings["mipmaps"] = image.has_mipmaps()
 settings["data"] = image.get_data().hex_encode()
 results.append(settings)

func _initialize():
 call_deferred("run")

func run():
 for size in [Vector2i(1, 1), Vector2i(7, 9), Vector2i(20, 14), Vector2i(8, 8), Vector2i(32, 32), Vector2i(64, 32), Vector2i(13, 21)]:
  var noise = FastNoiseLite.new()
  noise.seed = 19
  noise.frequency = 0.2
  noise.fractal_type = FastNoiseLite.FRACTAL_NONE
  for invert in [false, true]:
   for normalize in [false, true]:
    capture_image(noise.get_image(size.x, size.y, invert, false, normalize), {"kind":"image", "width":size.x, "height":size.y, "invert":invert, "normalize":normalize})
   for skirt in [0.0, 0.1, 0.25, 0.35, 0.5]:
    capture_image(noise.get_seamless_image(size.x, size.y, invert, false, skirt, true), {"kind":"seamless", "width":size.x, "height":size.y, "invert":invert, "normalize":true, "skirt":skirt})
 for size in [Vector2i(7, 9), Vector2i(16, 16)]:
  for seamless in [false, true]:
   for mode in range(6):
    var noise = FastNoiseLite.new()
    noise.seed = 19
    noise.frequency = 0.2
    noise.fractal_type = FastNoiseLite.FRACTAL_NONE
    var texture = NoiseTexture2D.new()
    texture.width = size.x
    texture.height = size.y
    texture.generate_mipmaps = mode in [3, 4, 5]
    texture.seamless = seamless
    texture.seamless_blend_skirt = 0.35
    texture.as_normal_map = mode in [2, 3]
    texture.bump_strength = 2.5
    if mode in [1, 3, 4]:
     var ramp = Gradient.new()
     ramp.colors = PackedColorArray([Color(0.1, 0.7, 0.2, 0.3), Color(0.9, 0.2, 0.8, 0.9)])
     texture.color_ramp = ramp
    texture.noise = noise
    await texture.changed
    capture_image(texture.get_image(), {"kind":"texture", "width":size.x, "height":size.y, "seamless":seamless, "skirt":0.35, "mode":mode})
 var output = {"commit":"ed1daf0bf001b61586d9930840f2f1394092c079", "version":Engine.get_version_info().string, "cases":results}
 FileAccess.open(OS.get_cmdline_user_args()[0], FileAccess.WRITE).store_string(JSON.stringify(output))
 quit()

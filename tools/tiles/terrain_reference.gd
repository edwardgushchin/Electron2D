extends SceneTree

var output = {"commit":"ed1daf0bf001b61586d9930840f2f1394092c079","cases":[]}

func capture(layer, operation, coords, terrain, ignore_empty):
 if operation == "clear":
  layer.clear()
 elif operation == "connect":
  layer.set_cells_terrain_connect(coords, 0, terrain, ignore_empty)
 else:
  layer.set_cells_terrain_path(coords, 0, terrain, ignore_empty)
 var cells = layer.get_used_cells()
 cells.sort()
 var rows = []
 for cell in cells:
  var data = layer.get_cell_tile_data(cell)
  var bits = []
  for bit in range(16):
   bits.append(data.get_terrain_peering_bit(bit) if data.is_valid_terrain_peering_bit(bit) else -1)
  var atlas = layer.get_cell_atlas_coords(cell)
  rows.append([cell.x, cell.y, layer.get_cell_source_id(cell), atlas.x, atlas.y, layer.get_cell_alternative_tile(cell), data.terrain, bits])
 var points = []
 for cell in coords:
  points.append([cell.x, cell.y])
 return {"operation":operation,"input":points,"terrain":terrain,"ignore_empty":ignore_empty,"cells":rows}

func _initialize():
 output["version"] = Engine.get_version_info().string
 for shape in range(4):
  for layout in range(6):
   for axis in range(2):
    for mode in range(3):
     var set = TileSet.new()
     set.tile_shape = shape
     set.tile_layout = layout
     set.tile_offset_axis = axis
     set.add_terrain_set()
     set.set_terrain_set_mode(0, mode)
     set.add_terrain(0)
     var atlas = TileSetAtlasSource.new()
     atlas.texture_region_size = Vector2i.ONE
     var image = Image.create_empty(3, 1, false, Image.FORMAT_RGBA8)
     atlas.texture = ImageTexture.create_from_image(image)
     set.add_source(atlas, 0)
     atlas.create_tile(Vector2i.ZERO)
     var probe = atlas.get_tile_data(Vector2i.ZERO, 0)
     probe.terrain_set = 0
     var bits = []
     for bit in range(16):
      if probe.is_valid_terrain_peering_bit(bit):
       bits.append(bit)
     var count = 1 << bits.size()
     for index in range(count * 2):
      var base = Vector2i(index / 4000, 0)
      var alternative = index % 4000
      if alternative == 0:
       if index > 0:
        atlas.create_tile(base)
      else:
       atlas.create_alternative_tile(base, alternative)
      var data = atlas.get_tile_data(base, alternative)
      data.terrain_set = 0
      data.terrain = -1 if index < count else 0
      for b in range(bits.size()):
       data.set_terrain_peering_bit(bits[b], 0 if index % count & (1 << b) else -1)
     var layer = TileMapLayer.new()
     layer.tile_set = set
     var first = 0 if shape == 0 or (shape > 1 and axis == 0) else 2
     var second = 4 if shape == 0 or (shape > 1 and axis == 1) else (6 if shape == 1 else 2)
     var a = Vector2i(-2, -1)
     var b = layer.get_neighbor_cell(a, first)
     var c = layer.get_neighbor_cell(a, second)
     var d = layer.get_neighbor_cell(b, second)
     var e = layer.get_neighbor_cell(d, first)
     var steps = []
     steps.append(capture(layer, "connect", [a, c, e], 0, true))
     steps.append(capture(layer, "connect", [b, d], 0, true))
     steps.append(capture(layer, "connect", [b], -1, false))
     steps.append(capture(layer, "clear", [], 0, true))
     steps.append(capture(layer, "connect", [a, b, c, d, e, a], 0, false))
     steps.append(capture(layer, "clear", [], 0, true))
     steps.append(capture(layer, "path", [c, a, b, d, e], 0, true))
     steps.append(capture(layer, "path", [a, b], -1, false))
     output.cases.append({"shape":shape,"layout":layout,"axis":axis,"mode":mode,"bits":bits,"steps":steps})
     layer.free()
 FileAccess.open(OS.get_cmdline_user_args()[0], FileAccess.WRITE).store_string(JSON.stringify(output))
 quit()

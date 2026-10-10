extends SceneTree

func _initialize():
 var cases = []
 for shape in range(4):
  for layout in range(6):
   for axis in range(2):
    var tile_set = TileSet.new()
    tile_set.tile_shape = shape
    tile_set.tile_layout = layout
    tile_set.tile_offset_axis = axis
    tile_set.tile_size = Vector2i(30, 22)
    var layer = TileMapLayer.new()
    layer.tile_set = tile_set
    var directions = [0, 3, 4, 7, 8, 11, 12, 15] if shape == 0 else ([1, 2, 5, 6, 9, 10, 13, 14] if shape == 1 else ([0, 2, 6, 8, 10, 14] if axis == 0 else [2, 4, 6, 10, 12, 14]))
    var cells = []
    var positions = []
    for y in range(-4, 5):
     for x in range(-4, 5):
      var coords = Vector2i(x, y)
      var center = layer.map_to_local(coords)
      var neighbors = []
      for direction in directions:
       var next = layer.get_neighbor_cell(coords, direction)
       neighbors.append([direction, next.x, next.y])
      var around = []
      for next in layer.get_surrounding_cells(coords):
       around.append([next.x, next.y])
      cells.append([x, y, center.x, center.y, neighbors, around])
    for y in range(-12, 13):
     for x in range(-12, 13):
      var position = Vector2(x * 7.5, y * 5.5)
      var cell = layer.local_to_map(position)
      positions.append([position.x, position.y, cell.x, cell.y])
    # Include either side of vertical, horizontal and sloping edges, plus square bias.
    for point in [Vector2.ZERO, Vector2(30, 22), Vector2(15, 0), Vector2(0, 11), Vector2(7.5, 5.5), Vector2(22.5, 5.5), Vector2(-15, -11)]:
     for shift in [Vector2.ZERO, Vector2(0.00001, 0), Vector2(-0.00001, 0), Vector2(0, 0.00001), Vector2(0, -0.00001), Vector2(-0.001, -0.001), Vector2(0.001, 0.001)]:
      var position = point + shift
      var cell = layer.local_to_map(position)
      positions.append([position.x, position.y, cell.x, cell.y])
    cases.append({"shape":shape,"layout":layout,"axis":axis,"cells":cells,"positions":positions})
    layer.free()
 var output = {"commit":"ed1daf0bf001b61586d9930840f2f1394092c079","version":Engine.get_version_info().string,"size":[30,22],"cases":cases}
 FileAccess.open(OS.get_cmdline_user_args()[0], FileAccess.WRITE).store_string(JSON.stringify(output))
 quit()

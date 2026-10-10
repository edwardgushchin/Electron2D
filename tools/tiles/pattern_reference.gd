# Pinned development oracle; not shipped runtime code.
extends SceneTree

func rows(object):
	var result = []
	for cell in object.get_used_cells():
		var atlas = object.get_cell_atlas_coords(cell)
		result.append([cell.x, cell.y, object.get_cell_source_id(cell), atlas.x, atlas.y, object.get_cell_alternative_tile(cell)])
	result.sort_custom(func(a, b): return a[0] < b[0] or a[0] == b[0] and a[1] < b[1])
	return result

func _initialize():
	var cases = []
	var selections = [
		[Vector2i(-3, -3), Vector2i(-3, -2), Vector2i(-2, -3), Vector2i(-1, -1), Vector2i(-3, -3)],
		[Vector2i(-2, -2), Vector2i(-2, -1), Vector2i(-1, -2), Vector2i(1, 1), Vector2i(-2, -2)],
		[Vector2i(-3, -3), Vector2i(-3, -2), Vector2i(-4, -2), Vector2i(-5, -5)]]
	for shape in range(4):
		for layout in range(6):
			for axis in range(2):
				for selected in selections:
					var tiles = TileSet.new()
					tiles.tile_shape = shape
					tiles.tile_layout = layout
					tiles.tile_offset_axis = axis
					var layer = TileMapLayer.new()
					layer.tile_set = tiles
					for i in range(selected.size() - 1):
						layer.set_cell(selected[i], i + 1, Vector2i(i, 2), i * 4096)
					var pattern = layer.get_pattern(selected)
					var mappings = []
					for anchor in [Vector2i(5, 5), Vector2i(-3, -3), Vector2i(4, 4), Vector2i(-4, -4)]:
						var mapped = []
						for cell in pattern.get_used_cells():
							var destination = layer.map_pattern(anchor, cell, pattern)
							mapped.append([cell.x, cell.y, destination.x, destination.y])
						mapped.sort_custom(func(a, b): return a[0] < b[0] or a[0] == b[0] and a[1] < b[1])
						layer.clear()
						layer.set_cell(anchor, 20, Vector2i(1, 1), 0)
						layer.set_pattern(anchor, pattern)
						mappings.append({"anchor": [anchor.x, anchor.y], "mapped": mapped, "placed": rows(layer)})
					var selection = []
					for cell in selected: selection.append([cell.x, cell.y])
					cases.append({"shape": shape, "layout": layout, "axis": axis, "selection": selection, "size": [pattern.get_size().x, pattern.get_size().y], "cells": rows(pattern), "placements": mappings})
					layer.free()
	var output = FileAccess.open(OS.get_cmdline_user_args()[0], FileAccess.WRITE)
	output.store_string(JSON.stringify({"pin": "ed1daf0bf001b61586d9930840f2f1394092c079", "cases": cases}))
	output.close()
	print("Pattern oracle: ", cases.size(), " captures, ", cases.size() * 4, " placements")
	quit()

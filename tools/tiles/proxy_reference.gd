# Pinned development oracle; not shipped runtime code.
extends SceneTree

func _initialize():
	var set = TileSet.new()
	var atlas = TileSetAtlasSource.new()
	atlas.texture = ImageTexture.create_from_image(Image.create_empty(16,16,false,Image.FORMAT_RGBA8))
	atlas.create_tile(Vector2i.ZERO)
	set.add_source(atlas, 0)
	var probes = [[0,0,0,0], [0,0,0,4096], [0,0,0,1], [0,9,9,0], [100,0,0,0], [100,0,0,1], [100,0,0,-1], [100,1,0,8192], [200,0,0,0], [-1,-1,-1,-1]]
	var actions = [
		["source",[100,200]], ["source",[200,100]], ["source",[0,9]],
		["coords",[100,0,0,3,4,5]], ["coords",[0,9,9,4,2,3]],
		["alternative",[100,0,0,0,8,6,7,16384]], ["alternative",[100,0,0,-1,7,0,0,-1]], ["alternative",[0,0,0,1,9,1,2,3]],
		["remove_alternative",[100,0,0,0]], ["remove_coords",[100,0,0]], ["remove_source",[100]],
		["cleanup",[]], ["add_alt",[]], ["cleanup",[]], ["rename",[]], ["source",[0,5]], ["cleanup",[]], ["clear",[]], ["clear",[]]]
	var stages = []
	for action in actions:
		var a = action[1]
		match action[0]:
			"source": set.set_source_level_tile_proxy(a[0], a[1])
			"coords": set.set_coords_level_tile_proxy(a[0], Vector2i(a[1],a[2]), a[3], Vector2i(a[4],a[5]))
			"alternative": set.set_alternative_level_tile_proxy(a[0], Vector2i(a[1],a[2]), a[3], a[4], Vector2i(a[5],a[6]), a[7])
			"remove_source": set.remove_source_level_tile_proxy(a[0])
			"remove_coords": set.remove_coords_level_tile_proxy(a[0], Vector2i(a[1],a[2]))
			"remove_alternative": set.remove_alternative_level_tile_proxy(a[0], Vector2i(a[1],a[2]), a[3])
			"cleanup": set.cleanup_invalid_tile_proxies()
			"clear": set.clear_tile_proxies()
			"add_alt": atlas.create_alternative_tile(Vector2i.ZERO, 1)
			"rename": set.set_source_id(0, 5)
		var mapped = []
		for probe in probes:
			var value = set.map_tile_proxy(probe[0], Vector2i(probe[1],probe[2]), probe[3])
			mapped.append([value[0],value[1].x,value[1].y,value[2]])
		var sources = []
		for key in [0,100,200]: sources.append([key,set.has_source_level_tile_proxy(key),set.get_source_level_tile_proxy(key) if set.has_source_level_tile_proxy(key) else -1])
		var coords = []
		for key in [[100,0,0],[0,9,9]]:
			var present = set.has_coords_level_tile_proxy(key[0],Vector2i(key[1],key[2]))
			var value = set.get_coords_level_tile_proxy(key[0],Vector2i(key[1],key[2])) if present else [-1,Vector2i(-1,-1)]
			coords.append([key[0],key[1],key[2],present,value[0],value[1].x,value[1].y])
		var alternatives = []
		for key in [[100,0,0,0],[100,0,0,-1],[0,0,0,1]]:
			var present = set.has_alternative_level_tile_proxy(key[0],Vector2i(key[1],key[2]),key[3])
			var value = set.get_alternative_level_tile_proxy(key[0],Vector2i(key[1],key[2]),key[3]) if present else [-1,Vector2i(-1,-1),-1]
			alternatives.append([key[0],key[1],key[2],key[3],present,value[0],value[1].x,value[1].y,value[2]])
		stages.append({"action":action,"mapped":mapped,"sources":sources,"coords":coords,"alternatives":alternatives})
	var file = FileAccess.open(OS.get_cmdline_user_args()[0],FileAccess.WRITE)
	file.store_string(JSON.stringify({"pin":"ed1daf0bf001b61586d9930840f2f1394092c079","probes":probes,"stages":stages})); file.close()
	print("Tile proxy oracle: ", stages.size(), " stages, ", stages.size()*probes.size(), " mapped identities")
	quit()

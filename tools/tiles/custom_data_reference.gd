# Pinned development oracle; not shipped runtime code.
extends SceneTree

func encoded(value):
	match typeof(value):
		TYPE_VECTOR2, TYPE_VECTOR2I: return [value.x,value.y]
		TYPE_RECT2, TYPE_RECT2I: return [value.position.x,value.position.y,value.size.x,value.size.y]
		TYPE_VECTOR4, TYPE_VECTOR4I: return [value.x,value.y,value.z,value.w]
		TYPE_COLOR: return [value.r,value.g,value.b,value.a]
		TYPE_TRANSFORM2D: return [value.x.x,value.x.y,value.y.x,value.y.y,value.origin.x,value.origin.y]
	return value

func _initialize():
	var set = TileSet.new()
	var atlas = TileSetAtlasSource.new()
	atlas.texture = ImageTexture.create_from_image(Image.create_empty(16,16,false,Image.FORMAT_RGBA8))
	atlas.create_tile(Vector2i.ZERO); set.add_source(atlas,0)
	var data = atlas.get_tile_data(Vector2i.ZERO,0)
	var types = {"nil":TYPE_NIL,"bool":TYPE_BOOL,"int":TYPE_INT,"float":TYPE_FLOAT,"string":TYPE_STRING,"vector":TYPE_VECTOR2,"vector_i":TYPE_VECTOR2I,"rect":TYPE_RECT2,"rect_i":TYPE_RECT2I,"vector4":TYPE_VECTOR4,"vector4_i":TYPE_VECTOR4I,"color":TYPE_COLOR,"transform":TYPE_TRANSFORM2D}
	var actions = [["add",[-1]],["name",[0,"A"]],["type",[0,"bool"]],["set",[0,true]],["type",[0,"int"]],["set",[0,12.75]],["type",[0,"int"]],["type",[0,"float"]],["type",[0,"string"]],["set",[0,"1dog2.8"]],["type",[0,"int"]],["set",[0," -12.75e1suffix"]],["type",[0,"float"]],["type",[0,"vector"]],["value_vector",[0]],["type",[0,"vector_i"]],["type",[0,"vector"]],["type",[0,"color"]],["set",[0,"#204080"]],["type",[0,"color"]],["type",[0,"transform"]],["type",[0,"nil"]],["add",[-1]],["name",[1,"B"]],["set",[1,77]],["add",[0]],["name",[0,"C"]],["move",[2,0]],["remove",[1]],["name",[0,"Renamed"]]]
	var stages = []
	for action in actions:
		var a = action[1]
		match action[0]:
			"add": set.add_custom_data_layer(a[0])
			"name": set.set_custom_data_layer_name(a[0],a[1])
			"type": set.set_custom_data_layer_type(a[0],types[a[1]])
			"set": data.set_custom_data_by_layer_id(a[0],a[1])
			"value_vector": data.set_custom_data_by_layer_id(a[0],Vector2(2.75,-3.25))
			"move": set.move_custom_data_layer(a[0],a[1])
			"remove": set.remove_custom_data_layer(a[0])
		var layers = []
		for i in range(set.get_custom_data_layers_count()):
			var value = data.get_custom_data_by_layer_id(i)
			layers.append({"name":set.get_custom_data_layer_name(i),"schema":set.get_custom_data_layer_type(i),"actual":typeof(value),"value":encoded(value)})
		stages.append({"action":action,"layers":layers,"lookup":[set.get_custom_data_layer_by_name("A"),set.get_custom_data_layer_by_name("B"),set.get_custom_data_layer_by_name("C"),set.get_custom_data_layer_by_name("Renamed")]})
	var file = FileAccess.open(OS.get_cmdline_user_args()[0],FileAccess.WRITE)
	file.store_string(JSON.stringify({"pin":"ed1daf0bf001b61586d9930840f2f1394092c079","types":types,"stages":stages}));file.close();print("Custom data oracle: ",stages.size()," stages");quit()

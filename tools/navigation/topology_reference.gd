extends SceneTree

func _initialize():
 call_deferred("run")

func vector(value):
 return Vector2(value[0], value[1])

func pair(value):
 return [value.x, value.y]

func counts():
 var values=[]
 for i in range(10): values.append(NavigationServer2D.get_process_info(i))
 return values

func run():
 var args=OS.get_cmdline_user_args()
 var cases=JSON.parse_string(FileAccess.get_file_as_string(args[0]))
 var results=[]
 for test in cases:
  var baseline=counts()
  var map=NavigationServer2D.map_create()
  NavigationServer2D.map_set_use_async_iterations(map,false)
  if test.get("active",true): NavigationServer2D.map_set_active(map,true)
  NavigationServer2D.map_set_cell_size(map,test.get("cell_size",1.0))
  # Normalize the constructor's stale initial raster dimensions before comparing the kernel.
  NavigationServer2D.map_set_merge_rasterizer_cell_scale(map,0.0001)
  NavigationServer2D.map_set_merge_rasterizer_cell_scale(map,test.get("scale",0.1))
  NavigationServer2D.map_set_use_edge_connections(map,test.get("connect",false))
  NavigationServer2D.map_set_edge_connection_margin(map,test.get("margin",1.0))
  var regions=[]
  for source in test.regions:
   var polygon=NavigationPolygon.new()
   var vertices=PackedVector2Array()
   var indexes=[]
   for cell in source.cells:
    var index=PackedInt32Array()
    for point in cell:
     index.append(vertices.size())
     vertices.append(vector(point))
    indexes.append(index)
   polygon.vertices=vertices
   for index in indexes: polygon.add_polygon(index)
   var region=NavigationServer2D.region_create()
   regions.append(region)
   NavigationServer2D.region_set_use_async_iterations(region,false)
   NavigationServer2D.region_set_map(region,map)
   NavigationServer2D.region_set_navigation_polygon(region,polygon)
   NavigationServer2D.region_set_enabled(region,source.get("enabled",true))
   NavigationServer2D.region_set_use_edge_connections(region,source.get("connect",true))
   NavigationServer2D.region_set_navigation_layers(region,source.get("layers",1))
  for i in range(4): await physics_frame
  var output={"name":test.name,"counts":counts(),"pathways":[],"raw_path":[],"optimized_path":[]}
  for i in range(10): output.counts[i]-=baseline[i]
  for region in regions:
   var pathways=[]
   for i in range(NavigationServer2D.region_get_connections_count(region)):
    pathways.append([pair(NavigationServer2D.region_get_connection_pathway_start(region,i)),pair(NavigationServer2D.region_get_connection_pathway_end(region,i))])
   output.pathways.append(pathways)
  if test.get("active",true):
   for point in NavigationServer2D.map_get_path(map,vector(test.start),vector(test.end),false,test.get("layers",1)): output.raw_path.append(pair(point))
   for point in NavigationServer2D.map_get_path(map,vector(test.start),vector(test.end),true,test.get("layers",1)): output.optimized_path.append(pair(point))
  results.append(output)
  for region in regions: NavigationServer2D.free_rid(region)
  NavigationServer2D.free_rid(map)
  for i in range(3): await physics_frame
 var file=FileAccess.open(args[1],FileAccess.WRITE)
 file.store_string(JSON.stringify({"commit":Engine.get_version_info().hash,"cases":results},"  "))
 file.close()
 print("Navigation topology reference cases: ",results.size())
 quit()

# must be in object mode
# with no verts selected on the object.

import bpy
import random
import bmesh
from mathutils import Vector

def randomize_vertices_along_random_edge():
    me = bpy.context.active_object.data
    bm = bmesh.new()
    bm.from_mesh(me) 
    for vertex in bm.verts:
        edges_length = len(vertex.link_edges)
        if(edges_length <= 0):
            break;
        edge_index = int(random.uniform(0, edges_length))
        edge = vertex.link_edges[edge_index]
        v_other = edge.other_vert(vertex)
        v1 = me.vertices[vertex.index]      
        v2 = me.vertices[v_other.index]
        v3 = (v2.co - v1.co) * random.uniform(0, 0.5)
        v4 = v1.co + v3
        me.vertices[vertex.index].co = v4

print("--   randomizing vertices along edges    --")
print("")
randomize_vertices_along_random_edge();
print("")
print("-- done randomizing vertices along edges --")

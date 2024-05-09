import open3d as o3d

print("Testing IO for point cloud ...")
# Load saved point cloud and visualize it
pcd_load = o3d.io.read_point_cloud("bunny.xyz")

# Visualization in window
o3d.visualization.draw_geometries([pcd_load])

# Saving point cloud
o3d.io.write_point_cloud("bunny.pts", pcd_load)

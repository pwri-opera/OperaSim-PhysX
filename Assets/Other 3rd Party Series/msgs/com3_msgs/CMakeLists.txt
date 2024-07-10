cmake_minimum_required(VERSION 3.5)
project(com3_msgs)

# Default to C++14
if(NOT CMAKE_CXX_STANDARD)
  set(CMAKE_CXX_STANDARD 14)
endif()

if(CMAKE_COMPILER_IS_GNUCXX OR CMAKE_CXX_COMPILER_ID MATCHES "Clang")
  add_compile_options(-Wall -Wextra -Wpedantic)
endif()

find_package(ament_cmake REQUIRED)
find_package(std_msgs REQUIRED)
find_package(sensor_msgs REQUIRED)
find_package(rosidl_default_generators REQUIRED)

rosidl_generate_interfaces(${PROJECT_NAME}
  "msg/ExcavatorCom3MachineSetting.msg"
  "msg/JointCmd.msg"
  "msg/FluidPressureArray.msg"
  DEPENDENCIES std_msgs sensor_msgs# Add packages that above messages depend on, in this case geometry_msgs for Sphere.msg
)

ament_package()
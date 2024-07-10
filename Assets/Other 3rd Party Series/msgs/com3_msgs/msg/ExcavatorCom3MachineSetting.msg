# engine rpm
# valid number is from 900 to 2500.
float64 engine_rpm

# bool hydraulic_off # false = on, true = on
# bool hydraulic_off # false = on, true = off

# power mode
# false:power_mode, true:eco_mode
bool power_eco_mode

# travel mode of track speed 
# false:rabbit_mode, true:turtle_mode
bool travel_speed_mode

# working_mode_notice for only setting Green and Blue LEDs lighting condition
# false:teleoperation mode, true:automation mode
bool working_mode_notice

# yellow led setting
# 0:Off, 1:Blinking, 2:On
uint8 yellow_led_mode

# control mode of front joints.
# 0:Off, 1:effort, 2:velocity
uint8 front_control_mode

# control mode of tracks.
# 0:Off, 1:effort(lever input), 2:Velocity of each tracks, 3:Velociy of center between tracks.
uint8 tracks_control_mode
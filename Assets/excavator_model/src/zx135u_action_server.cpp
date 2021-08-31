//http://clopema.felk.cvut.cz/redmine/projects/clopema/wiki/Sending_trajectory_to_the_controller
//http://wiki.ros.org/actionlib_tutorials/Tutorials/SimpleActionServer%28ExecuteCallbackMethod%29
//http://wiki.ros.org/actionlib_tutorials/Tutorials/SimpleActionServer%28GoalCallbackMethod%29
#include <ros/ros.h>
#include <actionlib/server/simple_action_server.h>
#include <control_msgs/FollowJointTrajectoryAction.h>
#include <trajectory_msgs/JointTrajectory.h>

class RobotTrajectoryFollower
{
protected:

  ros::NodeHandle nh_;
  // NodeHandle instance must be created before this line. Otherwise strange error may occur.
  actionlib::SimpleActionServer<control_msgs::FollowJointTrajectoryAction> as_; 
  std::string action_name_;

public:

  RobotTrajectoryFollower(std::string name) :
    as_(nh_, name, false),
    action_name_(name)
  {
    //Register callback functions:
    as_.registerGoalCallback(boost::bind(&RobotTrajectoryFollower::goalCB, this));
    as_.registerPreemptCallback(boost::bind(&RobotTrajectoryFollower::preemptCB, this));

    as_.start();
  }

  ~RobotTrajectoryFollower(void)//Destructor
  {
  }

  void goalCB()
  {
    // accept the new goal
    //goal_ = as_.acceptNewGoal()->samples;
  }

  void preemptCB()
  {
    ROS_INFO("%s: Preempted", action_name_.c_str());
    // set the action state to preempted
    as_.setPreempted();
  }
};


int main(int argc, char** argv)
{
  ros::init(argc, argv, "action_server");

  RobotTrajectoryFollower RobotTrajectoryFollower("zx135u/upper_arm_controller/follow_joint_trajectory");
  ros::spin();

  return 0;
}
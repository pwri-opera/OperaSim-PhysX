// Reference: https://blog.nekoteam.com/archives/2596

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

using Unity.Robotics.UrdfImporter;
using Unity.Robotics.Core;

// For Debug
using System.Reflection;

public class UpdateZX120Param : MonoBehaviour
{
	private static System.IO.FileSystemWatcher m_FileSystemWatcher = null;

    [Serializable] // <- JsonUtility を介す際に必要
    public class XDriveParam
    {
        public int joint_stiffness = 200000;
        public int joint_damping = 100000;
    }

    [Serializable] // <- JsonUtility を介す際に必要
    public class Param
    {
        public XDriveParam swing =  new XDriveParam();
        public XDriveParam boom =   new XDriveParam();
        public XDriveParam arm =    new XDriveParam();
        public XDriveParam bucket = new XDriveParam();
    }
	private static List<Param> m_ConfigChangedList = new List<Param>();

    private List<ArticulationBody> joints;
    private List<string> jointNames;

	//----------------------------------------------------------------------------
	private void Awake ()
	{
		m_FileSystemWatcher = new System.IO.FileSystemWatcher(); // FileSystemWatcher を作成
		m_FileSystemWatcher.NotifyFilter = System.IO.NotifyFilters.LastWrite; // 更新日時が変更になったら知らせる設定
		m_FileSystemWatcher.Path = "Assets/StreamConfig/"; // 監視するディレクトリ
		m_FileSystemWatcher.Filter = "zx120_config.json"; // 監視するファイル 

        // 変更時に呼ばれるデリゲート
		m_FileSystemWatcher.Changed += new System.IO.FileSystemEventHandler( ( source, e) =>
		{
			var pash = "Assets/StreamConfig/zx120_config.json";
			string json_text = "";

            Debug.Log("File Changed: " + e.FullPath);

			// ここが呼ばれるのは別スレッドかもしれないので、呼べる関数は制限される
			using( var fileStream = new FileStream( pash, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				using( var reader = new StreamReader( fileStream, System.Text.Encoding.UTF8))
				{
					json_text = reader.ReadToEnd();
				}
			}
			var newConfig = UnityEngine.JsonUtility.FromJson<Param> ( json_text );
            // Debug.Log("raw_json? : " + newConfig.swing.joint_stiffness);
            // Debug.Log("raw_json? : " + json_text);
			m_ConfigChangedList.Add( newConfig);
		});
		m_FileSystemWatcher.EnableRaisingEvents = true; // 監視開始


        joints = new List<ArticulationBody>();
        jointNames = new List<string>();

        foreach (var joint in this.GetComponentsInChildren<ArticulationBody>())
        {
            Type type = joint.GetType();
            FieldInfo[] fields = type.GetFields();
            
            if (joint.isActiveAndEnabled)
            {
                // Debug.Log("type? : " + type);
                // Debug.Log("fields? : " + fields);

                var ujoint = joint.GetComponent<UrdfJoint>();
                if (ujoint && !(ujoint is UrdfJointFixed))
                {
                    joints.Add(joint);
                    jointNames.Add(ujoint.jointName);
                    // Debug.Log("Name? : " + ujoint.jointName);
                }
            }
        }



        // 初回 config file 読み込み
        var pash = "Assets/StreamConfig/zx120_config.json";
		string json_text = "";
        using( var fileStream = new FileStream( pash, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            using( var reader = new StreamReader( fileStream, System.Text.Encoding.UTF8))
            {
                json_text = reader.ReadToEnd();
            }
        }
        var newConfig = UnityEngine.JsonUtility.FromJson<Param> ( json_text );
        UpdateJointParam (newConfig);

	}
	//----------------------------------------------------------------------------
	public void OnDestroy ()
	{
		// 一応後始末
		if( m_FileSystemWatcher != null)
		{
			m_FileSystemWatcher.EnableRaisingEvents = false;
			m_FileSystemWatcher = null;
		}
		m_ConfigChangedList = null;
	}
	//----------------------------------------------------------------------------
	private void Update ()
	{
		foreach( var config in m_ConfigChangedList)
		{
            Debug.Log("is_changed? :" + config.swing.joint_stiffness);
            UpdateJointParam (config);
		}
		m_ConfigChangedList.Clear();
	}


    private void UpdateJointParam (Param cfg)
    {
        for (int i = 0; i < joints.Count; i++)
        {
            var drive = joints[i].xDrive;
            if (jointNames[i] == "swing_joint")
            {
               drive.stiffness = cfg.swing.joint_stiffness;
                drive.damping = cfg.swing.joint_damping;
                drive.forceLimit = 3200000;
                Debug.Log("Swing params are changed to :: stiffness:" + cfg.swing.joint_stiffness + "damping:" + cfg.swing.joint_damping);
            }
            else if (jointNames[i] == "boom_joint")
            {
                drive.stiffness = cfg.boom.joint_stiffness;
                drive.damping = cfg.boom.joint_damping;
                drive.forceLimit = 400000;
                Debug.Log("Swing params are changed to :: stiffness:" + cfg.boom.joint_stiffness + "damping:" + cfg.boom.joint_damping);
            }
            else if (jointNames[i] == "arm_joint")
            {
                drive.stiffness = cfg.arm.joint_stiffness;
                drive.damping = cfg.arm.joint_damping;
                drive.forceLimit = 236187;
                Debug.Log("Swing params are changed to :: stiffness:" + cfg.arm.joint_stiffness + "damping:" + cfg.arm.joint_damping);
            }
            else if (jointNames[i] == "bucket_joint")
            {
                drive.stiffness = cfg.bucket.joint_stiffness;
                drive.damping = cfg.bucket.joint_damping;
                drive.forceLimit = 121770;
                Debug.Log("Swing params are changed to :: stiffness:" + cfg.bucket.joint_stiffness + "damping:" + cfg.bucket.joint_damping);
            }
            else
            {
                Debug.LogWarning("A warning: joints do not exist");
            }
            joints[i].xDrive = drive;
        }
    }
} 

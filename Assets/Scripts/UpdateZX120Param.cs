// Reference: https://blog.nekoteam.com/archives/2596

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class TestClass : MonoBehaviour
{
	private static System.IO.FileSystemWatcher m_FileSystemWatcher = null;
	private struct Config
	{
		public int m_Level;
	} // struct Config
	private static List<Config> m_ConfigChangedList = new List<Config>();

	//----------------------------------------------------------------------------
	private void Awake()
	{
		// FileSystemWatcher を作成
		m_FileSystemWatcher = new System.IO.FileSystemWatcher();
		// 更新日時が変更になったら知らせる設定
		m_FileSystemWatcher.NotifyFilter = System.IO.NotifyFilters.LastWrite;
		// 監視するディレクトリ
		m_FileSystemWatcher.Path = "Assets/StreamConfig/";
		// 監視するファイル
		m_FileSystemWatcher.Filter = "zx120_config.json";
		// 変更時に呼ばれるデリゲート
		m_FileSystemWatcher.Changed += new System.IO.FileSystemEventHandler( ( source, e) =>
		{
			var pash = "Assets/StreamConfig/zx120_config.json";
			string json_text = "";
			// ここが呼ばれるのは別スレッドかもしれないので、呼べる関数は制限される
			using( var fileStream = new FileStream( pash, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				using( var reader = new StreamReader( fileStream, System.Text.Encoding.UTF8))
				{
					json_text = reader.ReadToEnd();
				}
			}
			var newConfig = UnityEngine.JsonUtility.FromJson<Config>( json_text);
			m_ConfigChangedList.Add( newConfig);
		});
		// 監視開始
		m_FileSystemWatcher.EnableRaisingEvents = true;
	}
	//----------------------------------------------------------------------------
	public void OnDestroy()
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
	private void Update()
	{
		foreach( var config in m_ConfigChangedList)
		{
            // joint の stiffness 設定
            joint.swing.stiffness   = config.joint_stiffness.swing;
            joint.boom.stiffness    = config.joint_stiffness.boom;
            joint.arm.stiffness     = config.joint_stiffness.arm;
            joint.bucket.stiffness  = config.joint_stiffness.bucket;
            
            //joint の dumping 設定
            joint.swing.dumping   = config.joint_dumping.swing;
            joint.boom.dumping    = config.joint_dumping.boom;
            joint.arm.dumping     = config.joint_dumping.arm;
            joint.bucket.dumping  = config.joint_dumping.bucket;
		}
		m_ConfigChangedList.Clear();
	}
} // class TestClass

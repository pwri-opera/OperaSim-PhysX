# sim_physx
Simulator on Unity + PhysX communicating with ROS

## 概説
- 本シミュレータは自律施工技術開発基盤OPERA（Open Platform for Earth work with Robotics and Autonomy）の一部であり、どなたでも利用可能です
- シミュレータプラットフォームに[Unity](https://unity.com/)、物理エンジンに[Nvidia PhysX](https://www.nvidia.com/ja-jp/drivers/physx/physx-9-19-0218-driver/)を利用しています
- Unityを利用するため、利用者が所属する組織に応じたUnityのライセンスが必要です。詳細は[Unityの公式サイト](https://store.unity.com/ja)をご確認の上、利用登録をしてください。


![Videotogif](https://user-images.githubusercontent.com/24404939/159425467-c244de28-354e-4d2a-a615-5ccafc7b9709.gif)

## インストール方法
### 1. Unity(ver:2020.3.16f1)のインストール
使用しているPCのOSに応じて以下の通りUnityHubをインストールする


- windows 又は Macの場合: [https://unity3d.com/jp/get-unity/download](https://unity3d.com/jp/get-unity/download)
- Linuxの場合(Linux版は動作確認していない):[https://unity3d.com/get-unity/download](https://unity3d.com/get-unity/download)
 
 上記のサイトにて"Download Unity Hub"をクリックし、最新の`UnityHub.AppImage`をダウンロード後、実行権限を付与する
  ```bash
  $ sudo chmod +x UnityHub.AppImage
  ```
  UnityHub.AppImageを実行する
   ```bash
   $ ./UnityHub.AppImage
   ```
  UnityHubを利用するためのライセンス認証手続きを行った後、Unity Editor（version: `2020.3.16f1`）を以下のアーカイブサイトより選択してインストールする

[https://unity3d.com/get-unity/download/archive](https://unity3d.com/get-unity/download/archive)

### 2. Projectファイルの開き方
- UnityHubを起動し、`sim_physx`をクリックする（初回起動時には数分程度の時間がかかります）

### 3. Sceneファイルの選択
- デモ用のサンプルSceneファイルが`Asset/OpenConstructionSim/Scenes/SampleScene.unity`にあるので、これを開く.  

### 4. ROS-TCP-Connectorの設定
- UnityEditorの上部ツールバーからRobotics > ROS Settingを開き"ROS IP Address", "ROS Port"のところにROS側のIPアドレスおよびポート番号(defaultは10000)を入力する

![ros_ip_setting](https://user-images.githubusercontent.com/24404939/159395478-46617a2f-b05c-4227-9fc9-d93712dc4b9f.jpg)

### 5. ROSとの連携方法
![ros-unity](https://user-images.githubusercontent.com/24404939/161001271-0f81d211-4c8e-4341-8f9f-86a02e958c4d.jpg)
- 【初回のみ】ROS側で[ROS-TCP-Endpoint](https://github.com/Unity-Technologies/ROS-TCP-Endpoint)パッケージをcloneし、buildとセットアップを行う。
  ```bash
  $ git clone https://github.com/Unity-Technologies/ROS-TCP-Endpoint.git
  $ cd ./ROS-TCP-Endpoint/
  $ sudo chmod +x setup.py
  $ python setup.py build
  $ cd build/lib/ros_tcp_endpoint
  $ python default_server_endpoint.py
  ```
- ROS Masterを起動する。
  ```bash
  $ roscore  #別のターミナルで実行する必要
  ```
  
  <!--
- ROS側でendpoint.launchを実行する
  ```bash
  $ roslaunch ros_tcp_endpoint endpoint.launch
  ```
-->
- Unity Editor上部の実行ボタンをクリックする

![play_icon](https://user-images.githubusercontent.com/24404939/159396113-993ff0b2-d2bb-4567-ac68-0eafc9f524ac.png)
- ROS側で、対応する建機のunity用launch ファイルを起動する
  - 油圧ショベル
  ```bash
  $ roslaunch zx120_unity zx120_standby.launch
  ```
  - クローラダンプ
  ```bash
  $ roslaunch ic120_unity ic120_standby.launch
  ```
  - 油圧ショベルとクローラダンプの両方
  ```bash
  $ roslaunch zx120_ic120_standby.launch
  ```
 #### ROSと連携時の送受信データ
- Cmd (ROS -> Unity) 

| データの内容 | トピック名 | トピック型 | 物理量 | 単位 | 備考 |
| ----  |  ---- | ---- | ---- | ---- | ---- |
| 建機の移動体部に対する対地速度指令値 | /(建機のns)/tracks/cmd_vel | geometry_msgs/Twist | 速度 | [m/s],[rad/s] |  |
| ダンプトラックの荷台の傾斜角指令値 | /(建機のns)/vessel/cmd | std_msgs/Float64 | 角度 | [rad] |  |
| 建機のスイング軸の角度指令値 | /(建機のns)/swing/cmd | std_msgs/Float64 | 角度 | [rad] |  |
| 建機のブーム軸の角度指令値 | /(建機のns)/boom/cmd | std_msgs/Float64 | 角度 | [rad] |  |
| 建機のアーム軸の角度指令値 | /(建機のns)/arm/cmd | std_msgs/Float64 | 角度 | [rad] |  |
| 建機のバケット軸の角度指令値 | /(建機のns)/bucket/cmd | std_msgs/Float64 | 角度 | [rad] |  |
   
- Res（Unity -> ROS）
     
| データの内容 | トピック名 | トピック型 | 物理量 | 単位 | 備考 |
| ----  |  ---- | ---- | ---- | ---- | ---- |
| 建機のベースリンクの座標 | /(建機のns)  /base_link/pose | geometry_msgs/PoseStamped | 位置・姿勢 | 位置:[m]  姿勢:[-] | Unity内のworld座標系に対する座標の真値 |
| 建機のオドメトリ計算結果 | /(建機のns)  /vessel/cmd | nav_msgs/Odometry | オドメトリ | [rad] | 初期位置を原点として算出している |
| 建機の関節角度・角速度 | /(建機のns)  /joint_states | sensor_msgs/JointState | 角度・角速度 | 角度:[rad]  角速度:[rad/s] | 現在、effortは常に0.0 |

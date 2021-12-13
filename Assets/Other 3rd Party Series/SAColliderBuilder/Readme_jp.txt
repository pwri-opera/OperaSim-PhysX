----------------------------------------------
 SAColliderBuilder
 Copyright (c) 2014 Stereoarts Nora
 Version 1.0.3
 http://stereoarts.jp
 stereoarts.nora@gmail.com
----------------------------------------------

SACollider Builderをご購入頂きありがとうございます。

本ソフトウェアについての、ご意見ご要望等がございましたら、
メール(stereoarts.nora@gmail.com)もしくはツイッター(https://twitter.com/Stereoarts)にて
お問い合わせをよろしくお願い致します。

----------------------------------------------
 使用方法
----------------------------------------------

SAMeshColliderBuilder

- 任意のMeshFilterをマテリアル/プリミティブ/ポリゴンの境界で分離し、コリジョンを生成します。
- 任意のMeshRenderer/SkinnedMeshRendererを子にもつ GameObject にコンポーネントを追加します。
- "Process"ボタンを押すと、コリジョンが生成されます。

SABoneColliderBuilder

- 任意のSkinnedMeshRendererをボーンの重みで分離し、コリジョンを生成します。
- 任意のSkinnedMeshRendererを子にもつ GameObject にコンポーネントを追加します。
- "Process"ボタンを押すと、コリジョンが生成されます。

----------------------------------------------
 Split オプション(Mesh Collider)
----------------------------------------------

- Split Material
マテリアル境界でメッシュを分離します。

- Split Primitive
連続しないプリミティブ境界でメッシュを分離します。

- Split Polygon Normal
ポリゴン法線の一定以上異なる箇所でメッシュを分離します。

----------------------------------------------
Split オプション(Bone Collider)
----------------------------------------------

- Bone Weight
抽出対象のボーンの重みを指定します。

- Greater Bone Weight
頂点ごとに、最もボーンの重みのある頂点を優先して抽出します。

- Bone Triangle Extent
抽出された頂点を、三角形ポリゴンに拡張する方法を指定します。
	Disable ... 拡張なし
	Vertex 2 ... 対象三角形の 2 頂点以上含む場合に, その三角形を有効化します。
	Vertex 1 ... 対象三角形の 1 頂点以上含む場合に, その三角形を有効化します。

----------------------------------------------
Reducer オプション
----------------------------------------------

- Shape Type
	None ... コライダーを生成しません。
	Mesh ... MeshColliderを生成します。
	Box ... BoxColliderを生成します。
	Capsule ... CapsuleColliderを生成します。
	Sphere ... SphereColliderを生成します。

- Fit Type(Sphere, Capsule)
	Innter ... AABBバウンディングボックスに内接するようにコライダーを生成します。
	Outer ... AABBバウンディングボックスに外接するようにコライダーを生成します。

- Mesh Type
	Raw ... ポリゴンリダクションを行いません。(生のメッシュデータでコライダーを生成します。)
	Convex Hull ... Hull リダクションを行います。
	Convex Boxes ... ボックス分割リダクションを行います。
	Box ... AABBバウンディングボックスでコライダーを生成します。

- Max Triangles(Convex Boxes, Convex Hull)
ポリゴン削減レベルを指定します。(面単位, 最大255)

- Slice Mode(Convex Boxes)
ボックス分割の方向(X/Y/Z)を指定します。

- Scale
コライダーのスケール値を設定します。

- Min Thickness(Convex Boxes, Box, Capsule, Sphere)
コライダーの最小の厚みを指定します。

- Optimize Rotation
AABBバウンディングボックスの最小の方向を検索します。方向(X/Y/Z)ごとに有効/無効を指定できます。

- Collider To Child(Bone Collider)
	Auto ... 自動判定
	On ... 子オブジェクトにコライダーを生成します。
	Off ... 親オブジェクトにコライダーを生成します。

----------------------------------------------
 Collider オプション
----------------------------------------------

- Convex
詳しくは UnityEngine.Collider.convex をご参照ください。
http://docs-jp.unity3d.com/Documentation/ScriptReference/Collider-isTrigger.html

- Is Trigger
詳しくは UnityEngine.Collider.isTrigger をご参照ください。
http://docs-jp.unity3d.com/Documentation/ScriptReference/Collider-isTrigger.html

- Physics Material
詳しくは UnityEngine.Collider.material をご参照ください。
http://docs-jp.unity3d.com/Documentation/ScriptReference/Collider-material.html

- Create Asset(4Prefab)
メッシュコライダーのアセットをモデルの存在するフォルダに保存します。
(SABoneColliderBuilder & Shape Type が Mesh の場合のみ。 Prefab 化したインスタンスへの補正。)

----------------------------------------------
 Rigidbody オプション
----------------------------------------------

- Is Create
コライダーと一緒に UnityEngine.Rigidbody を生成します。

- Is Kinematic
詳しくは UnityEngine.Rigidbody.isKinematic をご参照ください。
http://docs-jp.unity3d.com/Documentation/ScriptReference/Rigidbody-isKinematic.html

- Mass
詳しくは UnityEngine.Rigidbody.mass をご参照ください。
http://docs-jp.unity3d.com/Documentation/ScriptReference/Rigidbody-mass.html

- Drag
詳しくは UnityEngine.Rigidbody.drag をご参照ください。
http://docs-jp.unity3d.com/Documentation/ScriptReference/Rigidbody-drag.html

- Angular Drag
詳しくは UnityEngine.Rigidbody.angularDrag をご参照ください。
http://docs-jp.unity3d.com/Documentation/ScriptReference/Rigidbody-angularDrag.html

- Use Gravity
詳しくは UnityEngine.Rigidbody.useGravity をご参照ください。
http://docs-jp.unity3d.com/Documentation/ScriptReference/Rigidbody-useGravity.html

- Interpolation
詳しくは UnityEngine.Rigidbody.interpolation をご参照ください。
http://docs-jp.unity3d.com/Documentation/ScriptReference/Rigidbody-interpolation.html

- Collision Detection Mode
詳しくは UnityEngine.Rigidbody.collisionDetectionMode をご参照ください。
http://docs-jp.unity3d.com/Documentation/ScriptReference/Rigidbody-collisionDetectionMode.html

----------------------------------------------
 その他
----------------------------------------------

- Modify Name
オブジェクトが手動で更新された場合、名前の末尾にマークを添付します。

- Cleanup Modified
"Cleanup"実行時、手動で変更した箇所も強制的にクリーンします。

- Is Debug
変換中のログを出力します。

- Recursivery(Bone Collider)
子オブジェクトに対しても同様のプロパティを反映させます。(手動で変更した箇所を除く)

----------------------------------------------
 変更履歴
----------------------------------------------

Version 1.0.3 2014/04/18
- Support Create Asset(4Prefab)
- Support Is Debug(Logging)
Version 1.0.2 2014/04/10
- Fix Null Reference in converting no boneWeight model (SABoneColliderBuilder).
Version 1.0.1 2014/02/13
- Update Tutorial.
Version 1.0.0 2014/02/02
- First release.

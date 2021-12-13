----------------------------------------------
 SAColliderBuilder
 Copyright (c) 2014 Stereoarts Nora
 Version 1.0.3
 http://stereoarts.jp
 stereoarts.nora@gmail.com
----------------------------------------------

Thank you for buying SACollider Builder.

If you have any questions, suggestions, comments or feature requests, please
send email(stereoarts.nora@gmail.com) or twitter(https://twitter.com/Stereoarts).

----------------------------------------------
 How to use
----------------------------------------------

SAMeshColliderBuilder

- Split material/primitive/polygon and generate collider from MeshFilter.
- Add component to any GameObject. That's children has MeshRenderer/SkinnedMeshRenderer.
- Press "Process" Button to create colliders.

SABoneColliderBuilder

- Split bone weight and generate collider from SkinnedMeshRenderer.
- Add component to any GameObject. That's children has SkinnedMeshRenderer.
- Press "Process" Button to create colliders.

----------------------------------------------
 Split Options(Mesh Collider)
----------------------------------------------

- Split Material
Split the mesh at the boundary of the material.

- Split Primitive
Split the mesh at the boundary of the primitive that is not contiguous.

- Split Polygon Normal
Split the mesh with a large part of the difference between the normal.

----------------------------------------------
Split Options(Bone Collider)
----------------------------------------------

- Bone Weight
Extract vertex greater than bone weight.

- Greater Bone Weight
Extract vertex the greater bone weight.

- Bone Triangle Extent
Extent triangle per extracted vertices.
	Disable ... No extent.
	Vertex 2 ... Extent triangle contains extracted line.
	Vertex 1 ... Extent triangle contains extracted point.

----------------------------------------------
 Reducer Options
----------------------------------------------

- Shape Type
	None ... No create collider.
	Mesh ... Create MeshCollider.
	Box ... Create BoxCollider.
	Capsule ... Create CapsuleCollider.
	Sphere ... Create SphereCollider.

- Fit Type(Sphere, Capsule)
	Innter ... Inner fitting for Box -> Sphere Collider.
	Outer ... Outer fitting for Box -> Capsule Collider.

- Mesh Type
	Raw ... No reduction.
	Convex Hull ... Polygon reduction use Hull.
	Convex Boxes ... Polygon reduction use Boxes slicing.
	Box ... Create AABB bouding box.

- Max Triangles(Convex Boxes, Convex Hull)
Specify the polygon reduction level.

- Slice Mode(Convex Boxes)
Slice arrow(X/Y/Z) for Convex boxes.

- Scale
Collision scale.

- Min Thickness(Convex Boxes, Box, Capsule, Sphere)
Collision min thickness.

- Optimize Rotation
Searching option for AABB bouding box.If not set dimention(X/Y/Z), lock target angle.

- Collider To Child(Bone Collider)
	Auto ... Automatically determines.
	On ... Create a child object collision.
	Off ... Create a parent object collision.

----------------------------------------------
 Collider Options
----------------------------------------------

- Convex
See UnityEngine.Collider.convex
http://docs.unity3d.com/Documentation/ScriptReference/Collider-isTrigger.html

- Is Trigger
See UnityEngine.Collider.isTrigger
http://docs.unity3d.com/Documentation/ScriptReference/Collider-isTrigger.html

- Physics Material
See UnityEngine.Collider.material
http://docs.unity3d.com/Documentation/ScriptReference/Collider-material.html

- Create Asset(4Prefab)
Create mesh collider assets in model folder.
(SABoneColliderBuilder & Shape Type as Mesh only. Fix for prefab instance.)

----------------------------------------------
 Rigidbody Options
----------------------------------------------

- Is Create
Create UnityEngine.Rigidbody.

- Is Kinematic
See UnityEngine.Rigidbody.isKinematic
http://docs.unity3d.com/Documentation/ScriptReference/Rigidbody-isKinematic.html

- Mass
See UnityEngine.Rigidbody.mass
http://docs.unity3d.com/Documentation/ScriptReference/Rigidbody-mass.html

- Drag
See UnityEngine.Rigidbody.drag
http://docs.unity3d.com/Documentation/ScriptReference/Rigidbody-drag.html

- Angular Drag
See UnityEngine.Rigidbody.angularDrag
http://docs.unity3d.com/Documentation/ScriptReference/Rigidbody-angularDrag.html

- Use Gravity
See UnityEngine.Rigidbody.useGravity
http://docs.unity3d.com/Documentation/ScriptReference/Rigidbody-useGravity.html

- Interpolation
See UnityEngine.Rigidbody.interpolation
http://docs.unity3d.com/Documentation/ScriptReference/Rigidbody-interpolation.html

- Collision Detection Mode
See UnityEngine.Rigidbody.collisionDetectionMode
http://docs.unity3d.com/Documentation/ScriptReference/Rigidbody-collisionDetectionMode.html

----------------------------------------------
 Other
----------------------------------------------

- Modify Name
If modified object manually, added postfix to gameObject name.

- Cleanup Modified
Cleanup modified children in "Cleanup".

- Is Debug
Logging for processing.

- Recursivery(Bone Collider)
Feedback properties for children.

----------------------------------------------
 History
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

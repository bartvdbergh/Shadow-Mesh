using UnityEngine;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

public class ShadowMesh : MonoBehaviour
{
	public Light source;
	public GameObject obj;
	public float normalScale;
	private void Start(){
		AccolateCollider(obj);
	}

	private void Update ()
	{
		List<Vector3> vertices;
		Vector3[] normals;
		List<int>triangles = new List<int> ();
		List<int>indices;


		ProjectVertcies(source, obj, out vertices, out normals);
			//-- Remove duplicates, and a 0,0,0 point --//
		vertices = TrimList (vertices);
			//-- Order vertices on y-axis so we can assign indeces in clockwise order --//
		vertices = vertices.OrderBy(v=>v.y).ToList();
		indices = SetIndices (vertices);
			//-- Sets the order of the verts so that vertices[indices[0]] = vertices[0], this means when you draw a line from vertix to vertix, you get an outline. Necessary for triangulation --//
		SortVertsToIndices (ref vertices, ref indices);
			//-- Copy and translate the existing vertices and indices to create depth --//
		vertices = ExtrudeVerts (vertices,Vector3.back);
		indices = ExtrudeIndices (indices);
			
			//-- Triangulate the shape --//
		triangles = ExtrudeTriangles (indices);
			//-- Feed the information back in to  the mesh --//
		CreateMesh(vertices, triangles);
		//DrawDebug (vertices,indices,triangles);
		DrawOutline (vertices, indices);

	}

	private void AccolateCollider (GameObject obj){
		GameObject colHolder = new GameObject ();
		colHolder.AddComponent<MeshCollider> ();
		colHolder.GetComponent<MeshCollider> ().sharedMesh = obj.GetComponent<MeshCollider> ().sharedMesh;
		colHolder.transform.parent = obj.transform;
		colHolder.transform.position = obj.transform.position;
		colHolder.transform.rotation = obj.transform.rotation;
		colHolder.transform.localScale = Vector3.one * .9999f;
		Destroy (obj.GetComponent<MeshCollider> ());
	}

	private void ProjectVertcies (Light source, GameObject obj, out List<Vector3> vertices, out Vector3[] normals)
	{
		vertices = obj.GetComponent<MeshFilter> ().mesh.vertices.ToList ();
		normals = new Vector3[vertices.Count];

		Vector3 sourcePos = source.transform.position;
		List<Vector3> projectedVerticies = new List<Vector3> ();

			//-- Project the outside vertices of the object to the wall --//
		for (var i = 0; i < vertices.Count; i++) {
			Ray direction = new Ray (sourcePos, obj.transform.TransformPoint (vertices [i]) - sourcePos);
			RaycastHit[] hits = Physics.RaycastAll (direction);

			if (hits.Length > 0) {
				if (hits [0].collider.gameObject.transform != obj.transform.GetChild(0).transform) {
					Transform transform = hits [0].transform;
					normals [i] = hits [0].normal;
					projectedVerticies.Add(hits [0].point);

					// DEBUG
					//Debug.DrawLine(source.transform.position, hits[0].point, Color.red);
					//Debug.DrawRay(hits[0].point, hits[0].normal, Color.yellow);
				}
			}
		}
		vertices = projectedVerticies;
	}

	private List<Vector3> TrimList (List<Vector3> list)
	{
		List<Vector3>  trim = new List<Vector3>();
		trim = list.Distinct ().ToList ();
		trim.Remove (Vector3.zero);
		return trim;
	}

	private void DrawOutline (List<Vector3> vertices, List<int>indices)
	{
		for (int i = 0; i < vertices.Count; i++) {
			Debug.DrawLine (vertices [i], vertices [(i + 1) % vertices.Count], Color.blue);
		}
	}

	private List<int>SetIndices (List<Vector3>  vertices)
	{
		List<int>indices = new List<int> ();
		indices.Add (0);
		int[] selectedIds = new int[vertices.Count];
		selectedIds [0] = 1;

			//-- Set indices on the right side of the top vertex from top to bottom --//
		for (int i = 1; i < vertices.Count - 1; ++i) {
			if (isLeft(vertices[vertices.Count - 1],vertices[0],vertices[i])){
				indices.Add (i);
				selectedIds [i] = 1;
				Debug.DrawRay (vertices [i], Vector3.back, new Color(100 + 75 * i,0,0));
			}
		}

			//-- Set indices on the left side of the bottom vertex from bottom to top--//
		for (int i = vertices.Count - 1; i > 0; i--) {
			if (selectedIds [i] == 0) {
				indices.Add (i);
				//Debug.DrawRay (vertices [i], Vector3.back, Color.green);
			}
		}
		indices.Reverse ();
		return indices;
	}

	private bool isLeft(Vector3 a, Vector3 b, Vector3 c){
		return ((b.x - a.x)*(c.y - a.y) - (b.y - a.y)*(c.x - a.x)) >= 0;
	}

	private void DrawDebug(List<Vector3> vertices, List<int>indices, List<int>triangles){

			//-- Draw the top vertix in white, the bottom in black --//
		Debug.DrawRay (vertices [0], Vector3.back, Color.white);
		Debug.DrawRay (vertices [vertices.Count - 1], Vector3.back, Color.black);
		Debug.DrawRay (vertices [0], Vector3.left, Color.yellow);
		Debug.DrawRay (vertices [0], Vector3.right, Color.yellow);

			//-- Draw wireframe --//
		/*for (int i = 0; i < vertices.Count; ++i) {
			Debug.DrawLine (vertices[triangles[i*3]],vertices[triangles[(i*3)+1]],Color.green);
			Debug.DrawLine (vertices[triangles[(i*3)+1]],vertices[triangles[(i*3)+2]],Color.green);
			Debug.DrawLine (vertices[triangles[(i*3)+2]],vertices[triangles[(i*3)]],Color.green);
		}*/
	}

	private void CreateMesh(List<Vector3> vertices,List<int>triangles){
		Mesh mesh = GetComponent <MeshFilter>().mesh;
		mesh.Clear ();
		mesh.vertices = vertices.ToArray ();
		mesh.triangles = triangles.ToArray ();

		

		mesh.RecalculateNormals ();
		GetComponent <MeshCollider> ().sharedMesh.Optimize();
		mesh.RecalculateBounds ();
		GetComponent <MeshCollider> ().sharedMesh = mesh;
	}

	private List<Vector3>  ExtrudeVerts(List<Vector3>  vertices, Vector3 offset)
	{
		int count = vertices.Count;
		for (int i = 0; i < count; ++i) {
			vertices.Add(vertices[i] + offset);
		}
		return vertices;
	}

	private List<int> ExtrudeIndices(List<int> indices){
		int count = indices.Count;
		for (int i = 0; i < count; i++) {
			indices.Add(indices[i] + count);
		}

		return indices;
	}

	private List<int> ExtrudeTriangles(List<int> indices){
		int count = indices.Count / 2;
		List <int> tris = new List<int> ();

		//triangulate the 2 parrallel planes
		for (int k = 0; k < 2; k++) {
			for (int i = 1; i < count-1; i++) {
				tris.Add(indices [0 + (count * k)]);
				tris.Add(indices [i + (count* k)]);
				tris.Add(indices [i + 1 + (count * k)]);
			}
		}

		//Triangulate te border
		for (int i = 0; i < count-1; i++) {
			tris.Add (indices [count+i+1]);
			tris.Add (indices [count + i]);
			tris.Add (indices [i]);


			tris.Add (indices [i+1]);
			tris.Add (indices [count+i+1]);
			tris.Add (indices [i]);
		}

		int j = count-1;
		tris.Add (count);
		tris.Add ((2*count) -1);
		tris.Add (j);

		tris.Add (0);
		tris.Add (count);
		tris.Add (j);

		return tris;
	}

	private void SortVertsToIndices(ref List<Vector3> vertices, ref List<int>indices){
		List<Vector3> verts = new List<Vector3>();
		for (int i = 0; i < indices.Count; i++) {
			verts.Add(vertices[indices[i]]);
		}

		vertices = verts;
		indices = indices.OrderBy(v=>v).ToList();
	}

}

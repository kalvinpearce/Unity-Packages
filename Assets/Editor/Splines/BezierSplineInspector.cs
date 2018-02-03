using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BezierSpline))]
public class BezierSplineInspector : Editor 
{
	/* Variables */
	private const int stepsPerCurve = 10;					// Visual resolution of curve
	private const float directionScale = 0.5f;				// Scalar size for direction vectors

	private const float handleSize = 0.04f;					// Scalar size for handle buttons
	private const float pickSize = 0.06f;					// Scalar size for pick
	private int selectedIndex = -1;							// Currently selected point in spline

	private BezierSpline spline;							// Reference to spline
	private Transform handleTransform;						// Temp handle transform
	private Quaternion handleRotation;						// Temp handle rotation

	private static Color[] modeColors = {
		Color.white,
		Color.yellow,
		Color.cyan
	};														// Colors to map different joining modes

	// Custom inspector
	public override void OnInspectorGUI()
	{
		// Grab spline reference
		spline = target as BezierSpline;

		// Velocity toggle button
		EditorGUI.BeginChangeCheck();
		bool showVel = EditorGUILayout.Toggle("Show Velocity", spline.showVelocity);
		if (EditorGUI.EndChangeCheck()) {
			Undo.RecordObject(spline, "Toggle Velocity");
			EditorUtility.SetDirty(spline);
			spline.showVelocity = showVel;
		}
		
		// If point selected, draw its info in inspector
		if( selectedIndex >= 0 && selectedIndex < spline.ControlPointCount )
		{
			DrawSelectedPointInspector();
		}

		// Add control point node button
		if( GUILayout.Button( "Add Node" ) )
		{
			Undo.RecordObject( spline, "Add Node" );
			spline.AddCurve();
			EditorUtility.SetDirty(spline);
		}
	}

	// Draw selected point info in inspector
	private void DrawSelectedPointInspector()
	{
		GUILayout.Label( "Selected Point" );

		// Position drawing/change check
		EditorGUI.BeginChangeCheck();
		Vector3 point = EditorGUILayout.Vector3Field( "Position", spline.GetControlPoint(selectedIndex) );
		if( EditorGUI.EndChangeCheck() )
		{
			Undo.RecordObject( spline, "Move Point" );
			EditorUtility.SetDirty(spline);
			spline.SetControlPoint( selectedIndex, point );
		}
		// Mode drawing/change check
		EditorGUI.BeginChangeCheck();
		BezierSpline.ControlPointMode mode = (BezierSpline.ControlPointMode)EditorGUILayout.EnumPopup( "Mode", spline.GetControlPointMode(selectedIndex) );
		if( EditorGUI.EndChangeCheck() )
		{
			Undo.RecordObject( spline, "Change Point Mode" );
			spline.SetControlPointMode( selectedIndex, mode );
			EditorUtility.SetDirty(spline);
		}
	}

	// Draw spline in scene view
	private void OnSceneGUI()
	{
		// Grab spline references
		spline = target as BezierSpline;
		handleTransform = spline.transform;
		handleRotation = Tools.pivotRotation == PivotRotation.Local ? handleTransform.rotation : Quaternion.identity;
		
		// Show start of spline
		Vector3 p0 = ShowPoint(0);
		// Draw whole spline
		for( int i = 1; i < spline.ControlPointCount; i += 3 )
		{
			Vector3 p1 = ShowPoint( i     );
			Vector3 p2 = ShowPoint( i + 1 );
			Vector3 p3 = ShowPoint( i + 2 );
			
			Handles.color = Color.gray;
			Handles.DrawLine( p0, p1 );
			Handles.DrawLine( p2, p3 );
			
			Handles.DrawBezier( p0, p3, p1, p2, Color.white, null, 2f );
			p0 = p3;
		}

		// Show directions
		if( spline.showVelocity )
			ShowDirections();
	}

	// Show the velocity/direction vectors
	private void ShowDirections()
	{
		Handles.color = Color.green;
		Vector3 point = spline.GetPoint(0f);
		Handles.DrawLine( point, point + spline.GetDirection(0f) * directionScale );
		int steps = stepsPerCurve * spline.CurveCount;
		for( int i = 1; i <= steps; i++ )
		{
			point = spline.GetPoint( i / (float)steps );
			Handles.DrawLine( point, point + spline.GetDirection( i / (float)steps ) * directionScale );
		}
	}

	// Show point handles
	private Vector3 ShowPoint( int index )
	{
		// Grab current point references
		Vector3 point = handleTransform.TransformPoint( spline.GetControlPoint(index) );
		float size = HandleUtility.GetHandleSize(point);
		Handles.color = modeColors[(int)spline.GetControlPointMode(index)];

		// Draw if: control point || is selected || is a control point modifier and its control point is selected
		if( index % 3 == 0 || selectedIndex == index ||
			( selectedIndex % 3 == 0 && ( index == selectedIndex - 1 || index == selectedIndex + 1 ) ) ||
			( selectedIndex % 3 == 2 && ( index == selectedIndex + 1 || index == selectedIndex + 2 ) ) ||
			( selectedIndex % 3 == 1 && ( index == selectedIndex - 1 || index == selectedIndex - 2 ) ) )
		{
			// If selected
			if( Handles.Button( point, handleRotation, size * handleSize, size * pickSize, Handles.DotHandleCap ) )
			{
				// Set selected point to this
				selectedIndex = index;
				Repaint();
			}
		}

		// Draw position movement handle on selected point
		if (selectedIndex == index)
		{
			EditorGUI.BeginChangeCheck();
			point = Handles.DoPositionHandle( point, handleRotation );
			if( EditorGUI.EndChangeCheck() )
			{
				Undo.RecordObject( spline, "Move Point" );
				EditorUtility.SetDirty(spline);
				spline.SetControlPoint(index, handleTransform.InverseTransformPoint(point) );
			}
		}
		return point;
	}
}

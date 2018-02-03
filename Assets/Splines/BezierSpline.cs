using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public static class Bezier
{
	/// <summary>
		/// Gets the worldspace position of a point on a cubic bezier curve
		/// </summary>
		/// <param name="p0">Start point of curve</param>
		/// <param name="p1">Curve modifier for p0</param>
		/// <param name="p2">Curve modifier for p3</param>
		/// <param name="p3">End pointof curve</param>
		/// <param name="t">Point in curve to return. Ranges from 0 -> 1</param>
		/// <returns>Position along the curve: p0->p1->p2->3 at the point in time: t</returns>
	public static Vector3 GetPoint( Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t )
	{
		// Cubic Quadratic formula
		// B(t) = (1 - t)3 P0 + 3 (1 - t)2 t P1 + 3 (1 - t) t2 P2 + t3 P3
		t = Mathf.Clamp01(t);
		return (1f - t) * (1f - t) * (1f - t) * p0 + 3f * (1f - t) * (1f - t) * t * p1 + 3f * (1f - t) * t * t * p2 + t * t * t * p3;
	}

	/// <summary>
		/// Gets the wolrdspace position of the first derivative on a cubic bezier curve 
		/// </summary>
		/// <param name="p0">Start point of curve</param>
		/// <param name="p1">Curve modifier for p0</param>
		/// <param name="p2">Curve modifier for p3</param>
		/// <param name="p3">End pointof curve</param>
		/// <param name="t">Point in curve to return. Ranges from 0 -> 1</param>
		/// <returns>Position along the curve: p0->p1->p2->3 at the point in time: t</returns>
	public static Vector3 GetFirstDerivative( Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t )
	{
		// B'(t) = 3 (1 - t)2 (P1 - P0) + 6 (1 - t) t (P2 - P1) + 3 t2 (P3 - P2)
		t = Mathf.Clamp01(t);
		return 3f * (1f - t) * (1f - t) * (p1 - p0) + 6f * (1f - t) * t * (p2 - p1) + 3f * t * t * (p3 - p2);
	}
}

public class BezierSpline : MonoBehaviour 
{
	public enum ControlPointMode
	{
		Free,
		Aligned,
		Mirrored
	}

	/* Variables */
	[SerializeField]
	private Vector3[] points;							// Array of points in the curve
	[SerializeField]
	private ControlPointMode[] modes;					// Array of modes to match points in curve
	public int CurveCount
	{
		get
		{
			return ( points.Length - 1 ) / 3;
		}
	}													// Number of curves in full curve
	public int ControlPointCount
	{
		get
		{
			return points.Length;
		}
	}													// Number of points in curve
	public bool showVelocity;

	/* Functions  */

	/// <summary>
		/// Gets the control point position from the array at index
		/// </summary>
		/// <param name="index">Index of point in array</param>
		/// <returns>Wolrdspace position of point[indesx]</returns>
	public Vector3 GetControlPoint( int index )
	{
		return points[index];
	}

	/// <summary>
		/// Set the position of a point in the points array at index
		/// </summary>
		/// <param name="index">Index of point in array</param>
		/// <param name="point">New position for point</param>
	public void SetControlPoint( int index, Vector3 point )
	{
		// If point is control node
		if( index % 3 == 0 )
		{
			// move children nodes with it
			Vector3 delta = point - points[index];
			if( index > 0 )
			{
				points[index - 1] += delta;
			}
			if( index + 1 < points.Length )
			{
				points[index + 1] += delta;
			}
		}
		// Set position
		points[index] = point;
		// Set positions of neighbors
		EnforceMode(index);
	}

	/// <summary>
		/// Get mode of point at index
		/// </summary>
		/// <param name="index">Index of array</param>
		/// <returns>Control point mode of point[index]</returns>
	public ControlPointMode GetControlPointMode( int index )
	{
		return modes[(index + 1) / 3];
	}

	/// <summary>
		/// Set the mode of point at index in array
		/// </summary>
		/// <param name="index">Index of point to set</param>
		/// <param name="mode">Mode type to set</param>
	public void SetControlPointMode( int index, ControlPointMode mode )
	{
		modes[(index + 1) / 3] = mode;
		// Set positions of neighbors
		EnforceMode(index);
	}

	// Sets positions of neighboring nodes
	private void EnforceMode( int index )
	{
		int modeIndex = ( index + 1 ) / 3;
		ControlPointMode mode = modes[modeIndex];
		// If node doesnt need effecting, return
		if( mode == ControlPointMode.Free || modeIndex == 0 || modeIndex == modes.Length - 1 )
			return;

		// Calc nodes to fix/enforce
		int middleIndex = modeIndex * 3;
		int fixedIndex, enforcedIndex;
		if( index <= middleIndex )
		{
			fixedIndex = middleIndex - 1;
			enforcedIndex = middleIndex + 1;
		}
		else
		{
			fixedIndex = middleIndex + 1;
			enforcedIndex = middleIndex - 1;
		}

		// If mirrored, set tangent to oposite
		Vector3 middle = points[middleIndex];
		Vector3 enforcedTangent = middle - points[fixedIndex];
		// If aligned, adjust tangent by distancea
		if( mode == ControlPointMode.Aligned )
		{
			enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, points[enforcedIndex]);
		}
		// Adjust position by tangent
		points[enforcedIndex] = middle + enforcedTangent;
	}

	/// <summary>
		/// Get worldspace position on curve at time: t
		/// </summary>
		/// <param name="t">Position on curve to read. Ranges from 0 -> 1</param>
		/// <returns>Worldspace position of point on curve at t</returns>
	public Vector3 GetPoint( float t )
	{
		// Clamp to end of curve
		int i;
		if( t >= 1f )
		{
			t = 1f;
			i = points.Length - 4;
		}
		// Adjust values to which is current curve
		else
		{
			t = Mathf.Clamp01(t) * CurveCount;
			i = (int)t;
			t -= i;
			i *= 3;
		}
		return transform.TransformPoint( Bezier.GetPoint( points[i], points[i + 1], points[i + 2], points[i + 3], t ) );
	}
	
	/// <summary>
		/// 
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
	public Vector3 GetVelocity( float t )
	{
		int i;
		if( t >= 1f )
		{
			t = 1f;
			i = points.Length - 4;
		}
		else
		{
			t = Mathf.Clamp01(t) * CurveCount;
			i = (int)t;
			t -= i;
			i *= 3;
		}
		return transform.TransformPoint( Bezier.GetFirstDerivative( points[i], points[i + 1], points[i + 2], points[i + 3], t ) ) - transform.position;
	}
	
	/// <summary>
		/// Get the normalized direction of the curve at t
		/// </summary>
		/// <param name="t">Time in curve to sample</param>
		/// <returns>Normalized direction of point in curve at t</returns>
	public Vector3 GetDirection( float t )
	{
		return GetVelocity(t).normalized;
	}
	
	/// <summary>
		/// Resets the spline to default 2 points
		/// </summary>
	public void Reset()
	{
		points = new Vector3[] {
			new Vector3( 1f, 0f, 0f ),
			new Vector3( 2f, 0f, 0f ),
			new Vector3( 3f, 0f, 0f ),
			new Vector3( 4f, 0f, 0f )
		};
		modes = new ControlPointMode[] {
			ControlPointMode.Free,
			ControlPointMode.Free
		};
	}

	/// <summary>
		/// Adds a control point to the spline
		/// </summary>
	public void AddCurve()
	{
		Vector3 point = points[points.Length - 1];
		Array.Resize( ref points, points.Length + 3 );
		point.x += 1f;
		points[points.Length - 3] = point;
		point.x += 1f;
		points[points.Length - 2] = point;
		point.x += 1f;
		points[points.Length - 1] = point;

		Array.Resize(ref modes, modes.Length + 1);
		modes[modes.Length - 1] = modes[modes.Length - 2];
		EnforceMode( points.Length - 4 );
	}
}

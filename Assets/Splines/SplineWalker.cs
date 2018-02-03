using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplineWalker : MonoBehaviour 
{
	/* Variables */
	public BezierSpline spline;

	public float duration;

	private float progress;
	public AnimationCurve curve;

	private void Update ()
	{
		progress += Time.deltaTime / duration;
		if( progress > 1f )
		{
			progress = 0f;
		}
		transform.localPosition = spline.GetPoint(progress);
	}
}

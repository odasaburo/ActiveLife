using UnityEngine;
using System.Collections;

public class ScrollItemScale : MonoBehaviour {

	/// <summary>
	/// The parent scrollview that this scroll item belongs to.
	/// </summary>
	private UIScrollView _parentScrollView;
	public UIScrollView ParentScrollView
	{
		get { return _parentScrollView; }
		set { _parentScrollView = value; }
	}

	void Update () {
		Vector2 offset = _parentScrollView.panel.clipOffset;
		Vector4 range = _parentScrollView.panel.drawCallClipRange;
		Vector2 localpos = (Vector2) this.gameObject.transform.localPosition;

		float diff;
		float scale = 1;
		switch (_parentScrollView.movement) {
		case UIScrollView.Movement.Vertical:
			diff = Mathf.Abs (offset.y - localpos.y);
			scale = Mathf.Clamp (1 - (diff / range.w), 0, 1);
			break;
		case UIScrollView.Movement.Horizontal:
			diff = Mathf.Abs (offset.y - localpos.y);
			scale = Mathf.Clamp (1 - (diff / range.z), 0, 1);
			break;
		}

		scale = Mathf.Sqrt (scale);
		transform.localScale = new Vector3 (scale, scale, 1);
	}
}

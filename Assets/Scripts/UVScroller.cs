using UnityEngine;

namespace Assets.Scripts
{
	public class UVScroller : MonoBehaviour
	{
		readonly Vector2 _uvSpeed = new Vector2(0.0f, 0.01f);
		Vector2 _uvOffset = Vector2.zero;

		void LateUpdate()
		{
			_uvOffset += _uvSpeed * Time.deltaTime;

			// ensure we don't scroll the texture too far 
			if (_uvOffset.x > 0.0625f) _uvOffset = new Vector2(0, _uvOffset.y);
			if (_uvOffset.y > 0.0625f) _uvOffset = new Vector2(_uvOffset.x, 0);

			GetComponent<Renderer>().materials[0].
				SetTextureOffset("_MainTex", _uvOffset);
		}
	}
}
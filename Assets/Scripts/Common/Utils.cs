using System.Runtime.CompilerServices;

namespace Voxels.Common
{
	public static class Utils
	{
		/// <summary>
		/// Converts coordinates to index in 2D space.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexFlattenizer2D(int x, int y, int lengthX) => y * lengthX + x;

		/// <summary>
		/// Extracts coordinates from the index in 2D space.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void IndexDeflattenizer2D(int index, int lengthX, out int x, out int y)
		{
			y = index / lengthX;
			x = index - y * lengthX;
		}

		/// <summary>
		/// Converts coordinates to index in 3D space.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexFlattenizer3D(int x, int y, int z, int lengthX, int lengthY) => z * lengthY * lengthX + y * lengthX + x;

		/// <summary>
		/// Extracts coordinates from the index in 3D space.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void IndexDeflattenizer3D(int index, int lengthX, int lengthY, out int x, out int y, out int z)
		{
			z = index / (lengthX * lengthY); // 10 / (3*2) = 1
			var rest = index - z * lengthX * lengthY; // 10 - 1 * 3 * 2 = 4
			y = rest / lengthX; // 4 / 3 = 1
			x = rest - y * lengthX; // 4 - 1 * 2 = 1
		}
	}
}

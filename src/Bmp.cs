using System.Runtime.InteropServices;

namespace StbTrueTypeSharp
{
	partial class StbTrueType
	{
		public static void stbtt__rasterize(stbtt__bitmap result, stbtt__point[] pts, int[] wcount, int windings,
			float scale_x, float scale_y, float shift_x, float shift_y, int off_x, int off_y, int invert)
		{
			var y_scale_inv = invert != 0 ? -scale_y : scale_y;
			var n = 0;
			var i = 0;
			var j = 0;
			var k = 0;
			var m = 0;
			var vsubsample = 1;
			n = 0;
			for (i = 0; i < windings; ++i) n += wcount[i];
			var e = new stbtt__edge[n + 1];
			for (i = 0; i < e.Length; ++i) e[i] = new stbtt__edge();
			n = 0;
			m = 0;
			for (i = 0; i < windings; ++i)
			{
				var p = new FakePtr<stbtt__point>(pts, m);
				m += wcount[i];
				j = wcount[i] - 1;
				for (k = 0; k < wcount[i]; j = k++)
				{
					var a = k;
					var b = j;
					if (p[j].y == p[k].y)
						continue;
					e[n].invert = 0;
					if (invert != 0 && p[j].y > p[k].y || invert == 0 && p[j].y < p[k].y)
					{
						e[n].invert = 1;
						a = j;
						b = k;
					}

					e[n].x0 = p[a].x * scale_x + shift_x;
					e[n].y0 = (p[a].y * y_scale_inv + shift_y) * vsubsample;
					e[n].x1 = p[b].x * scale_x + shift_x;
					e[n].y1 = (p[b].y * y_scale_inv + shift_y) * vsubsample;
					++n;
				}
			}

			var ptr = new FakePtr<stbtt__edge>(e);
			stbtt__sort_edges(ptr, n);
			stbtt__rasterize_sorted_edges(result, ptr, n, vsubsample, off_x, off_y);
		}

		public static void stbtt_Rasterize(stbtt__bitmap result, float flatness_in_pixels, stbtt_vertex[] vertices,
			int num_verts, float scale_x, float scale_y, float shift_x, float shift_y, int x_off, int y_off, int invert)
		{
			var scale = scale_x > scale_y ? scale_y : scale_x;
			var winding_count = 0;
			int[] winding_lengths = null;
			var windings = stbtt_FlattenCurves(vertices, num_verts, flatness_in_pixels / scale, out winding_lengths,
				out winding_count);
			if (windings != null)
				stbtt__rasterize(result, windings, winding_lengths, winding_count, scale_x, scale_y, shift_x, shift_y,
					x_off, y_off, invert);
		}

		[StructLayout(LayoutKind.Sequential)]
		public class stbtt__bitmap
		{
			public int h;
			public FakePtr<byte> pixels;
			public int stride;
			public int w;
		}
	}
}
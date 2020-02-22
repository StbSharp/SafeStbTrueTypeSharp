namespace StbTrueTypeSharp
{
	partial class StbTrueType
	{
		public static void stbtt__rasterize(stbtt__bitmap result, stbtt__point[] pts, int[] wcount, int windings, float scale_x, float scale_y, float shift_x, float shift_y, int off_x, int off_y, int invert)
		{
			float y_scale_inv = (float)((invert) != 0 ? -scale_y : scale_y);
			int n = 0;
			int i = 0;
			int j = 0;
			int k = 0;
			int m = 0;
			int vsubsample = (int)(1);
			n = (int)(0);
			for (i = (int)(0); (i) < (windings); ++i)
			{
				n += (int)(wcount[i]);
			}
			var e = new stbtt__edge[n + 1];
			for (i = 0; i < e.Length; ++i)
			{
				e[i] = new stbtt__edge();
			}
			n = (int)(0);
			m = (int)(0);
			for (i = (int)(0); (i) < (windings); ++i)
			{
				FakePtr<stbtt__point> p = new FakePtr<stbtt__point>(pts, m);
				m += (int)(wcount[i]);
				j = (int)(wcount[i] - 1);
				for (k = (int)(0); (k) < (wcount[i]); j = (int)(k++))
				{
					int a = (int)(k);
					int b = (int)(j);
					if ((p[j].y) == (p[k].y))
						continue;
					e[n].invert = (int)(0);
					if ((((invert) != 0) && ((p[j].y) > (p[k].y))) || ((invert == 0) && ((p[j].y) < (p[k].y))))
					{
						e[n].invert = (int)(1);
						a = (int)(j);
						b = (int)(k);
					}
					e[n].x0 = (float)(p[a].x * scale_x + shift_x);
					e[n].y0 = (float)((p[a].y * y_scale_inv + shift_y) * vsubsample);
					e[n].x1 = (float)(p[b].x * scale_x + shift_x);
					e[n].y1 = (float)((p[b].y * y_scale_inv + shift_y) * vsubsample);
					++n;
				}
			}
			var ptr = new FakePtr<stbtt__edge>(e);
			stbtt__sort_edges(ptr, (int)(n));
			stbtt__rasterize_sorted_edges(result, ptr, (int)(n), (int)(vsubsample), (int)(off_x), (int)(off_y));
		}

		public static void stbtt_Rasterize(stbtt__bitmap result, float flatness_in_pixels, stbtt_vertex[] vertices, int num_verts, float scale_x, float scale_y, float shift_x, float shift_y, int x_off, int y_off, int invert)
		{
			float scale = (float)((scale_x) > (scale_y) ? scale_y : scale_x);
			int winding_count = (int)(0);
			int[] winding_lengths = (null);
			stbtt__point[] windings = stbtt_FlattenCurves(vertices, (int)(num_verts), (float)(flatness_in_pixels / scale), out winding_lengths, out winding_count);
			if ((windings) != null)
			{
				stbtt__rasterize(result, windings, winding_lengths, (int)(winding_count), (float)(scale_x), (float)(scale_y), (float)(shift_x), (float)(shift_y), (int)(x_off), (int)(y_off), (int)(invert));
			}

		}
	}
}

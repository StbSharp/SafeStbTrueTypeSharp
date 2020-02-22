namespace StbTrueTypeSharp
{
	partial class StbTrueType
	{
		public static void stbtt__track_vertex(stbtt__csctx c, int x, int y)
		{
			if (((x) > (c.max_x)) || (c.started == 0))
				c.max_x = (int)(x);
			if (((y) > (c.max_y)) || (c.started == 0))
				c.max_y = (int)(y);
			if (((x) < (c.min_x)) || (c.started == 0))
				c.min_x = (int)(x);
			if (((y) < (c.min_y)) || (c.started == 0))
				c.min_y = (int)(y);
			c.started = (int)(1);
		}

		public static void stbtt__csctx_v(stbtt__csctx c, byte type, int x, int y, int cx, int cy, int cx1, int cy1)
		{
			if ((c.bounds) != 0)
			{
				stbtt__track_vertex(c, (int)(x), (int)(y));
				if ((type) == (STBTT_vcubic))
				{
					stbtt__track_vertex(c, (int)(cx), (int)(cy));
					stbtt__track_vertex(c, (int)(cx1), (int)(cy1));
				}
			}
			else
			{
				var v = new stbtt_vertex();
				stbtt_setvertex(ref v, (byte)(type), (int)(x), (int)(y), (int)(cx), (int)(cy));
				c.pvertices[c.num_vertices] = v;
				c.pvertices[c.num_vertices].cx1 = ((short)(cx1));
				c.pvertices[c.num_vertices].cy1 = ((short)(cy1));
			}

			c.num_vertices++;
		}

		public static void stbtt__csctx_close_shape(stbtt__csctx ctx)
		{
			if ((ctx.first_x != ctx.x) || (ctx.first_y != ctx.y))
				stbtt__csctx_v(ctx, (byte)(STBTT_vline), (int)(ctx.first_x), (int)(ctx.first_y), (int)(0), (int)(0), (int)(0), (int)(0));
		}

		public static void stbtt__csctx_rmove_to(stbtt__csctx ctx, float dx, float dy)
		{
			stbtt__csctx_close_shape(ctx);
			ctx.first_x = (float)(ctx.x = (float)(ctx.x + dx));
			ctx.first_y = (float)(ctx.y = (float)(ctx.y + dy));
			stbtt__csctx_v(ctx, (byte)(STBTT_vmove), (int)(ctx.x), (int)(ctx.y), (int)(0), (int)(0), (int)(0), (int)(0));
		}

		public static void stbtt__csctx_rline_to(stbtt__csctx ctx, float dx, float dy)
		{
			ctx.x += (float)(dx);
			ctx.y += (float)(dy);
			stbtt__csctx_v(ctx, (byte)(STBTT_vline), (int)(ctx.x), (int)(ctx.y), (int)(0), (int)(0), (int)(0), (int)(0));
		}

		public static void stbtt__csctx_rccurve_to(stbtt__csctx ctx, float dx1, float dy1, float dx2, float dy2, float dx3, float dy3)
		{
			float cx1 = (float)(ctx.x + dx1);
			float cy1 = (float)(ctx.y + dy1);
			float cx2 = (float)(cx1 + dx2);
			float cy2 = (float)(cy1 + dy2);
			ctx.x = (float)(cx2 + dx3);
			ctx.y = (float)(cy2 + dy3);
			stbtt__csctx_v(ctx, (byte)(STBTT_vcubic), (int)(ctx.x), (int)(ctx.y), (int)(cx1), (int)(cy1), (int)(cx2), (int)(cy2));
		}
	}
}
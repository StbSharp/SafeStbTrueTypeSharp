using System;

namespace StbTrueTypeSharp
{
	partial class StbTrueType
	{
		public static int stbtt__get_svg(stbtt_fontinfo info)
		{
			uint t = 0;
			if (info.svg < 0)
			{
				t = stbtt__find_table(info.data, (uint) info.fontstart, "SVG ");
				if (t != 0)
				{
					var offset = ttULONG(info.data + t + 2);
					info.svg = (int) (t + offset);
				}
				else
				{
					info.svg = 0;
				}
			}

			return info.svg;
		}

		public static int stbtt_InitFont_internal(stbtt_fontinfo info, byte[] data, int fontstart)
		{
			uint cmap = 0;
			uint t = 0;
			var i = 0;
			var numTables = 0;
			var ptr = new FakePtr<byte>(data);
			info.data = ptr;
			info.fontstart = fontstart;
			info.cff = stbtt__new_buf(FakePtr<byte>.Null, 0);
			cmap = stbtt__find_table(ptr, (uint) fontstart, "cmap");
			info.loca = (int) stbtt__find_table(ptr, (uint) fontstart, "loca");
			info.head = (int) stbtt__find_table(ptr, (uint) fontstart, "head");
			info.glyf = (int) stbtt__find_table(ptr, (uint) fontstart, "glyf");
			info.hhea = (int) stbtt__find_table(ptr, (uint) fontstart, "hhea");
			info.hmtx = (int) stbtt__find_table(ptr, (uint) fontstart, "hmtx");
			info.kern = (int) stbtt__find_table(ptr, (uint) fontstart, "kern");
			info.gpos = (int) stbtt__find_table(ptr, (uint) fontstart, "GPOS");
			if (cmap == 0 || info.head == 0 || info.hhea == 0 || info.hmtx == 0)
				return 0;
			if (info.glyf != 0)
			{
				if (info.loca == 0)
					return 0;
			}
			else
			{
				var b = new stbtt__buf();
				var topdict = new stbtt__buf();
				var topdictidx = new stbtt__buf();
				var cstype = (uint) 2;
				var charstrings = (uint) 0;
				var fdarrayoff = (uint) 0;
				var fdselectoff = (uint) 0;
				uint cff = 0;
				cff = stbtt__find_table(ptr, (uint) fontstart, "CFF ");
				if (cff == 0)
					return 0;
				info.fontdicts = stbtt__new_buf(FakePtr<byte>.Null, 0);
				info.fdselect = stbtt__new_buf(FakePtr<byte>.Null, 0);
				info.cff = stbtt__new_buf(new FakePtr<byte>(ptr, (int) cff), 512 * 1024 * 1024);
				b = info.cff;
				stbtt__buf_skip(b, 2);
				stbtt__buf_seek(b, stbtt__buf_get8(b));
				stbtt__cff_get_index(b);
				topdictidx = stbtt__cff_get_index(b);
				topdict = stbtt__cff_index_get(topdictidx, 0);
				stbtt__cff_get_index(b);
				info.gsubrs = stbtt__cff_get_index(b);
				stbtt__dict_get_ints(topdict, 17, out charstrings);
				stbtt__dict_get_ints(topdict, 0x100 | 6, out cstype);
				stbtt__dict_get_ints(topdict, 0x100 | 36, out fdarrayoff);
				stbtt__dict_get_ints(topdict, 0x100 | 37, out fdselectoff);
				info.subrs = stbtt__get_subrs(b, topdict);

				if (cstype != 2)
					return 0;
				if (charstrings == 0)
					return 0;
				if (fdarrayoff != 0)
				{
					if (fdselectoff == 0)
						return 0;
					stbtt__buf_seek(b, (int) fdarrayoff);
					info.fontdicts = stbtt__cff_get_index(b);
					info.fdselect = stbtt__buf_range(b, (int) fdselectoff, (int) (b.size - fdselectoff));
				}

				stbtt__buf_seek(b, (int) charstrings);
				info.charstrings = stbtt__cff_get_index(b);
			}

			t = stbtt__find_table(ptr, (uint) fontstart, "maxp");
			if (t != 0)
				info.numGlyphs = ttUSHORT(ptr + t + 4);
			else
				info.numGlyphs = 0xffff;
			info.svg = -1;
			numTables = ttUSHORT(ptr + cmap + 2);
			info.index_map = 0;
			for (i = 0; i < numTables; ++i)
			{
				var encoding_record = (uint) (cmap + 4 + 8 * i);
				switch (ttUSHORT(ptr + encoding_record))
				{
					case STBTT_PLATFORM_ID_MICROSOFT:
						switch (ttUSHORT(ptr + encoding_record + 2))
						{
							case STBTT_MS_EID_UNICODE_BMP:
							case STBTT_MS_EID_UNICODE_FULL:
								info.index_map = (int) (cmap + ttULONG(ptr + encoding_record + 4));
								break;
						}

						break;
					case STBTT_PLATFORM_ID_UNICODE:
						info.index_map = (int) (cmap + ttULONG(ptr + encoding_record + 4));
						break;
				}
			}

			if (info.index_map == 0)
				return 0;
			info.indexToLocFormat = ttUSHORT(ptr + info.head + 50);
			return 1;
		}

		public static int stbtt_FindGlyphIndex(stbtt_fontinfo info, int unicode_codepoint)
		{
			var data = info.data;
			var index_map = (uint) info.index_map;
			var format = ttUSHORT(data + index_map + 0);
			if (format == 0)
			{
				var bytes = (int) ttUSHORT(data + index_map + 2);
				if (unicode_codepoint < bytes - 6)
					return data[index_map + 6 + unicode_codepoint];
				return 0;
			}

			if (format == 6)
			{
				var first = (uint) ttUSHORT(data + index_map + 6);
				var count = (uint) ttUSHORT(data + index_map + 8);
				if ((uint) unicode_codepoint >= first && (uint) unicode_codepoint < first + count)
					return ttUSHORT(data + index_map + 10 + (unicode_codepoint - first) * 2);
				return 0;
			}

			if (format == 2) return 0;

			if (format == 4)
			{
				var segcount = (ushort) (ttUSHORT(data + index_map + 6) >> 1);
				var searchRange = (ushort) (ttUSHORT(data + index_map + 8) >> 1);
				var entrySelector = ttUSHORT(data + index_map + 10);
				var rangeShift = (ushort) (ttUSHORT(data + index_map + 12) >> 1);
				var endCount = index_map + 14;
				var search = endCount;
				if (unicode_codepoint > 0xffff)
					return 0;
				if (unicode_codepoint >= ttUSHORT(data + search + rangeShift * 2))
					search += (uint) (rangeShift * 2);
				search -= 2;
				while (entrySelector != 0)
				{
					ushort end = 0;
					searchRange >>= 1;
					end = ttUSHORT(data + search + searchRange * 2);
					if (unicode_codepoint > end)
						search += (uint) (searchRange * 2);
					--entrySelector;
				}

				search += 2;
				{
					ushort offset = 0;
					ushort start = 0;
					var item = (ushort) ((search - endCount) >> 1);
					start = ttUSHORT(data + index_map + 14 + segcount * 2 + 2 + 2 * item);
					if (unicode_codepoint < start)
						return 0;
					offset = ttUSHORT(data + index_map + 14 + segcount * 6 + 2 + 2 * item);
					if (offset == 0)
						return (ushort) (unicode_codepoint +
						                 ttSHORT(data + index_map + 14 + segcount * 4 + 2 + 2 * item));
					return ttUSHORT(data + offset + (unicode_codepoint - start) * 2 + index_map + 14 + segcount * 6 +
					                2 + 2 * item);
				}
			}

			if (format == 12 || format == 13)
			{
				var ngroups = ttULONG(data + index_map + 12);
				var low = 0;
				var high = 0;
				low = 0;
				high = (int) ngroups;
				while (low < high)
				{
					var mid = low + ((high - low) >> 1);
					var start_char = ttULONG(data + index_map + 16 + mid * 12);
					var end_char = ttULONG(data + index_map + 16 + mid * 12 + 4);
					if ((uint) unicode_codepoint < start_char)
					{
						high = mid;
					}
					else if ((uint) unicode_codepoint > end_char)
					{
						low = mid + 1;
					}
					else
					{
						var start_glyph = ttULONG(data + index_map + 16 + mid * 12 + 8);
						if (format == 12)
							return (int) (start_glyph + unicode_codepoint - start_char);
						return (int) start_glyph;
					}
				}

				return 0;
			}

			return 0;
		}

		public static int stbtt_GetCodepointShape(stbtt_fontinfo info, int unicode_codepoint,
			out stbtt_vertex[] vertices)
		{
			return stbtt_GetGlyphShape(info, stbtt_FindGlyphIndex(info, unicode_codepoint), out vertices);
		}

		public static int stbtt__GetGlyfOffset(stbtt_fontinfo info, int glyph_index)
		{
			var g1 = 0;
			var g2 = 0;
			if (glyph_index >= info.numGlyphs)
				return -1;
			if (info.indexToLocFormat >= 2)
				return -1;
			if (info.indexToLocFormat == 0)
			{
				g1 = info.glyf + ttUSHORT(info.data + info.loca + glyph_index * 2) * 2;
				g2 = info.glyf + ttUSHORT(info.data + info.loca + glyph_index * 2 + 2) * 2;
			}
			else
			{
				g1 = (int) (info.glyf + ttULONG(info.data + info.loca + glyph_index * 4));
				g2 = (int) (info.glyf + ttULONG(info.data + info.loca + glyph_index * 4 + 4));
			}

			return g1 == g2 ? -1 : g1;
		}

		public static int stbtt_GetGlyphBox(stbtt_fontinfo info, int glyph_index, ref int x0, ref int y0, ref int x1,
			ref int y1)
		{
			if (info.cff.size != 0)
			{
				stbtt__GetGlyphInfoT2(info, glyph_index, ref x0, ref y0, ref x1, ref y1);
			}
			else
			{
				var g = stbtt__GetGlyfOffset(info, glyph_index);
				if (g < 0)
					return 0;
				x0 = ttSHORT(info.data + g + 2);
				y0 = ttSHORT(info.data + g + 4);
				x1 = ttSHORT(info.data + g + 6);
				y1 = ttSHORT(info.data + g + 8);
			}

			return 1;
		}

		public static int stbtt_GetCodepointBox(stbtt_fontinfo info, int codepoint, ref int x0, ref int y0, ref int x1,
			ref int y1)
		{
			return stbtt_GetGlyphBox(info, stbtt_FindGlyphIndex(info, codepoint), ref x0, ref y0, ref x1, ref y1);
		}

		public static int stbtt_IsGlyphEmpty(stbtt_fontinfo info, int glyph_index)
		{
			short numberOfContours = 0;
			var g = 0;

			int x0 = 0, y0 = 0, x1 = 0, y1 = 0;
			if (info.cff.size != 0)
				return stbtt__GetGlyphInfoT2(info, glyph_index, ref x0, ref y0, ref x1, ref y1) == 0 ? 1 : 0;
			g = stbtt__GetGlyfOffset(info, glyph_index);
			if (g < 0)
				return 1;
			numberOfContours = ttSHORT(info.data + g);
			return numberOfContours == 0 ? 1 : 0;
		}

		public static int stbtt__GetGlyphShapeTT(stbtt_fontinfo info, int glyph_index, out stbtt_vertex[] pvertices)
		{
			short numberOfContours = 0;
			FakePtr<byte> endPtsOfContours;
			var data = info.data;
			stbtt_vertex[] vertices = null;
			var num_vertices = 0;
			var g = stbtt__GetGlyfOffset(info, glyph_index);
			pvertices = null;
			if (g < 0)
				return 0;
			numberOfContours = ttSHORT(data + g);
			if (numberOfContours > 0)
			{
				var flags = (byte) 0;
				byte flagcount = 0;
				var ins = 0;
				var i = 0;
				var j = 0;
				var m = 0;
				var n = 0;
				var next_move = 0;
				var was_off = 0;
				var off = 0;
				var start_off = 0;
				var x = 0;
				var y = 0;
				var cx = 0;
				var cy = 0;
				var sx = 0;
				var sy = 0;
				var scx = 0;
				var scy = 0;
				FakePtr<byte> points;
				endPtsOfContours = data + g + 10;
				ins = ttUSHORT(data + g + 10 + numberOfContours * 2);
				points = data + g + 10 + numberOfContours * 2 + 2 + ins;
				n = 1 + ttUSHORT(endPtsOfContours + numberOfContours * 2 - 2);
				m = n + 2 * numberOfContours;
				vertices = new stbtt_vertex[m];
				next_move = 0;
				flagcount = 0;
				off = m - n;
				for (i = 0; i < n; ++i)
				{
					if (flagcount == 0)
					{
						flags = points.GetAndIncrease();
						if ((flags & 8) != 0)
							flagcount = points.GetAndIncrease();
					}
					else
					{
						--flagcount;
					}

					vertices[off + i].type = flags;
				}

				x = 0;
				for (i = 0; i < n; ++i)
				{
					flags = vertices[off + i].type;
					if ((flags & 2) != 0)
					{
						var dx = (short) points.GetAndIncrease();
						x += (flags & 16) != 0 ? dx : -dx;
					}
					else
					{
						if ((flags & 16) == 0)
						{
							x = x + (short) (points[0] * 256 + points[1]);
							points += 2;
						}
					}

					vertices[off + i].x = (short) x;
				}

				y = 0;
				for (i = 0; i < n; ++i)
				{
					flags = vertices[off + i].type;
					if ((flags & 4) != 0)
					{
						var dy = (short) points.GetAndIncrease();
						y += (flags & 32) != 0 ? dy : -dy;
					}
					else
					{
						if ((flags & 32) == 0)
						{
							y = y + (short) (points[0] * 256 + points[1]);
							points += 2;
						}
					}

					vertices[off + i].y = (short) y;
				}

				num_vertices = 0;
				sx = sy = cx = cy = scx = scy = 0;
				for (i = 0; i < n; ++i)
				{
					flags = vertices[off + i].type;
					x = vertices[off + i].x;
					y = vertices[off + i].y;
					if (next_move == i)
					{
						if (i != 0)
							num_vertices = stbtt__close_shape(vertices, num_vertices, was_off, start_off, sx, sy, scx,
								scy, cx, cy);
						start_off = (flags & 1) != 0 ? 0 : 1;
						if (start_off != 0)
						{
							scx = x;
							scy = y;
							if ((vertices[off + i + 1].type & 1) == 0)
							{
								sx = (x + vertices[off + i + 1].x) >> 1;
								sy = (y + vertices[off + i + 1].y) >> 1;
							}
							else
							{
								sx = vertices[off + i + 1].x;
								sy = vertices[off + i + 1].y;
								++i;
							}
						}
						else
						{
							sx = x;
							sy = y;
						}

						stbtt_setvertex(ref vertices[num_vertices++], STBTT_vmove, sx, sy, 0, 0);
						was_off = 0;
						next_move = 1 + ttUSHORT(endPtsOfContours + j * 2);
						++j;
					}
					else
					{
						if ((flags & 1) == 0)
						{
							if (was_off != 0)
								stbtt_setvertex(ref vertices[num_vertices++], STBTT_vcurve, (cx + x) >> 1,
									(cy + y) >> 1, cx, cy);
							cx = x;
							cy = y;
							was_off = 1;
						}
						else
						{
							if (was_off != 0)
								stbtt_setvertex(ref vertices[num_vertices++], STBTT_vcurve, x, y, cx, cy);
							else
								stbtt_setvertex(ref vertices[num_vertices++], STBTT_vline, x, y, 0, 0);
							was_off = 0;
						}
					}
				}

				num_vertices = stbtt__close_shape(vertices, num_vertices, was_off, start_off, sx, sy, scx, scy, cx, cy);
			}
			else if (numberOfContours < 0)
			{
				var more = 1;
				var comp = data + g + 10;
				num_vertices = 0;
				vertices = null;
				while (more != 0)
				{
					ushort flags = 0;
					ushort gidx = 0;
					var comp_num_verts = 0;
					var i = 0;
					stbtt_vertex[] comp_verts;
					stbtt_vertex[] tmp;
					var mtx = new float[6];
					mtx[0] = 1;
					mtx[1] = 0;
					mtx[2] = 0;
					mtx[3] = 1;
					mtx[4] = 0;
					mtx[5] = 0;
					float m = 0;
					float n = 0;
					flags = (ushort) ttSHORT(comp);
					comp += 2;
					gidx = (ushort) ttSHORT(comp);
					comp += 2;
					if ((flags & 2) != 0)
					{
						if ((flags & 1) != 0)
						{
							mtx[4] = ttSHORT(comp);
							comp += 2;
							mtx[5] = ttSHORT(comp);
							comp += 2;
						}
						else
						{
							mtx[4] = comp.Value;
							comp += 1;
							mtx[5] = comp.Value;
							comp += 1;
						}
					}

					if ((flags & (1 << 3)) != 0)
					{
						mtx[0] = mtx[3] = ttSHORT(comp) / 16384.0f;
						comp += 2;
						mtx[1] = mtx[2] = 0;
					}
					else if ((flags & (1 << 6)) != 0)
					{
						mtx[0] = ttSHORT(comp) / 16384.0f;
						comp += 2;
						mtx[1] = mtx[2] = 0;
						mtx[3] = ttSHORT(comp) / 16384.0f;
						comp += 2;
					}
					else if ((flags & (1 << 7)) != 0)
					{
						mtx[0] = ttSHORT(comp) / 16384.0f;
						comp += 2;
						mtx[1] = ttSHORT(comp) / 16384.0f;
						comp += 2;
						mtx[2] = ttSHORT(comp) / 16384.0f;
						comp += 2;
						mtx[3] = ttSHORT(comp) / 16384.0f;
						comp += 2;
					}

					m = (float) Math.Sqrt(mtx[0] * mtx[0] + mtx[1] * mtx[1]);
					n = (float) Math.Sqrt(mtx[2] * mtx[2] + mtx[3] * mtx[3]);
					comp_num_verts = stbtt_GetGlyphShape(info, gidx, out comp_verts);
					if (comp_num_verts > 0)
					{
						for (i = 0; i < comp_num_verts; ++i)
						{
							short x = 0;
							short y = 0;
							x = comp_verts[i].x;
							y = comp_verts[i].y;
							comp_verts[i].x = (short) (m * (mtx[0] * x + mtx[2] * y + mtx[4]));
							comp_verts[i].y = (short) (n * (mtx[1] * x + mtx[3] * y + mtx[5]));
							x = comp_verts[i].cx;
							y = comp_verts[i].cy;
							comp_verts[i].cx = (short) (m * (mtx[0] * x + mtx[2] * y + mtx[4]));
							comp_verts[i].cy = (short) (n * (mtx[1] * x + mtx[3] * y + mtx[5]));
						}

						tmp = new stbtt_vertex[num_vertices + comp_num_verts];
						if (num_vertices > 0)
							Array.Copy(vertices, tmp, num_vertices);

						Array.Copy(comp_verts, 0, tmp, num_vertices, comp_num_verts);
						vertices = tmp;
						num_vertices += comp_num_verts;
					}

					more = flags & (1 << 5);
				}
			}

			pvertices = vertices;
			return num_vertices;
		}

		public static stbtt__buf stbtt__cid_get_glyph_subrs(stbtt_fontinfo info, int glyph_index)
		{
			var fdselect = info.fdselect;
			var nranges = 0;
			var start = 0;
			var end = 0;
			var v = 0;
			var fmt = 0;
			var fdselector = -1;
			var i = 0;
			stbtt__buf_seek(fdselect, 0);
			fmt = stbtt__buf_get8(fdselect);
			if (fmt == 0)
			{
				stbtt__buf_skip(fdselect, glyph_index);
				fdselector = stbtt__buf_get8(fdselect);
			}
			else if (fmt == 3)
			{
				nranges = (int) stbtt__buf_get(fdselect, 2);
				start = (int) stbtt__buf_get(fdselect, 2);
				for (i = 0; i < nranges; i++)
				{
					v = stbtt__buf_get8(fdselect);
					end = (int) stbtt__buf_get(fdselect, 2);
					if (glyph_index >= start && glyph_index < end)
					{
						fdselector = v;
						break;
					}

					start = end;
				}
			}

			if (fdselector == -1)
				stbtt__new_buf(FakePtr<byte>.Null, 0);
			return stbtt__get_subrs(info.cff, stbtt__cff_index_get(info.fontdicts, fdselector));
		}

		public static int stbtt__run_charstring(stbtt_fontinfo info, int glyph_index, stbtt__csctx c)
		{
			var in_header = 1;
			var maskbits = 0;
			var subr_stack_height = 0;
			var sp = 0;
			var v = 0;
			var i = 0;
			var b0 = 0;
			var has_subrs = 0;
			var clear_stack = 0;
			var s = new float[48];
			var subr_stack = new stbtt__buf[10];
			for (i = 0; i < subr_stack.Length; ++i) subr_stack[i] = new stbtt__buf();

			var subrs = info.subrs;
			var b = new stbtt__buf();
			float f = 0;
			b = stbtt__cff_index_get(info.charstrings, glyph_index);
			while (b.cursor < b.size)
			{
				i = 0;
				clear_stack = 1;
				b0 = stbtt__buf_get8(b);
				switch (b0)
				{
					case 0x13:
					case 0x14:
						if (in_header != 0)
							maskbits += sp / 2;
						in_header = 0;
						stbtt__buf_skip(b, (maskbits + 7) / 8);
						break;
					case 0x01:
					case 0x03:
					case 0x12:
					case 0x17:
						maskbits += sp / 2;
						break;
					case 0x15:
						in_header = 0;
						if (sp < 2)
							return 0;
						stbtt__csctx_rmove_to(c, s[sp - 2], s[sp - 1]);
						break;
					case 0x04:
						in_header = 0;
						if (sp < 1)
							return 0;
						stbtt__csctx_rmove_to(c, 0, s[sp - 1]);
						break;
					case 0x16:
						in_header = 0;
						if (sp < 1)
							return 0;
						stbtt__csctx_rmove_to(c, s[sp - 1], 0);
						break;
					case 0x05:
						if (sp < 2)
							return 0;
						for (; i + 1 < sp; i += 2) stbtt__csctx_rline_to(c, s[i], s[i + 1]);
						break;
					case 0x07:
					case 0x06:
						if (sp < 1)
							return 0;
						var goto_vlineto = b0 == 0x07 ? 1 : 0;
						for (;;)
						{
							if (goto_vlineto == 0)
							{
								if (i >= sp)
									break;
								stbtt__csctx_rline_to(c, s[i], 0);
								i++;
							}

							goto_vlineto = 0;
							if (i >= sp)
								break;
							stbtt__csctx_rline_to(c, 0, s[i]);
							i++;
						}

						break;
					case 0x1F:
					case 0x1E:
						if (sp < 4)
							return 0;
						var goto_hvcurveto = b0 == 0x1F ? 1 : 0;
						for (;;)
						{
							if (goto_hvcurveto == 0)
							{
								if (i + 3 >= sp)
									break;
								stbtt__csctx_rccurve_to(c, 0, s[i], s[i + 1], s[i + 2], s[i + 3],
									sp - i == 5 ? s[i + 4] : 0.0f);
								i += 4;
							}

							goto_hvcurveto = 0;
							if (i + 3 >= sp)
								break;
							stbtt__csctx_rccurve_to(c, s[i], 0, s[i + 1], s[i + 2], sp - i == 5 ? s[i + 4] : 0.0f,
								s[i + 3]);
							i += 4;
						}

						break;
					case 0x08:
						if (sp < 6)
							return 0;
						for (; i + 5 < sp; i += 6)
							stbtt__csctx_rccurve_to(c, s[i], s[i + 1], s[i + 2], s[i + 3], s[i + 4], s[i + 5]);
						break;
					case 0x18:
						if (sp < 8)
							return 0;
						for (; i + 5 < sp - 2; i += 6)
							stbtt__csctx_rccurve_to(c, s[i], s[i + 1], s[i + 2], s[i + 3], s[i + 4], s[i + 5]);
						if (i + 1 >= sp)
							return 0;
						stbtt__csctx_rline_to(c, s[i], s[i + 1]);
						break;
					case 0x19:
						if (sp < 8)
							return 0;
						for (; i + 1 < sp - 6; i += 2) stbtt__csctx_rline_to(c, s[i], s[i + 1]);
						if (i + 5 >= sp)
							return 0;
						stbtt__csctx_rccurve_to(c, s[i], s[i + 1], s[i + 2], s[i + 3], s[i + 4], s[i + 5]);
						break;
					case 0x1A:
					case 0x1B:
						if (sp < 4)
							return 0;
						f = (float) 0.0;
						if ((sp & 1) != 0)
						{
							f = s[i];
							i++;
						}

						for (; i + 3 < sp; i += 4)
						{
							if (b0 == 0x1B)
								stbtt__csctx_rccurve_to(c, s[i], f, s[i + 1], s[i + 2], s[i + 3], (float) 0.0);
							else
								stbtt__csctx_rccurve_to(c, f, s[i], s[i + 1], s[i + 2], (float) 0.0, s[i + 3]);
							f = (float) 0.0;
						}

						break;
					case 0x0A:
					case 0x1D:
						if (b0 == 0x0A)
							if (has_subrs == 0)
							{
								if (info.fdselect.size != 0)
									subrs = stbtt__cid_get_glyph_subrs(info, glyph_index);
								has_subrs = 1;
							}

						if (sp < 1)
							return 0;
						v = (int) s[--sp];
						if (subr_stack_height >= 10)
							return 0;
						subr_stack[subr_stack_height++] = b;
						b = stbtt__get_subr(b0 == 0x0A ? subrs : info.gsubrs, v);
						if (b.size == 0)
							return 0;
						b.cursor = 0;
						clear_stack = 0;
						break;
					case 0x0B:
						if (subr_stack_height <= 0)
							return 0;
						b = subr_stack[--subr_stack_height];
						clear_stack = 0;
						break;
					case 0x0E:
						stbtt__csctx_close_shape(c);
						return 1;
					case 0x0C:
					{
						float dx1 = 0;
						float dx2 = 0;
						float dx3 = 0;
						float dx4 = 0;
						float dx5 = 0;
						float dx6 = 0;
						float dy1 = 0;
						float dy2 = 0;
						float dy3 = 0;
						float dy4 = 0;
						float dy5 = 0;
						float dy6 = 0;
						float dx = 0;
						float dy = 0;
						var b1 = (int) stbtt__buf_get8(b);
						switch (b1)
						{
							case 0x22:
								if (sp < 7)
									return 0;
								dx1 = s[0];
								dx2 = s[1];
								dy2 = s[2];
								dx3 = s[3];
								dx4 = s[4];
								dx5 = s[5];
								dx6 = s[6];
								stbtt__csctx_rccurve_to(c, dx1, 0, dx2, dy2, dx3, 0);
								stbtt__csctx_rccurve_to(c, dx4, 0, dx5, -dy2, dx6, 0);
								break;
							case 0x23:
								if (sp < 13)
									return 0;
								dx1 = s[0];
								dy1 = s[1];
								dx2 = s[2];
								dy2 = s[3];
								dx3 = s[4];
								dy3 = s[5];
								dx4 = s[6];
								dy4 = s[7];
								dx5 = s[8];
								dy5 = s[9];
								dx6 = s[10];
								dy6 = s[11];
								stbtt__csctx_rccurve_to(c, dx1, dy1, dx2, dy2, dx3, dy3);
								stbtt__csctx_rccurve_to(c, dx4, dy4, dx5, dy5, dx6, dy6);
								break;
							case 0x24:
								if (sp < 9)
									return 0;
								dx1 = s[0];
								dy1 = s[1];
								dx2 = s[2];
								dy2 = s[3];
								dx3 = s[4];
								dx4 = s[5];
								dx5 = s[6];
								dy5 = s[7];
								dx6 = s[8];
								stbtt__csctx_rccurve_to(c, dx1, dy1, dx2, dy2, dx3, 0);
								stbtt__csctx_rccurve_to(c, dx4, 0, dx5, dy5, dx6, -(dy1 + dy2 + dy5));
								break;
							case 0x25:
								if (sp < 11)
									return 0;
								dx1 = s[0];
								dy1 = s[1];
								dx2 = s[2];
								dy2 = s[3];
								dx3 = s[4];
								dy3 = s[5];
								dx4 = s[6];
								dy4 = s[7];
								dx5 = s[8];
								dy5 = s[9];
								dx6 = dy6 = s[10];
								dx = dx1 + dx2 + dx3 + dx4 + dx5;
								dy = dy1 + dy2 + dy3 + dy4 + dy5;
								if (Math.Abs((double) dx) > Math.Abs((double) dy))
									dy6 = -dy;
								else
									dx6 = -dx;
								stbtt__csctx_rccurve_to(c, dx1, dy1, dx2, dy2, dx3, dy3);
								stbtt__csctx_rccurve_to(c, dx4, dy4, dx5, dy5, dx6, dy6);
								break;
							default:
								return 0;
						}
					}
						break;
					default:
						if (b0 != 255 && b0 != 28 && (b0 < 32 || b0 > 254))
							return 0;
						if (b0 == 255)
						{
							f = (float) (int) stbtt__buf_get(b, 4) / 0x10000;
						}
						else
						{
							stbtt__buf_skip(b, -1);
							f = (short) stbtt__cff_int(b);
						}

						if (sp >= 48)
							return 0;
						s[sp++] = f;
						clear_stack = 0;
						break;
				}

				if (clear_stack != 0)
					sp = 0;
			}

			return 0;
		}

		public static int stbtt__GetGlyphShapeT2(stbtt_fontinfo info, int glyph_index, out stbtt_vertex[] pvertices)
		{
			var count_ctx = new stbtt__csctx();
			count_ctx.bounds = 1;
			var output_ctx = new stbtt__csctx();
			if (stbtt__run_charstring(info, glyph_index, count_ctx) != 0)
			{
				pvertices = new stbtt_vertex[count_ctx.num_vertices];
				output_ctx.pvertices = pvertices;
				if (stbtt__run_charstring(info, glyph_index, output_ctx) != 0) return output_ctx.num_vertices;
			}

			pvertices = null;
			return 0;
		}

		public static int stbtt__GetGlyphInfoT2(stbtt_fontinfo info, int glyph_index, ref int x0, ref int y0,
			ref int x1, ref int y1)
		{
			var c = new stbtt__csctx();
			c.bounds = 1;
			var r = stbtt__run_charstring(info, glyph_index, c);
			x0 = r != 0 ? c.min_x : 0;
			y0 = r != 0 ? c.min_y : 0;
			x1 = r != 0 ? c.max_x : 0;
			y1 = r != 0 ? c.max_y : 0;
			return r != 0 ? c.num_vertices : 0;
		}

		public static int stbtt_GetGlyphShape(stbtt_fontinfo info, int glyph_index, out stbtt_vertex[] pvertices)
		{
			if (info.cff.size == 0)
				return stbtt__GetGlyphShapeTT(info, glyph_index, out pvertices);
			return stbtt__GetGlyphShapeT2(info, glyph_index, out pvertices);
		}

		public static void stbtt_GetGlyphHMetrics(stbtt_fontinfo info, int glyph_index, ref int advanceWidth,
			ref int leftSideBearing)
		{
			var numOfLongHorMetrics = ttUSHORT(info.data + info.hhea + 34);
			if (glyph_index < numOfLongHorMetrics)
			{
				advanceWidth = ttSHORT(info.data + info.hmtx + 4 * glyph_index);
				leftSideBearing = ttSHORT(info.data + info.hmtx + 4 * glyph_index + 2);
			}
			else
			{
				advanceWidth = ttSHORT(info.data + info.hmtx + 4 * (numOfLongHorMetrics - 1));
				leftSideBearing = ttSHORT(info.data + info.hmtx + 4 * numOfLongHorMetrics +
				                          2 * (glyph_index - numOfLongHorMetrics));
			}
		}

		public static int stbtt_GetKerningTableLength(stbtt_fontinfo info)
		{
			var data = info.data + info.kern;
			if (info.kern == 0)
				return 0;
			if (ttUSHORT(data + 2) < 1)
				return 0;
			if (ttUSHORT(data + 8) != 1)
				return 0;
			return ttUSHORT(data + 10);
		}

		public static int stbtt_GetKerningTable(stbtt_fontinfo info, stbtt_kerningentry[] table, int table_length)
		{
			var data = info.data + info.kern;
			var k = 0;
			var length = 0;
			if (info.kern == 0)
				return 0;
			if (ttUSHORT(data + 2) < 1)
				return 0;
			if (ttUSHORT(data + 8) != 1)
				return 0;
			length = ttUSHORT(data + 10);
			if (table_length < length)
				length = table_length;
			for (k = 0; k < length; k++)
			{
				table[k].glyph1 = ttUSHORT(data + 18 + k * 6);
				table[k].glyph2 = ttUSHORT(data + 20 + k * 6);
				table[k].advance = ttSHORT(data + 22 + k * 6);
			}

			return length;
		}

		public static int stbtt__GetGlyphKernInfoAdvance(stbtt_fontinfo info, int glyph1, int glyph2)
		{
			var data = info.data + info.kern;
			uint needle = 0;
			uint straw = 0;
			var l = 0;
			var r = 0;
			var m = 0;
			if (info.kern == 0)
				return 0;
			if (ttUSHORT(data + 2) < 1)
				return 0;
			if (ttUSHORT(data + 8) != 1)
				return 0;
			l = 0;
			r = ttUSHORT(data + 10) - 1;
			needle = (uint) ((glyph1 << 16) | glyph2);
			while (l <= r)
			{
				m = (l + r) >> 1;
				straw = ttULONG(data + 18 + m * 6);
				if (needle < straw)
					r = m - 1;
				else if (needle > straw)
					l = m + 1;
				else
					return ttSHORT(data + 22 + m * 6);
			}

			return 0;
		}

		public static int stbtt__GetGlyphGPOSInfoAdvance(stbtt_fontinfo info, int glyph1, int glyph2)
		{
			ushort lookupListOffset = 0;
			FakePtr<byte> lookupList;
			ushort lookupCount = 0;
			FakePtr<byte> data;
			var i = 0;
			if (info.gpos == 0)
				return 0;
			data = info.data + info.gpos;
			if (ttUSHORT(data + 0) != 1)
				return 0;
			if (ttUSHORT(data + 2) != 0)
				return 0;
			lookupListOffset = ttUSHORT(data + 8);
			lookupList = data + lookupListOffset;
			lookupCount = ttUSHORT(lookupList);
			for (i = 0; i < lookupCount; ++i)
			{
				var lookupOffset = ttUSHORT(lookupList + 2 + 2 * i);
				var lookupTable = lookupList + lookupOffset;
				var lookupType = ttUSHORT(lookupTable);
				var subTableCount = ttUSHORT(lookupTable + 4);
				var subTableOffsets = lookupTable + 6;
				switch (lookupType)
				{
					case 2:
					{
						var sti = 0;
						for (sti = 0; sti < subTableCount; sti++)
						{
							var subtableOffset = ttUSHORT(subTableOffsets + 2 * sti);
							var table = lookupTable + subtableOffset;
							var posFormat = ttUSHORT(table);
							var coverageOffset = ttUSHORT(table + 2);
							var coverageIndex = stbtt__GetCoverageIndex(table + coverageOffset, glyph1);
							if (coverageIndex == -1)
								continue;
							switch (posFormat)
							{
								case 1:
								{
									var l = 0;
									var r = 0;
									var m = 0;
									var straw = 0;
									var needle = 0;
									var valueFormat1 = ttUSHORT(table + 4);
									var valueFormat2 = ttUSHORT(table + 6);
									var valueRecordPairSizeInBytes = 2;
									var pairSetCount = ttUSHORT(table + 8);
									var pairPosOffset = ttUSHORT(table + 10 + 2 * coverageIndex);
									var pairValueTable = table + pairPosOffset;
									var pairValueCount = ttUSHORT(pairValueTable);
									var pairValueArray = pairValueTable + 2;
									if (valueFormat1 != 4)
										return 0;
									if (valueFormat2 != 0)
										return 0;
									needle = glyph2;
									r = pairValueCount - 1;
									l = 0;
									while (l <= r)
									{
										ushort secondGlyph = 0;
										FakePtr<byte> pairValue;
										m = (l + r) >> 1;
										pairValue = pairValueArray + (2 + valueRecordPairSizeInBytes) * m;
										secondGlyph = ttUSHORT(pairValue);
										straw = secondGlyph;
										if (needle < straw)
										{
											r = m - 1;
										}
										else if (needle > straw)
										{
											l = m + 1;
										}
										else
										{
											var xAdvance = ttSHORT(pairValue + 2);
											return xAdvance;
										}
									}
								}
									break;
								case 2:
								{
									var valueFormat1 = ttUSHORT(table + 4);
									var valueFormat2 = ttUSHORT(table + 6);
									var classDef1Offset = ttUSHORT(table + 8);
									var classDef2Offset = ttUSHORT(table + 10);
									var glyph1class = stbtt__GetGlyphClass(table + classDef1Offset, glyph1);
									var glyph2class = stbtt__GetGlyphClass(table + classDef2Offset, glyph2);
									var class1Count = ttUSHORT(table + 12);
									var class2Count = ttUSHORT(table + 14);
									if (valueFormat1 != 4)
										return 0;
									if (valueFormat2 != 0)
										return 0;
									if (glyph1class >= 0 && glyph1class < class1Count && glyph2class >= 0 &&
									    glyph2class < class2Count)
									{
										var class1Records = table + 16;
										var class2Records = class1Records + 2 * glyph1class * class2Count;
										var xAdvance = ttSHORT(class2Records + 2 * glyph2class);
										return xAdvance;
									}
								}
									break;
							}
						}

						break;
					}
				}
			}

			return 0;
		}

		public static int stbtt_GetGlyphKernAdvance(stbtt_fontinfo info, int g1, int g2)
		{
			var xAdvance = 0;
			if (info.gpos != 0)
				xAdvance += stbtt__GetGlyphGPOSInfoAdvance(info, g1, g2);
			else if (info.kern != 0)
				xAdvance += stbtt__GetGlyphKernInfoAdvance(info, g1, g2);
			return xAdvance;
		}

		public static int stbtt_GetCodepointKernAdvance(stbtt_fontinfo info, int ch1, int ch2)
		{
			if (info.kern == 0 && info.gpos == 0)
				return 0;
			return stbtt_GetGlyphKernAdvance(info, stbtt_FindGlyphIndex(info, ch1), stbtt_FindGlyphIndex(info, ch2));
		}

		public static void stbtt_GetCodepointHMetrics(stbtt_fontinfo info, int codepoint, ref int advanceWidth,
			ref int leftSideBearing)
		{
			stbtt_GetGlyphHMetrics(info, stbtt_FindGlyphIndex(info, codepoint), ref advanceWidth, ref leftSideBearing);
		}

		public static void stbtt_GetFontVMetrics(stbtt_fontinfo info, out int ascent, out int descent, out int lineGap)
		{
			ascent = ttSHORT(info.data + info.hhea + 4);
			descent = ttSHORT(info.data + info.hhea + 6);
			lineGap = ttSHORT(info.data + info.hhea + 8);
		}

		public static int stbtt_GetFontVMetricsOS2(stbtt_fontinfo info, ref int typoAscent, ref int typoDescent,
			ref int typoLineGap)
		{
			var tab = (int) stbtt__find_table(info.data, (uint) info.fontstart, "OS/2");
			if (tab == 0)
				return 0;
			typoAscent = ttSHORT(info.data + tab + 68);
			typoDescent = ttSHORT(info.data + tab + 70);
			typoLineGap = ttSHORT(info.data + tab + 72);
			return 1;
		}

		public static void stbtt_GetFontBoundingBox(stbtt_fontinfo info, ref int x0, ref int y0, ref int x1, ref int y1)
		{
			x0 = ttSHORT(info.data + info.head + 36);
			y0 = ttSHORT(info.data + info.head + 38);
			x1 = ttSHORT(info.data + info.head + 40);
			y1 = ttSHORT(info.data + info.head + 42);
		}

		public static float stbtt_ScaleForPixelHeight(stbtt_fontinfo info, float height)
		{
			var fheight = ttSHORT(info.data + info.hhea + 4) - ttSHORT(info.data + info.hhea + 6);
			return height / fheight;
		}

		public static float stbtt_ScaleForMappingEmToPixels(stbtt_fontinfo info, float pixels)
		{
			var unitsPerEm = (int) ttUSHORT(info.data + info.head + 18);
			return pixels / unitsPerEm;
		}

		public static FakePtr<byte> stbtt_FindSVGDoc(stbtt_fontinfo info, int gl)
		{
			var i = 0;
			var data = info.data;
			var svg_doc_list = data + stbtt__get_svg(info);
			var numEntries = (int) ttUSHORT(svg_doc_list);
			var svg_docs = svg_doc_list + 2;
			for (i = 0; i < numEntries; i++)
			{
				var svg_doc = svg_docs + 12 * i;
				if (gl >= ttUSHORT(svg_doc) && gl <= ttUSHORT(svg_doc + 2))
					return svg_doc;
			}

			return FakePtr<byte>.Null;
		}

		public static int stbtt_GetGlyphSVG(stbtt_fontinfo info, int gl, ref FakePtr<byte> svg)
		{
			var data = info.data;
			FakePtr<byte> svg_doc;
			if (info.svg == 0)
				return 0;
			svg_doc = stbtt_FindSVGDoc(info, gl);
			if (!svg_doc.IsNull)
			{
				svg = data + info.svg + ttULONG(svg_doc + 4);
				return (int) ttULONG(svg_doc + 8);
			}

			return 0;
		}

		public static int stbtt_GetCodepointSVG(stbtt_fontinfo info, int unicode_codepoint, ref FakePtr<byte> svg)
		{
			return stbtt_GetGlyphSVG(info, stbtt_FindGlyphIndex(info, unicode_codepoint), ref svg);
		}

		public static void stbtt_GetGlyphBitmapBoxSubpixel(stbtt_fontinfo font, int glyph, float scale_x, float scale_y,
			float shift_x, float shift_y, ref int ix0, ref int iy0, ref int ix1, ref int iy1)
		{
			var x0 = 0;
			var y0 = 0;
			var x1 = 0;
			var y1 = 0;
			if (stbtt_GetGlyphBox(font, glyph, ref x0, ref y0, ref x1, ref y1) == 0)
			{
				ix0 = 0;
				iy0 = 0;
				ix1 = 0;
				iy1 = 0;
			}
			else
			{
				ix0 = (int) Math.Floor(x0 * scale_x + shift_x);
				iy0 = (int) Math.Floor(-y1 * scale_y + shift_y);
				ix1 = (int) Math.Ceiling(x1 * scale_x + shift_x);
				iy1 = (int) Math.Ceiling(-y0 * scale_y + shift_y);
			}
		}

		public static void stbtt_GetGlyphBitmapBox(stbtt_fontinfo font, int glyph, float scale_x, float scale_y,
			ref int ix0, ref int iy0, ref int ix1, ref int iy1)
		{
			stbtt_GetGlyphBitmapBoxSubpixel(font, glyph, scale_x, scale_y, 0.0f, 0.0f, ref ix0, ref iy0, ref ix1,
				ref iy1);
		}

		public static void stbtt_GetCodepointBitmapBoxSubpixel(stbtt_fontinfo font, int codepoint, float scale_x,
			float scale_y, float shift_x, float shift_y, ref int ix0, ref int iy0, ref int ix1, ref int iy1)
		{
			stbtt_GetGlyphBitmapBoxSubpixel(font, stbtt_FindGlyphIndex(font, codepoint), scale_x, scale_y, shift_x,
				shift_y, ref ix0, ref iy0, ref ix1, ref iy1);
		}

		public static void stbtt_GetCodepointBitmapBox(stbtt_fontinfo font, int codepoint, float scale_x, float scale_y,
			ref int ix0, ref int iy0, ref int ix1, ref int iy1)
		{
			stbtt_GetCodepointBitmapBoxSubpixel(font, codepoint, scale_x, scale_y, 0.0f, 0.0f, ref ix0, ref iy0,
				ref ix1, ref iy1);
		}

		public static FakePtr<byte> stbtt_GetGlyphBitmapSubpixel(stbtt_fontinfo info, float scale_x, float scale_y,
			float shift_x, float shift_y, int glyph, ref int width, ref int height, ref int xoff, ref int yoff)
		{
			var ix0 = 0;
			var iy0 = 0;
			var ix1 = 0;
			var iy1 = 0;
			var gbm = new stbtt__bitmap();
			stbtt_vertex[] vertices;
			var num_verts = stbtt_GetGlyphShape(info, glyph, out vertices);
			if (scale_x == 0)
				scale_x = scale_y;
			if (scale_y == 0)
			{
				if (scale_x == 0) return FakePtr<byte>.Null;
				scale_y = scale_x;
			}

			stbtt_GetGlyphBitmapBoxSubpixel(info, glyph, scale_x, scale_y, shift_x, shift_y, ref ix0, ref iy0, ref ix1,
				ref iy1);
			gbm.w = ix1 - ix0;
			gbm.h = iy1 - iy0;
			width = gbm.w;
			height = gbm.h;
			xoff = ix0;
			yoff = iy0;
			if (gbm.w != 0 && gbm.h != 0)
			{
				gbm.pixels = FakePtr<byte>.CreateWithSize(gbm.w * gbm.h);
				gbm.stride = gbm.w;
				stbtt_Rasterize(gbm, 0.35f, vertices, num_verts, scale_x, scale_y, shift_x, shift_y, ix0, iy0, 1);
			}

			return gbm.pixels;
		}

		public static FakePtr<byte> stbtt_GetGlyphBitmap(stbtt_fontinfo info, float scale_x, float scale_y, int glyph,
			ref int width, ref int height, ref int xoff, ref int yoff)
		{
			return stbtt_GetGlyphBitmapSubpixel(info, scale_x, scale_y, 0.0f, 0.0f, glyph, ref width, ref height,
				ref xoff, ref yoff);
		}

		public static void stbtt_MakeGlyphBitmapSubpixel(stbtt_fontinfo info, FakePtr<byte> output, int out_w,
			int out_h, int out_stride, float scale_x, float scale_y, float shift_x, float shift_y, int glyph)
		{
			var ix0 = 0;
			var iy0 = 0;
			var ix1 = 0;
			var iy1 = 0;
			stbtt_vertex[] vertices;
			var num_verts = stbtt_GetGlyphShape(info, glyph, out vertices);
			var gbm = new stbtt__bitmap();
			stbtt_GetGlyphBitmapBoxSubpixel(info, glyph, scale_x, scale_y, shift_x, shift_y, ref ix0, ref iy0, ref ix1,
				ref iy1);
			gbm.pixels = output;
			gbm.w = out_w;
			gbm.h = out_h;
			gbm.stride = out_stride;
			if (gbm.w != 0 && gbm.h != 0)
				stbtt_Rasterize(gbm, 0.35f, vertices, num_verts, scale_x, scale_y, shift_x, shift_y, ix0, iy0, 1);
		}

		public static void stbtt_MakeGlyphBitmap(stbtt_fontinfo info, FakePtr<byte> output, int out_w, int out_h,
			int out_stride, float scale_x, float scale_y, int glyph)
		{
			stbtt_MakeGlyphBitmapSubpixel(info, output, out_w, out_h, out_stride, scale_x, scale_y, 0.0f, 0.0f, glyph);
		}

		public static FakePtr<byte> stbtt_GetCodepointBitmapSubpixel(stbtt_fontinfo info, float scale_x, float scale_y,
			float shift_x, float shift_y, int codepoint, ref int width, ref int height, ref int xoff, ref int yoff)
		{
			return stbtt_GetGlyphBitmapSubpixel(info, scale_x, scale_y, shift_x, shift_y,
				stbtt_FindGlyphIndex(info, codepoint), ref width, ref height, ref xoff, ref yoff);
		}

		public static void stbtt_MakeCodepointBitmapSubpixelPrefilter(stbtt_fontinfo info, FakePtr<byte> output,
			int out_w, int out_h, int out_stride, float scale_x, float scale_y, float shift_x, float shift_y,
			int oversample_x, int oversample_y, ref float sub_x, ref float sub_y, int codepoint)
		{
			stbtt_MakeGlyphBitmapSubpixelPrefilter(info, output, out_w, out_h, out_stride, scale_x, scale_y, shift_x,
				shift_y, oversample_x, oversample_y, ref sub_x, ref sub_y, stbtt_FindGlyphIndex(info, codepoint));
		}

		public static void stbtt_MakeCodepointBitmapSubpixel(stbtt_fontinfo info, FakePtr<byte> output, int out_w,
			int out_h, int out_stride, float scale_x, float scale_y, float shift_x, float shift_y, int codepoint)
		{
			stbtt_MakeGlyphBitmapSubpixel(info, output, out_w, out_h, out_stride, scale_x, scale_y, shift_x, shift_y,
				stbtt_FindGlyphIndex(info, codepoint));
		}

		public static FakePtr<byte> stbtt_GetCodepointBitmap(stbtt_fontinfo info, float scale_x, float scale_y,
			int codepoint, ref int width, ref int height, ref int xoff, ref int yoff)
		{
			return stbtt_GetCodepointBitmapSubpixel(info, scale_x, scale_y, 0.0f, 0.0f, codepoint, ref width,
				ref height, ref xoff, ref yoff);
		}

		public static void stbtt_MakeCodepointBitmap(stbtt_fontinfo info, FakePtr<byte> output, int out_w, int out_h,
			int out_stride, float scale_x, float scale_y, int codepoint)
		{
			stbtt_MakeCodepointBitmapSubpixel(info, output, out_w, out_h, out_stride, scale_x, scale_y, 0.0f, 0.0f,
				codepoint);
		}

		public static void stbtt_MakeGlyphBitmapSubpixelPrefilter(stbtt_fontinfo info, FakePtr<byte> output, int out_w,
			int out_h, int out_stride, float scale_x, float scale_y, float shift_x, float shift_y, int prefilter_x,
			int prefilter_y, ref float sub_x, ref float sub_y, int glyph)
		{
			stbtt_MakeGlyphBitmapSubpixel(info, output, out_w - (prefilter_x - 1), out_h - (prefilter_y - 1),
				out_stride, scale_x, scale_y, shift_x, shift_y, glyph);
			if (prefilter_x > 1)
				stbtt__h_prefilter(output, out_w, out_h, out_stride, (uint) prefilter_x);
			if (prefilter_y > 1)
				stbtt__v_prefilter(output, out_w, out_h, out_stride, (uint) prefilter_y);
			sub_x = stbtt__oversample_shift(prefilter_x);
			sub_y = stbtt__oversample_shift(prefilter_y);
		}

		public static byte[] stbtt_GetGlyphSDF(stbtt_fontinfo info, float scale, int glyph, int padding,
			byte onedge_value, float pixel_dist_scale, ref int width, ref int height, ref int xoff, ref int yoff)
		{
			var scale_x = scale;
			var scale_y = scale;
			var ix0 = 0;
			var iy0 = 0;
			var ix1 = 0;
			var iy1 = 0;
			var w = 0;
			var h = 0;
			byte[] data = null;
			if (scale == 0)
				return null;
			stbtt_GetGlyphBitmapBoxSubpixel(info, glyph, scale, scale, 0.0f, 0.0f, ref ix0, ref iy0, ref ix1, ref iy1);
			if (ix0 == ix1 || iy0 == iy1)
				return null;
			ix0 -= padding;
			iy0 -= padding;
			ix1 += padding;
			iy1 += padding;
			w = ix1 - ix0;
			h = iy1 - iy0;
			width = w;
			height = h;
			xoff = ix0;
			yoff = iy0;
			scale_y = -scale_y;
			{
				var x = 0;
				var y = 0;
				var i = 0;
				var j = 0;
				float[] precompute;
				stbtt_vertex[] verts;
				var num_verts = stbtt_GetGlyphShape(info, glyph, out verts);
				data = new byte[w * h];
				precompute = new float[num_verts];
				for (i = 0, j = num_verts - 1; i < num_verts; j = i++)
					if (verts[i].type == STBTT_vline)
					{
						var x0 = verts[i].x * scale_x;
						var y0 = verts[i].y * scale_y;
						var x1 = verts[j].x * scale_x;
						var y1 = verts[j].y * scale_y;
						var dist = (float) Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
						precompute[i] = dist == 0 ? 0.0f : 1.0f / dist;
					}
					else if (verts[i].type == STBTT_vcurve)
					{
						var x2 = verts[j].x * scale_x;
						var y2 = verts[j].y * scale_y;
						var x1 = verts[i].cx * scale_x;
						var y1 = verts[i].cy * scale_y;
						var x0 = verts[i].x * scale_x;
						var y0 = verts[i].y * scale_y;
						var bx = x0 - 2 * x1 + x2;
						var by = y0 - 2 * y1 + y2;
						var len2 = bx * bx + by * by;
						if (len2 != 0.0f)
							precompute[i] = 1.0f / (bx * bx + by * by);
						else
							precompute[i] = 0.0f;
					}
					else
					{
						precompute[i] = 0.0f;
					}

				for (y = iy0; y < iy1; ++y)
				for (x = ix0; x < ix1; ++x)
				{
					float val = 0;
					var min_dist = 999999.0f;
					var sx = x + 0.5f;
					var sy = y + 0.5f;
					var x_gspace = sx / scale_x;
					var y_gspace = sy / scale_y;
					var winding = stbtt__compute_crossings_x(x_gspace, y_gspace, num_verts, verts);
					for (i = 0; i < num_verts; ++i)
					{
						var x0 = verts[i].x * scale_x;
						var y0 = verts[i].y * scale_y;
						var dist2 = (x0 - sx) * (x0 - sx) + (y0 - sy) * (y0 - sy);
						if (dist2 < min_dist * min_dist)
							min_dist = (float) Math.Sqrt(dist2);
						if (verts[i].type == STBTT_vline)
						{
							var x1 = verts[i - 1].x * scale_x;
							var y1 = verts[i - 1].y * scale_y;
							var dist = (float) Math.Abs((double) ((x1 - x0) * (y0 - sy) - (y1 - y0) * (x0 - sx))) *
							           precompute[i];
							if (dist < min_dist)
							{
								var dx = x1 - x0;
								var dy = y1 - y0;
								var px = x0 - sx;
								var py = y0 - sy;
								var t = -(px * dx + py * dy) / (dx * dx + dy * dy);
								if (t >= 0.0f && t <= 1.0f)
									min_dist = dist;
							}
						}
						else if (verts[i].type == STBTT_vcurve)
						{
							var x2 = verts[i - 1].x * scale_x;
							var y2 = verts[i - 1].y * scale_y;
							var x1 = verts[i].cx * scale_x;
							var y1 = verts[i].cy * scale_y;
							var box_x0 = (x0 < x1 ? x0 : x1) < x2 ? x0 < x1 ? x0 : x1 : x2;
							var box_y0 = (y0 < y1 ? y0 : y1) < y2 ? y0 < y1 ? y0 : y1 : y2;
							var box_x1 = (x0 < x1 ? x1 : x0) < x2 ? x2 : x0 < x1 ? x1 : x0;
							var box_y1 = (y0 < y1 ? y1 : y0) < y2 ? y2 : y0 < y1 ? y1 : y0;
							if (sx > box_x0 - min_dist && sx < box_x1 + min_dist && sy > box_y0 - min_dist &&
							    sy < box_y1 + min_dist)
							{
								var num = 0;
								var ax = x1 - x0;
								var ay = y1 - y0;
								var bx = x0 - 2 * x1 + x2;
								var by = y0 - 2 * y1 + y2;
								var mx = x0 - sx;
								var my = y0 - sy;
								var res = new float[3];
								float px = 0;
								float py = 0;
								float t = 0;
								float it = 0;
								var a_inv = precompute[i];
								if (a_inv == 0.0)
								{
									var a = 3 * (ax * bx + ay * by);
									var b = 2 * (ax * ax + ay * ay) + (mx * bx + my * by);
									var c = mx * ax + my * ay;
									if (a == 0.0)
									{
										if (b != 0.0) res[num++] = -c / b;
									}
									else
									{
										var discriminant = b * b - 4 * a * c;
										if (discriminant < 0)
										{
											num = 0;
										}
										else
										{
											var root = (float) Math.Sqrt(discriminant);
											res[0] = (-b - root) / (2 * a);
											res[1] = (-b + root) / (2 * a);
											num = 2;
										}
									}
								}
								else
								{
									var b = 3 * (ax * bx + ay * by) * a_inv;
									var c = (2 * (ax * ax + ay * ay) + (mx * bx + my * by)) * a_inv;
									var d = (mx * ax + my * ay) * a_inv;
									num = stbtt__solve_cubic(b, c, d, res);
								}

								if (num >= 1 && res[0] >= 0.0f && res[0] <= 1.0f)
								{
									t = res[0];
									it = 1.0f - t;
									px = it * it * x0 + 2 * t * it * x1 + t * t * x2;
									py = it * it * y0 + 2 * t * it * y1 + t * t * y2;
									dist2 = (px - sx) * (px - sx) + (py - sy) * (py - sy);
									if (dist2 < min_dist * min_dist)
										min_dist = (float) Math.Sqrt(dist2);
								}

								if (num >= 2 && res[1] >= 0.0f && res[1] <= 1.0f)
								{
									t = res[1];
									it = 1.0f - t;
									px = it * it * x0 + 2 * t * it * x1 + t * t * x2;
									py = it * it * y0 + 2 * t * it * y1 + t * t * y2;
									dist2 = (px - sx) * (px - sx) + (py - sy) * (py - sy);
									if (dist2 < min_dist * min_dist)
										min_dist = (float) Math.Sqrt(dist2);
								}

								if (num >= 3 && res[2] >= 0.0f && res[2] <= 1.0f)
								{
									t = res[2];
									it = 1.0f - t;
									px = it * it * x0 + 2 * t * it * x1 + t * t * x2;
									py = it * it * y0 + 2 * t * it * y1 + t * t * y2;
									dist2 = (px - sx) * (px - sx) + (py - sy) * (py - sy);
									if (dist2 < min_dist * min_dist)
										min_dist = (float) Math.Sqrt(dist2);
								}
							}
						}
					}

					if (winding == 0)
						min_dist = -min_dist;
					val = onedge_value + pixel_dist_scale * min_dist;
					if (val < 0)
						val = 0;
					else if (val > 255)
						val = 255;
					data[(y - iy0) * w + (x - ix0)] = (byte) val;
				}
			}

			return data;
		}

		public static byte[] stbtt_GetCodepointSDF(stbtt_fontinfo info, float scale, int codepoint, int padding,
			byte onedge_value, float pixel_dist_scale, ref int width, ref int height, ref int xoff, ref int yoff)
		{
			return stbtt_GetGlyphSDF(info, scale, stbtt_FindGlyphIndex(info, codepoint), padding, onedge_value,
				pixel_dist_scale, ref width, ref height, ref xoff, ref yoff);
		}

		public static FakePtr<byte> stbtt_GetFontNameString(stbtt_fontinfo font, ref int length, int platformID,
			int encodingID, int languageID, int nameID)
		{
			var i = 0;
			var count = 0;
			var stringOffset = 0;
			var fc = font.data;
			var offset = (uint) font.fontstart;
			var nm = stbtt__find_table(fc, offset, "name");
			if (nm == 0)
				return FakePtr<byte>.Null;
			count = ttUSHORT(fc + nm + 2);
			stringOffset = (int) (nm + ttUSHORT(fc + nm + 4));
			for (i = 0; i < count; ++i)
			{
				var loc = (uint) (nm + 6 + 12 * i);
				if (platformID == ttUSHORT(fc + loc + 0) && encodingID == ttUSHORT(fc + loc + 2) &&
				    languageID == ttUSHORT(fc + loc + 4) && nameID == ttUSHORT(fc + loc + 6))
				{
					length = ttUSHORT(fc + loc + 8);
					return fc + stringOffset + ttUSHORT(fc + loc + 10);
				}
			}

			return FakePtr<byte>.Null;
		}

		public static int stbtt_InitFont(stbtt_fontinfo info, byte[] data, int offset)
		{
			return stbtt_InitFont_internal(info, data, offset);
		}

		public class stbtt_fontinfo
		{
			public stbtt__buf cff = new stbtt__buf();
			public stbtt__buf charstrings = new stbtt__buf();
			public FakePtr<byte> data;
			public stbtt__buf fdselect = new stbtt__buf();
			public stbtt__buf fontdicts = new stbtt__buf();
			public int fontstart;
			public int glyf;
			public int gpos;
			public stbtt__buf gsubrs = new stbtt__buf();
			public int head;
			public int hhea;
			public int hmtx;
			public int index_map;
			public int indexToLocFormat;
			public int kern;
			public int loca;
			public int numGlyphs;
			public stbtt__buf subrs = new stbtt__buf();
			public int svg;
		}
	}
}
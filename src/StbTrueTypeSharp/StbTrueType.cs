namespace StbTrueTypeSharp
{
#if !STBSHARP_INTERNAL
	public
#else
	internal
#endif
	static partial class StbTrueType
	{
		public static uint stbtt__find_table(FakePtr<byte> data, uint fontstart, string tag)
		{
			int num_tables = ttUSHORT(data + fontstart + 4);
			var tabledir = fontstart + 12;
			int i;
			for (i = 0; i < num_tables; ++i)
			{
				var loc = (uint)(tabledir + 16 * i);
				if ((data + loc + 0)[0] == tag[0] && (data + loc + 0)[1] == tag[1] &&
					(data + loc + 0)[2] == tag[2] && (data + loc + 0)[3] == tag[3])
					return ttULONG(data + loc + 8);
			}

			return 0;
		}

		public static void stbtt__dict_get_ints(stbtt__buf b, int key, out uint _out_)
		{
			var temp = new FakePtr<uint>(new uint[1]);

			stbtt__dict_get_ints(b, key, 1, temp);

			_out_ = temp[0];
		}
	}
}
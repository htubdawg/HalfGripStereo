using System.Collections.Generic;
using UnityEngine;

namespace HalfGripStereo
{
	/// <summary>
	/// The stereo's pixel art, drawn in code in the manner of the cockpit (chunky flat colours, speckled plastic, hard edges): the head
	/// unit that sits left of the steering wheel, its glowing display, the CD wallet on the sun visor. Pixel arrays
	/// only (rows from the bottom, as Unity keeps them), so <c>tools/preview</c> draws the same pictures outside the game.
	/// </summary>
	internal static class Art
	{
		internal static readonly Color32 Clear = new Color32(0, 0, 0, 0);

		/// <summary>A picture being drawn: pixels with rows from the bottom.</summary>
		internal sealed class Canvas
		{
			public readonly int W;
			public readonly int H;
			public readonly Color32[] Px;

			public Canvas(int w, int h)
			{
				W = w;
				H = h;
				Px = new Color32[w * h];
			}

			public bool In(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;

			public void Set(int x, int y, Color32 c)
			{
				if (In(x, y))
				{
					Px[y * W + x] = c;
				}
			}

			public Color32 Get(int x, int y) => In(x, y) ? Px[y * W + x] : Clear;

			/// <summary>Fills x0..x1, y0..y1, both ends included.</summary>
			public void Rect(int x0, int y0, int x1, int y1, Color32 c)
			{
				for (int y = y0; y <= y1; y++)
				{
					for (int x = x0; x <= x1; x++)
					{
						Set(x, y, c);
					}
				}
			}

			/// <summary>A filled disc round the middle of pixel (cx, cy), or between pixels when the centre ends in .5.</summary>
			public void Disc(float cx, float cy, float r, Color32 c)
			{
				for (int y = Mathf.FloorToInt(cy - r); y <= Mathf.CeilToInt(cy + r); y++)
				{
					for (int x = Mathf.FloorToInt(cx - r); x <= Mathf.CeilToInt(cx + r); x++)
					{
						float dx = x - cx, dy = y - cy;
						if (dx * dx + dy * dy <= r * r)
						{
							Set(x, y, c);
						}
					}
				}
			}

			/// <summary>Varies every drawn pixel a little, hard steps only, the speckle of the dash's plastic.</summary>
			public void Speckle(int x0, int y0, int x1, int y1, int amount, int seed)
			{
				for (int y = y0; y <= y1; y++)
				{
					for (int x = x0; x <= x1; x++)
					{
						if (!In(x, y) || Px[y * W + x].a == 0)
						{
							continue;
						}
						int h = Hash(x, y, seed) % 7;
						int d = h == 0 ? amount : h == 1 ? -amount : 0;
						Px[y * W + x] = Shade(Px[y * W + x], d);
					}
				}
			}

			/// <summary>Draws another picture onto this one, its bottom left at (x, y), skipping its clear pixels.</summary>
			public void Paste(Canvas other, int x, int y)
			{
				for (int j = 0; j < other.H; j++)
				{
					for (int i = 0; i < other.W; i++)
					{
						Color32 c = other.Px[j * other.W + i];
						if (c.a > 0)
						{
							Set(x + i, y + j, c);
						}
					}
				}
			}
		}

		internal static int Hash(int x, int y, int seed)
		{
			unchecked
			{
				uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519);
				h = (h ^ (h >> 13)) * 1274126177;
				return (int)((h ^ (h >> 16)) & 0x7fffffff);
			}
		}

		internal static Color32 Shade(Color32 c, int d) => new Color32((byte)Mathf.Clamp(c.r + d, 0, 255), (byte)Mathf.Clamp(c.g + d, 0, 255), (byte)Mathf.Clamp(c.b + d, 0, 255), c.a);

		// ---------------------------------------------------------------------------------------------------------------------------
		// The 3x5 font, for the display and the labels. Lower case is drawn as upper case; anything unknown as '?'.

		private static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
		{
			{ 'A', "010101111101101" }, { 'B', "110101110101110" }, { 'C', "011100100100011" }, { 'D', "110101101101110" },
			{ 'E', "111100110100111" }, { 'F', "111100110100100" }, { 'G', "011100101101011" }, { 'H', "101101111101101" },
			{ 'I', "111010010010111" }, { 'J', "001001001101010" }, { 'K', "101101110101101" }, { 'L', "100100100100111" },
			{ 'M', "101111111101101" }, { 'N', "110101101101101" }, { 'O', "010101101101010" }, { 'P', "110101110100100" },
			{ 'Q', "010101101110011" }, { 'R', "110101110101101" }, { 'S', "011100010001110" }, { 'T', "111010010010010" },
			{ 'U', "101101101101111" }, { 'V', "101101101101010" }, { 'W', "101101111111101" }, { 'X', "101101010101101" },
			{ 'Y', "101101010010010" }, { 'Z', "111001010100111" },
			{ '0', "111101101101111" }, { '1', "010110010010111" }, { '2', "110001010100111" }, { '3', "110001010001110" },
			{ '4', "101101111001001" }, { '5', "111100110001110" }, { '6', "011100111101111" }, { '7', "111001010010010" },
			{ '8', "111101111101111" }, { '9', "111101111001110" },
			{ ' ', "000000000000000" }, { ':', "000010000010000" }, { '-', "000000111000000" }, { '.', "000000000000010" },
			{ ',', "000000000010100" }, { '/', "001001010100100" }, { '!', "010010010000010" }, { '?', "110001010000010" },
			{ '\'', "010010000000000" }, { '(', "001010010010001" }, { ')', "100010010010100" }, { '&', "010101010101011" },
			{ '_', "000000000000111" }, { '+', "000010111010000" }, { '#', "101111101111101" }, { '=', "000111000111000" },
			{ '*', "000101010101000" }, { '[', "011010010010011" }, { ']', "110010010010110" },
			// The display's own signs: a full block (volume bar), play, pause.
			{ '█', "111111111111111" }, { '▶', "100110111110100" }, { '‖', "000101101101000" },
		};

		internal const int GlyphWidth = 3;
		internal const int GlyphHeight = 5;
		/// <summary>Pixels per letter along a line: the glyph and one pixel of space.</summary>
		internal const int Advance = GlyphWidth + 1;

		internal static bool Known(char c) => Glyphs.ContainsKey(char.ToUpperInvariant(c));

		/// <summary>Writes <paramref name="text"/> with its first letter's bottom left at (x, y); letters off the canvas are cut.</summary>
		internal static void Text(Canvas c, string text, int x, int y, Color32 colour)
		{
			for (int i = 0; i < text.Length; i++)
			{
				char ch = char.ToUpperInvariant(text[i]);
				if (!Glyphs.TryGetValue(ch, out string g))
				{
					g = Glyphs['?'];
				}
				for (int row = 0; row < GlyphHeight; row++)
				{
					for (int col = 0; col < GlyphWidth; col++)
					{
						if (g[row * GlyphWidth + col] == '1')
						{
							c.Set(x + i * Advance + col, y + GlyphHeight - 1 - row, colour);
						}
					}
				}
			}
		}

		// ---------------------------------------------------------------------------------------------------------------------------
		// The head unit: a black single-piece car stereo, a little taller than one DIN, seen face on like the dash's dials.

		private static readonly Color32 Outline = new Color32(12, 12, 14, 255);
		private static readonly Color32 Body = new Color32(40, 40, 45, 255);
		private static readonly Color32 BodyTop = new Color32(66, 66, 72, 255);
		private static readonly Color32 BodyLow = new Color32(28, 28, 32, 255);
		private static readonly Color32 Chrome = new Color32(150, 150, 158, 255);
		private static readonly Color32 ChromeDark = new Color32(92, 92, 100, 255);
		private static readonly Color32 Slot = new Color32(6, 6, 8, 255);
		private static readonly Color32 KnobRim = new Color32(100, 100, 108, 255);
		private static readonly Color32 KnobFace = new Color32(58, 58, 64, 255);
		private static readonly Color32 KnobDark = new Color32(30, 30, 34, 255);
		private static readonly Color32 KnobMark = new Color32(220, 220, 220, 255);
		private static readonly Color32 Key = new Color32(64, 64, 70, 255);
		private static readonly Color32 KeyTop = new Color32(98, 98, 106, 255);
		private static readonly Color32 KeyLow = new Color32(22, 22, 26, 255);
		private static readonly Color32 KeyPressed = new Color32(44, 44, 49, 255);
		private static readonly Color32 KeySign = new Color32(190, 190, 198, 255);
		private static readonly Color32 LedOn = new Color32(236, 52, 36, 255);
		private static readonly Color32 LedOff = new Color32(72, 22, 20, 255);
		private static readonly Color32 Badge = new Color32(176, 150, 96, 255);

		/// <summary>The head unit's size in dash pixels.</summary>
		internal const int UnitWidth = 64;
		internal const int UnitHeight = 28;

		/// <summary>The display's window on the unit (inside the chrome frame), in dash pixels; the display itself is drawn at <see cref="LcdDetail"/> times this.</summary>
		internal static readonly RectInt LcdWindow = new RectInt(21, 10, 32, 10);
		internal const int LcdDetail = 2;

		/// <summary>The parts of the unit the hand can press.</summary>
		internal enum Part { Knob, Power, Prev, Play, Next, Eject }

		internal static readonly Part[] Parts = { Part.Knob, Part.Power, Part.Prev, Part.Play, Part.Next, Part.Eject };

		/// <summary>Where each part is on the unit, in dash pixels (the knob's box round its circle).</summary>
		internal static RectInt Place(Part part)
		{
			switch (part)
			{
				case Part.Knob: return new RectInt(3, 5, 15, 15);
				case Part.Power: return new RectInt(20, 2, 7, 5);
				case Part.Prev: return new RectInt(29, 2, 7, 5);
				case Part.Play: return new RectInt(38, 2, 7, 5);
				case Part.Next: return new RectInt(47, 2, 7, 5);
				default: return new RectInt(56, 21, 6, 4);
			}
		}

		/// <summary>
		/// The head unit's casing (everything but the display, which is its own glowing sprite over the window): <paramref name="pressed"/>
		/// is drawn pushed in, <paramref name="power"/> lights the power key's red light, <paramref name="knobTurn"/> (0 to 1) turns the
		/// volume knob's mark from 7 o'clock to 5 o'clock.
		/// </summary>
		internal static Canvas HeadUnit(Part? pressed, bool power, float knobTurn)
		{
			int w = UnitWidth, h = UnitHeight;
			Canvas c = new Canvas(w, h);
			c.Rect(0, 0, w - 1, h - 1, Outline);
			c.Rect(1, 1, w - 2, h - 2, Body);
			// A lighter lip along the top (light from above, as on the dash's parts), a darker one along the bottom.
			c.Rect(1, h - 3, w - 2, h - 2, BodyTop);
			c.Rect(1, 1, w - 2, 1, BodyLow);
			c.Speckle(1, 1, w - 2, h - 2, 5, 11);

			// The CD slot across the top, with a highlight under its lip, and the eject key at its end.
			c.Rect(20, 23, 53, 23, Slot);
			c.Rect(20, 22, 53, 22, BodyTop);
			Key3(c, Place(Part.Eject), pressed == Part.Eject);
			RectInt e = Place(Part.Eject);
			int ex = e.x + 1, ey = e.y + (pressed == Part.Eject ? 0 : 1);
			c.Set(ex + 1, ey + 2, KeySign);
			c.Rect(ex, ey + 1, ex + 2, ey + 1, KeySign);
			c.Rect(ex, ey, ex + 2, ey, KeySign);

			// The display's chrome frame; the window inside it is left dark, the display sprite covers it.
			RectInt l = LcdWindow;
			c.Rect(l.x - 2, l.y - 2, l.xMax + 1, l.yMax + 1, ChromeDark);
			c.Rect(l.x - 2, l.yMax + 1, l.xMax + 1, l.yMax + 1, Chrome);
			c.Rect(l.xMax + 1, l.y - 1, l.xMax + 1, l.yMax + 1, Chrome);
			c.Rect(l.x - 1, l.y - 1, l.xMax, l.yMax, Outline);
			c.Rect(l.x, l.y, l.xMax - 1, l.yMax - 1, new Color32(20, 12, 6, 255));

			// The volume knob: a chrome rim, a dark groove, a knurled face, a white mark that turns with the volume.
			RectInt k = Place(Part.Knob);
			float cx = k.x + (k.width - 1) * 0.5f, cy = k.y + (k.height - 1) * 0.5f;
			bool knobIn = pressed == Part.Knob;
			c.Disc(cx, cy, 7.4f, Outline);
			c.Disc(cx, cy, 6.6f, KnobRim);
			c.Disc(cx, cy, 5.6f, KnobDark);
			c.Disc(cx, cy + (knobIn ? -0.5f : 0f), 4.8f, knobIn ? KnobDark : KnobFace);
			for (int a = 0; a < 12; a++)
			{
				float ang = a * Mathf.PI / 6f;
				c.Set(Mathf.RoundToInt(cx + Mathf.Cos(ang) * 4.6f), Mathf.RoundToInt(cy + Mathf.Sin(ang) * 4.6f), KnobRim);
			}
			c.Disc(cx - 1.5f, cy + 1.5f, 1.1f, Shade(KnobFace, 26));
			float turn = Mathf.Lerp(Mathf.PI * 1.25f, -Mathf.PI * 0.25f, Mathf.Clamp01(knobTurn));
			for (float r = 1.5f; r <= 4.2f; r += 0.5f)
			{
				c.Set(Mathf.RoundToInt(cx + Mathf.Cos(turn) * r), Mathf.RoundToInt(cy + Mathf.Sin(turn) * r), KnobMark);
			}
			// Tick marks round the knob for the volume's range.
			for (int t = 0; t <= 4; t++)
			{
				float ang = Mathf.Lerp(Mathf.PI * 1.25f, -Mathf.PI * 0.25f, t / 4f);
				c.Set(Mathf.RoundToInt(cx + Mathf.Cos(ang) * 8.2f), Mathf.RoundToInt(cy + Mathf.Sin(ang) * 8.2f), ChromeDark);
			}

			// The keys under the display.
			foreach (Part p in new[] { Part.Power, Part.Prev, Part.Play, Part.Next })
			{
				RectInt r = Place(p);
				bool down = pressed == p;
				Key3(c, r, down);
				int sx = r.x + 2, sy = r.y + (down ? 1 : 2);
				switch (p)
				{
					case Part.Power:
						// The power sign (a ring open at the top, a stroke through it) beside a red light that is lit while on.
						c.Rect(sx - 1, sy - 1, sx - 1, sy, KeySign);
						c.Rect(sx + 1, sy - 1, sx + 1, sy, KeySign);
						c.Set(sx, sy - 1, KeySign);
						c.Rect(sx, sy, sx, sy + 1, KeySign);
						c.Set(sx + 3, sy + 1, power ? LedOn : LedOff);
						break;
					case Part.Prev:
						c.Rect(sx - 1, sy - 1, sx - 1, sy + 1, KeySign);
						c.Set(sx, sy, KeySign);
						c.Rect(sx + 1, sy - 1, sx + 1, sy + 1, KeySign);
						c.Set(sx + 2, sy, KeySign);
						c.Rect(sx + 3, sy - 1, sx + 3, sy + 1, KeySign);
						c.Set(sx + 2, sy, KeySign);
						break;
					case Part.Play:
						c.Rect(sx - 1, sy - 1, sx - 1, sy + 1, KeySign);
						c.Set(sx, sy, KeySign);
						c.Rect(sx + 2, sy - 1, sx + 2, sy + 1, KeySign);
						c.Rect(sx + 3, sy - 1, sx + 3, sy + 1, KeySign);
						break;
					case Part.Next:
						c.Rect(sx - 1, sy - 1, sx - 1, sy + 1, KeySign);
						c.Set(sx, sy, KeySign);
						c.Rect(sx + 1, sy - 1, sx + 1, sy + 1, KeySign);
						c.Set(sx + 2, sy, KeySign);
						c.Rect(sx + 3, sy - 1, sx + 3, sy + 1, KeySign);
						break;
				}
			}
			// Prev is the mirror of next: |<< rather than >>|.
			Mirror(c, Place(Part.Prev));

			// A small gold "CD" badge in the right hand corner, the way car stereos wore one.
			Text(c, "CD", 56, 11, Badge);
			c.Rect(56, 9, 62, 9, ChromeDark);
			return c;
		}

		/// <summary>A key: a raised block with a light top edge and a dark bottom edge, or pushed in, flat and darker.</summary>
		private static void Key3(Canvas c, RectInt r, bool down)
		{
			c.Rect(r.x, r.y, r.xMax - 1, r.yMax - 1, KeyLow);
			if (down)
			{
				c.Rect(r.x + 1, r.y, r.xMax - 2, r.yMax - 2, KeyPressed);
				return;
			}
			c.Rect(r.x, r.y + 1, r.xMax - 1, r.yMax - 1, Key);
			c.Rect(r.x, r.yMax - 1, r.xMax - 1, r.yMax - 1, KeyTop);
		}

		/// <summary>Mirrors the key signs (only their light pixels) left for right inside a key's box.</summary>
		private static void Mirror(Canvas c, RectInt r)
		{
			List<Vector2Int> lit = new List<Vector2Int>();
			for (int y = r.y; y < r.yMax; y++)
			{
				for (int x = r.x; x < r.xMax; x++)
				{
					if (Same(c.Get(x, y), KeySign))
					{
						lit.Add(new Vector2Int(x, y));
					}
				}
			}
			Color32 under = c.Get(r.x + 1, r.y + 2);
			foreach (Vector2Int p in lit)
			{
				c.Set(p.x, p.y, under);
			}
			foreach (Vector2Int p in lit)
			{
				c.Set(r.x + r.xMax - 1 - p.x, p.y, KeySign);
			}
		}

		private static bool Same(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

		// ---------------------------------------------------------------------------------------------------------------------------
		// The display: amber segments on a dark brown glass, with the faint ghost of every unlit segment, as on an old car stereo.

		internal static readonly Color32 LcdGlass = new Color32(34, 18, 8, 255);
		internal static readonly Color32 LcdGhost = new Color32(52, 30, 12, 255);
		internal static readonly Color32 LcdLit = new Color32(255, 168, 52, 255);
		internal static readonly Color32 LcdDim = new Color32(150, 90, 30, 255);

		/// <summary>Display pixels across and up.</summary>
		internal static int LcdWidth => LcdWindow.width * LcdDetail;
		internal static int LcdHeight => LcdWindow.height * LcdDetail;

		/// <summary>Letters that fit on one line of the display.</summary>
		internal static int LcdColumns => (LcdWidth - 1) / Advance;

		/// <summary>
		/// The display with its two lines: <paramref name="top"/> (the disc, the track and the time) and <paramref name="bottom"/> (the
		/// song's name, scrolled by the caller). Off, only the ghosts of the segments show.
		/// </summary>
		internal static void Lcd(Color32[] into, string top, string bottom, bool on)
		{
			int w = LcdWidth, h = LcdHeight;
			Canvas c = new Canvas(w, h);
			c.Rect(0, 0, w - 1, h - 1, LcdGlass);
			int cols = LcdColumns;
			string ghost = new string('█', cols);
			int x0 = (w - cols * Advance + 1) / 2;
			int topY = h - 2 - GlyphHeight;
			int bottomY = 3;
			Text(c, ghost, x0, topY, LcdGhost);
			Text(c, ghost, x0, bottomY, LcdGhost);
			if (on)
			{
				Text(c, Fit(top, cols), x0, topY, LcdDim);
				Text(c, Fit(bottom, cols), x0, bottomY, LcdLit);
			}
			System.Array.Copy(c.Px, into, into.Length);
		}

		private static string Fit(string s, int n) => s == null ? "" : s.Length > n ? s.Substring(0, n) : s;

		// ---------------------------------------------------------------------------------------------------------------------------
		// The CD wallet on the sun visor.

		private static readonly Color32 Visor = new Color32(70, 66, 62, 255);
		private static readonly Color32 VisorEdge = new Color32(44, 42, 40, 255);
		private static readonly Color32 VisorLight = new Color32(92, 88, 82, 255);
		private static readonly Color32 Stitch = new Color32(110, 104, 96, 255);
		private static readonly Color32 Hinge = new Color32(128, 128, 134, 255);
		private static readonly Color32 HingeDark = new Color32(70, 70, 76, 255);
		private static readonly Color32 Wallet = new Color32(22, 22, 24, 255);
		private static readonly Color32 WalletEdge = new Color32(46, 46, 50, 255);
		private static readonly Color32 Pocket = new Color32(16, 16, 18, 255);
		private static readonly Color32 Sleeve = new Color32(150, 170, 186, 255);
		private static readonly Color32 Strap = new Color32(34, 34, 38, 255);
		private static readonly Color32 Silver = new Color32(196, 200, 208, 255);
		private static readonly Color32 SilverDark = new Color32(132, 136, 146, 255);
		private static readonly Color32 Hole = new Color32(14, 14, 16, 255);
		private static readonly Color32 LabelInk = new Color32(30, 30, 30, 255);

		/// <summary>One CD as the wallet shows it: its label colour and a short mark on the label.</summary>
		internal struct DiscLook
		{
			public Color32 Label;
			public string Mark;
			/// <summary>The disc is in the stereo: its pocket is empty.</summary>
			public bool Out;
		}

		/// <summary>Pocket width (and pitch) in dash pixels, and the disc's diameter.</summary>
		internal const int PocketWidth = 13;
		internal const int PocketGap = 1;
		internal const int DiscSize = 11;
		internal const int VisorHeight = 30;

		/// <summary>The visor's width for <paramref name="discs"/> pockets.</summary>
		internal static int VisorWidth(int discs) => 2 * 7 + Mathf.Max(1, discs) * (PocketWidth + PocketGap) - PocketGap + 4;

		/// <summary>Where pocket <paramref name="i"/> is on the visor, in dash pixels: what the hand clicks.</summary>
		internal static RectInt PocketPlace(int i) => new RectInt(9 + i * (PocketWidth + PocketGap), 5, PocketWidth, 20);

		/// <summary>
		/// The sun visor, folded up against the roof, with a black CD wallet strapped round it: one pocket per disc, each disc showing
		/// over the top of its clear sleeve with its number on the strap below. A disc in the stereo leaves its pocket empty.
		/// </summary>
		internal static Canvas VisorWallet(IList<DiscLook> discs)
		{
			int n = Mathf.Max(1, discs.Count);
			int w = VisorWidth(n), h = VisorHeight;
			Canvas c = new Canvas(w, h);
			// The padded visor: rounded corners at the bottom (the free edge), straight along the hinge at the top.
			c.Rect(1, 0, w - 2, h - 1, VisorEdge);
			c.Rect(0, 2, w - 1, h - 1, VisorEdge);
			c.Rect(2, 1, w - 3, h - 2, Visor);
			c.Rect(1, 3, w - 2, h - 2, Visor);
			c.Rect(2, h - 3, w - 3, h - 2, VisorLight);
			c.Speckle(1, 1, w - 2, h - 2, 4, 5);
			for (int x = 3; x < w - 3; x += 2)
			{
				c.Set(x, 2, Stitch);
				c.Set(x, h - 4, Stitch);
			}
			// The hinge rod showing at the top corners.
			c.Rect(0, h - 2, 3, h - 1, HingeDark);
			c.Rect(0, h - 1, 3, h - 1, Hinge);
			c.Rect(w - 4, h - 2, w - 1, h - 1, HingeDark);
			c.Rect(w - 4, h - 1, w - 1, h - 1, Hinge);

			// The wallet, with its elastic straps round the visor at each end.
			int wx0 = 6, wx1 = w - 7, wy0 = 4, wy1 = h - 5;
			c.Rect(wx0, wy0, wx1, wy1, WalletEdge);
			c.Rect(wx0 + 1, wy0 + 1, wx1 - 1, wy1 - 1, Wallet);
			c.Rect(wx0 - 3, 1, wx0 - 1, h - 2, Strap);
			c.Rect(wx1 + 1, 1, wx1 + 3, h - 2, Strap);
			c.Speckle(wx0, wy0, wx1, wy1, 3, 9);

			for (int i = 0; i < n; i++)
			{
				RectInt p = PocketPlace(i);
				DiscLook d = i < discs.Count ? discs[i] : new DiscLook { Out = true };
				int px0 = p.x, px1 = p.xMax - 1;
				int sleeveTop = p.y + 11;
				c.Rect(px0, p.y + 6, px1, p.yMax - 1, Pocket);
				float cx = px0 + (PocketWidth - 1) * 0.5f, cy = sleeveTop + 1f;
				if (!d.Out)
				{
					DrawDisc(c, cx, cy, DiscSize * 0.5f, d.Label);
				}
				// The clear sleeve over the disc's lower half: a light top edge and a glint, the disc seen through it a shade paler.
				for (int y = p.y + 6; y <= sleeveTop; y++)
				{
					for (int x = px0; x <= px1; x++)
					{
						Color32 under = c.Get(x, y);
						c.Set(x, y, Shade(under, 22));
					}
				}
				c.Rect(px0, sleeveTop, px1, sleeveTop, Sleeve);
				c.Set(px0 + 1, sleeveTop - 2, Shade(Sleeve, -30));
				c.Set(px0 + 2, sleeveTop - 3, Shade(Sleeve, -30));
				// Its number on the strap underneath, in white pen.
				string mark = string.IsNullOrEmpty(d.Mark) ? (i + 1).ToString() : d.Mark;
				int tw = mark.Length * Advance - 1;
				Text(c, mark, px0 + (PocketWidth - tw) / 2, p.y, new Color32(210, 206, 196, 255));
			}
			return c;
		}

		/// <summary>A CD: a silver rim, the label, a clear ring round the hole and the hole, with a glint on the silver.</summary>
		internal static void DrawDisc(Canvas c, float cx, float cy, float r, Color32 label)
		{
			c.Disc(cx, cy, r, SilverDark);
			c.Disc(cx, cy, r - 0.7f, Silver);
			c.Disc(cx, cy, r * 0.68f, label);
			c.Disc(cx, cy, Mathf.Max(1.2f, r * 0.3f), SilverDark);
			c.Disc(cx, cy, Mathf.Max(0.6f, r * 0.14f), Hole);
			c.Set(Mathf.RoundToInt(cx + r * 0.55f), Mathf.RoundToInt(cy + r * 0.62f), new Color32(255, 255, 255, 255));
		}

		/// <summary>The label colour of a disc, from the song: the colours of the maps the songs belong to.</summary>
		internal static Color32 LabelFor(string title)
		{
			string t = (title ?? "").ToUpperInvariant();
			if (t.StartsWith("FOREST")) return new Color32(58, 150, 64, 255);
			if (t.StartsWith("NEON 2")) return new Color32(150, 80, 230, 255);
			if (t.StartsWith("NEON")) return new Color32(236, 64, 176, 255);
			if (t.StartsWith("FACTORY")) return new Color32(232, 122, 34, 255);
			if (t.StartsWith("STADION") || t.StartsWith("STADIUM")) return new Color32(54, 114, 226, 255);
			if (t.StartsWith("WINTER")) return new Color32(186, 224, 246, 255);
			if (t.StartsWith("MIX")) return new Color32(236, 234, 222, 255);
			int h = Hash(t.Length, t.GetHashCode(), 3);
			Color c = Color.HSVToRGB((h % 360) / 360f, 0.6f, 0.85f);
			return new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), 255);
		}
	}
}

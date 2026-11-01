using System.Collections.Generic;
using UnityEngine;

namespace HalfGripStereo
{
	/// <summary>
	/// The head unit's face as the cockpit camera sees it. The dash beside the wheel is drawn in perspective: its edges run forward,
	/// toward the vanishing point in the middle of the view (the owner's test of 0.2.1, with the lines drawn on: "you have the
	/// perspective completely opposite"). So the face's left end is the near one, tallest; its top and bottom edges run toward the
	/// vanishing point, closing in to the right; and the face is narrower than face on.
	///
	/// The flat art (<see cref="Art.HeadUnit"/>, 64x28) gives the layout. The plain parts (outline, lips, slot, display frame, keys) are
	/// drawn straight into the face column by column, each line kept a pixel thick wherever the face is shallower; the knob is an
	/// ellipse; the key signs, the badge and the display's letters stay upright and whole, lifted to their place. No picture is
	/// resampled, so nothing breaks apart (0.2.0's sheared face did).
	///
	/// Coordinates: x in face pixels from the left edge, rows from the canvas bottom; (u, v) a point of the flat art.
	/// </summary>
	internal sealed class Face
	{
		/// <summary>The face's width in cockpit pixels, and the canvas's height.</summary>
		internal readonly int W;
		internal readonly int Rows;
		/// <summary>The canvas's bottom row, in cockpit pixels above the face's bottom left corner (negative when the face dips).</summary>
		internal readonly int Low;
		private readonly float bottomSlope;
		private readonly float topSlope;

		/// <param name="vanish">The vanishing point, in cockpit pixels from the face's bottom left corner.</param>
		/// <param name="amount">0 face on (the 0.1.0 look), 1 as the camera sees it.</param>
		internal Face(Vector2 vanish, float amount)
		{
			amount = Mathf.Clamp01(amount);
			W = Mathf.RoundToInt(Art.UnitWidth * (1f - 0.3f * amount));
			// A vanishing point on or left of the face would turn it inside out: keep it well to the right.
			float vx = Mathf.Max(vanish.x, W * 2f);
			bottomSlope = vanish.y / vx * amount;
			topSlope = (vanish.y - Art.UnitHeight) / vx * amount;
			float lo = Mathf.Min(0f, bottomSlope * W);
			float hi = Mathf.Max(Art.UnitHeight, Art.UnitHeight + topSlope * W);
			Low = Mathf.FloorToInt(lo);
			Rows = Mathf.CeilToInt(hi) - Low + 1;
		}

		/// <summary>The face pixel column of flat column <paramref name="u"/> (a column edge).</summary>
		internal float X(float u) => u * W / Art.UnitWidth;

		/// <summary>The flat column at face x.</summary>
		internal float U(float x) => x * Art.UnitWidth / W;

		/// <summary>Height above the canvas bottom of flat row <paramref name="v"/> at face x (continuous).</summary>
		internal float Y(float x, float v)
		{
			float b = bottomSlope * x, t = Art.UnitHeight + topSlope * x;
			return b + (t - b) * v / Art.UnitHeight - Low;
		}

		/// <summary>A flat point's place on the canvas (continuous).</summary>
		internal Vector2 Map(float u, float v)
		{
			float x = X(u);
			return new Vector2(x, Y(x, v));
		}

		/// <summary>The face's height in rows at face x, against the flat art's 28: how much it is squeezed there.</summary>
		internal float Squeeze(float x) => (Y(x, Art.UnitHeight) - Y(x, 0f)) / Art.UnitHeight;

		/// <summary>The row holding flat row <paramref name="v"/> in column x (its pixel's middle).</summary>
		internal int Row(int x, float v) => Mathf.FloorToInt(Y(x + 0.5f, v));

		/// <summary>The columns of a flat span [u0, u1).</summary>
		internal void Columns(float u0, float u1, out int x0, out int x1)
		{
			x0 = Mathf.RoundToInt(X(u0));
			x1 = Mathf.Max(x0, Mathf.RoundToInt(X(u1)) - 1);
		}

		/// <summary>
		/// The rows of a flat box [v0, v1) in column x, shrunk by <paramref name="inset"/> pixels on each side but never to nothing, so
		/// nested rings each keep a pixel.
		/// </summary>
		internal void Span(int x, float v0, float v1, int inset, out int y0, out int y1)
		{
			int b = Row(x, v0), t = Mathf.Max(b, Mathf.FloorToInt(Y(x + 0.5f, v1) - 0.001f));
			y0 = b + inset;
			y1 = t - inset;
			if (y1 < y0)
			{
				y0 = y1 = (b + t) / 2;
			}
		}

		// ---------------------------------------------------------------------------------------------------------------------------
		// The casing.

		private static readonly Color32 Outline = new Color32(12, 12, 14, 255);
		private static readonly Color32 Body = new Color32(40, 40, 45, 255);
		private static readonly Color32 BodyTop = new Color32(66, 66, 72, 255);
		private static readonly Color32 BodyLow = new Color32(28, 28, 32, 255);
		private static readonly Color32 Chrome = new Color32(150, 150, 158, 255);
		private static readonly Color32 ChromeDark = new Color32(92, 92, 100, 255);
		private static readonly Color32 SlotDark = new Color32(6, 6, 8, 255);
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
		private static readonly Color32 Window = new Color32(20, 12, 6, 255);

		/// <summary>The display frame's flat box (round the window), and the window's.</summary>
		private static readonly Rect Frame = new Rect(19, 8, 36, 14);

		/// <summary>The knob's middle and radius on the flat art.</summary>
		private static readonly Vector2 KnobAt = new Vector2(10.5f, 12.5f);
		private const float KnobRadius = 7.4f;

		/// <summary>The knob's middle on the canvas and its radii (narrow across, the face being turned away).</summary>
		internal void Knob(out Vector2 centre, out float rx, out float ry)
		{
			centre = Map(KnobAt.x, KnobAt.y);
			rx = KnobRadius * W / Art.UnitWidth;
			ry = KnobRadius * Squeeze(centre.x);
		}

		/// <summary>The middle of a part on the canvas and the size of its box, for its click target.</summary>
		internal Rect Target(Art.Part p)
		{
			if (p == Art.Part.Knob)
			{
				Knob(out Vector2 c, out float rx, out float ry);
				return new Rect(c.x - rx - 1f, c.y - ry - 1f, 2f * rx + 2f, 2f * ry + 2f);
			}
			RectInt r = Art.Place(p);
			Columns(r.x, r.xMax, out int x0, out int x1);
			float lo = float.MaxValue, hi = float.MinValue;
			for (int x = x0; x <= x1; x++)
			{
				Span(x, r.y, r.yMax, 0, out int y0, out int y1);
				lo = Mathf.Min(lo, y0);
				hi = Mathf.Max(hi, y1 + 1);
			}
			return new Rect(x0, lo, x1 - x0 + 1, hi - lo);
		}

		/// <summary>The head unit's casing on this face (the display is its own sprite, see <see cref="Display"/>).</summary>
		internal Art.Canvas Casing(Art.Part? pressed, bool power, float knobTurn)
		{
			Art.Canvas c = new Art.Canvas(W, Rows);
			int h = Art.UnitHeight;
			for (int x = 0; x < W; x++)
			{
				Span(x, 0, h, 0, out int y0, out int y1);
				c.Rect(x, y0, x, y1, Outline);
				if (x == 0 || x == W - 1)
				{
					continue;
				}
				c.Rect(x, y0 + 1, x, y1 - 1, Body);
				// A lighter lip along the top (light from above), a darker one along the bottom.
				Span(x, h - 3, h, 0, out int ly0, out int _);
				c.Rect(x, Mathf.Max(y0 + 2, ly0), x, y1 - 1, BodyTop);
				c.Set(x, y0 + 1, BodyLow);
			}
			c.Speckle(1, 0, W - 2, Rows - 1, 5, 11);

			// The CD slot, with a highlight under its lip.
			Columns(Art.SlotLeft, Art.SlotRight + 1, out int sx0, out int sx1);
			for (int x = sx0; x <= sx1; x++)
			{
				int y = SlotRowAt(x);
				c.Set(x, y, SlotDark);
				c.Set(x, y - 1, BodyTop);
			}

			// The display's frame: chrome lit on top and on the right, dark on the left and bottom, a black ring, the window.
			Columns(Frame.xMin, Frame.xMax, out int fx0, out int fx1);
			for (int x = fx0; x <= fx1; x++)
			{
				Span(x, Frame.yMin, Frame.yMax, 0, out int y0, out int y1);
				c.Rect(x, y0, x, y1, x == fx1 ? Chrome : ChromeDark);
				c.Set(x, y1, Chrome);
				if (x > fx0 && x < fx1)
				{
					Span(x, Frame.yMin, Frame.yMax, 1, out int o0, out int o1);
					c.Rect(x, o0, x, o1, Outline);
				}
				if (x > fx0 + 1 && x < fx1 - 1)
				{
					WindowRows(x, out int w0, out int w1);
					c.Rect(x, w0, x, w1, Window);
				}
			}

			// The keys under the display and the eject key by the slot: raised blocks, or pushed in, with their signs upright on them.
			foreach (Art.Part p in new[] { Art.Part.Power, Art.Part.Prev, Art.Part.Play, Art.Part.Next, Art.Part.Eject })
			{
				DrawKey(c, p, pressed == p, power);
			}

			// The volume knob, an ellipse: a chrome rim, a dark groove, a knurled face, the mark turned with the volume, ticks round it.
			Knob(out Vector2 k, out float rx, out float ry);
			bool knobIn = pressed == Art.Part.Knob;
			float sx = rx / KnobRadius, sy = ry / KnobRadius;
			Oval(c, k.x, k.y, rx, ry, Outline);
			Oval(c, k.x, k.y, 6.6f * sx, 6.6f * sy, KnobRim);
			Oval(c, k.x, k.y, 5.6f * sx, 5.6f * sy, KnobDark);
			Oval(c, k.x, k.y - (knobIn ? 0.5f : 0f), 4.8f * sx, 4.8f * sy, knobIn ? KnobDark : KnobFace);
			for (int a = 0; a < 12; a++)
			{
				float ang = a * Mathf.PI / 6f;
				c.Set(Mathf.FloorToInt(k.x + Mathf.Cos(ang) * 4.6f * sx), Mathf.FloorToInt(k.y + Mathf.Sin(ang) * 4.6f * sy), KnobRim);
			}
			Oval(c, k.x - 1.5f * sx, k.y + 1.5f * sy, 1.1f, 1.1f, Art.Shade(KnobFace, 26));
			float turn = Mathf.Lerp(Mathf.PI * 1.25f, -Mathf.PI * 0.25f, Mathf.Clamp01(knobTurn));
			for (float r = 1.2f; r <= 4.2f; r += 0.4f)
			{
				c.Set(Mathf.FloorToInt(k.x + Mathf.Cos(turn) * r * sx), Mathf.FloorToInt(k.y + Mathf.Sin(turn) * r * sy), KnobMark);
			}
			for (int t = 0; t <= 4; t++)
			{
				float ang = Mathf.Lerp(Mathf.PI * 1.25f, -Mathf.PI * 0.25f, t / 4f);
				c.Set(Mathf.FloorToInt(k.x + Mathf.Cos(ang) * 8.4f * sx), Mathf.FloorToInt(k.y + Mathf.Sin(ang) * 8.4f * sy), ChromeDark);
			}

			// The gold CD badge, upright, at the far end, on a chrome line that follows the face.
			int bx = fx1 + Mathf.Max(2, (W - 1 - fx1 - 7) / 2);
			if (bx + 7 <= W - 1)
			{
				int by = Mathf.RoundToInt(Y(bx + 3.5f, 11f));
				Art.Text(c, "CD", bx, by, Badge);
				for (int x = bx; x < bx + 7; x++)
				{
					c.Set(x, Mathf.FloorToInt(Y(x + 0.5f, 11f)) - 2, ChromeDark);
				}
			}
			return c;
		}

		/// <summary>The slot's row in column x (flat row 23).</summary>
		internal int SlotRowAt(int x) => Row(x, Art.SlotRow + 0.5f);

		/// <summary>The slot's columns.</summary>
		internal void SlotColumns(out int x0, out int x1) => Columns(Art.SlotLeft, Art.SlotRight + 1, out x0, out x1);

		/// <summary>The display window's columns (inside the frame and its ring).</summary>
		internal void WindowColumns(out int x0, out int x1)
		{
			Columns(Frame.xMin, Frame.xMax, out int fx0, out int fx1);
			x0 = fx0 + 2;
			x1 = fx1 - 2;
		}

		/// <summary>The display window's rows in column x.</summary>
		internal void WindowRows(int x, out int y0, out int y1) => Span(x, Frame.yMin, Frame.yMax, 2, out y0, out y1);

		private static readonly Dictionary<Art.Part, string> Signs = new Dictionary<Art.Part, string>
		{
			// 3x3, rows from the top: power (a ring open at the top), previous, play / pause, next, eject.
			{ Art.Part.Power, "101101111" }, { Art.Part.Prev, "011111011" }, { Art.Part.Play, "101101101" },
			{ Art.Part.Next, "110111110" }, { Art.Part.Eject, "010111111" },
		};

		private void DrawKey(Art.Canvas c, Art.Part p, bool down, bool power)
		{
			RectInt r = Art.Place(p);
			Columns(r.x, r.xMax, out int x0, out int x1);
			int topMid = 0, lowMid = 0;
			for (int x = x0; x <= x1; x++)
			{
				Span(x, r.y, r.yMax, 0, out int y0, out int y1);
				c.Rect(x, y0, x, y1, KeyLow);
				if (down)
				{
					if (y1 - 1 > y0)
					{
						c.Rect(x, y0, x, y1 - 1, KeyPressed);
					}
				}
				else if (y1 > y0)
				{
					c.Rect(x, y0 + 1, x, y1, Key);
					c.Set(x, y1, KeyTop);
				}
				if (x == (x0 + x1) / 2)
				{
					topMid = y1;
					lowMid = y0;
				}
			}
			// The sign, upright, in the key's middle.
			string s = Signs[p];
			int sx = (x0 + x1) / 2 - 1, sy = (lowMid + topMid) / 2 - 1 - (down ? 1 : 0);
			for (int row = 0; row < 3; row++)
			{
				for (int col = 0; col < 3; col++)
				{
					if (s[row * 3 + col] == '1')
					{
						c.Set(sx + col, sy + 2 - row, KeySign);
					}
				}
			}
			if (p == Art.Part.Power)
			{
				Span(x1, r.y, r.yMax, 0, out int _, out int py1);
				c.Set(x1, py1, power ? LedOn : LedOff);
			}
		}

		/// <summary>A filled ellipse round (cx, cy).</summary>
		internal static void Oval(Art.Canvas c, float cx, float cy, float rx, float ry, Color32 colour)
		{
			rx = Mathf.Max(0.5f, rx);
			ry = Mathf.Max(0.5f, ry);
			for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
			{
				for (int x = Mathf.FloorToInt(cx - rx); x <= Mathf.CeilToInt(cx + rx); x++)
				{
					float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
					if (dx * dx + dy * dy <= 1f)
					{
						c.Set(x, y, colour);
					}
				}
			}
		}

		// ---------------------------------------------------------------------------------------------------------------------------
		// The display, at twice the cockpit's resolution, its own glowing sprite over the window.

		/// <summary>The display canvas's bottom left in face pixels (its columns start at the window's first column).</summary>
		internal void DisplayCorner(out int x, out int y)
		{
			WindowColumns(out int x0, out int x1);
			int lo = int.MaxValue;
			for (int i = x0; i <= x1; i++)
			{
				WindowRows(i, out int y0, out int _);
				lo = Mathf.Min(lo, y0);
			}
			x = x0;
			y = lo;
		}

		/// <summary>The display canvas's size, in display pixels.</summary>
		internal void DisplaySize(out int w, out int h)
		{
			WindowColumns(out int x0, out int x1);
			DisplayCorner(out int _, out int lo);
			int hi = int.MinValue;
			for (int i = x0; i <= x1; i++)
			{
				WindowRows(i, out int _, out int y1);
				hi = Mathf.Max(hi, y1);
			}
			w = (x1 - x0 + 1) * Art.LcdDetail;
			h = (hi - lo + 1) * Art.LcdDetail;
		}

		/// <summary>Letters that fit on a line of the display.</summary>
		internal int DisplayColumns
		{
			get
			{
				DisplaySize(out int w, out int _);
				return Mathf.Max(1, (w - 2) / Art.Advance);
			}
		}

		/// <summary>
		/// The display: the glass filling the window (its edges in whole cockpit pixels, meeting the frame), the two lines of upright
		/// letters along the face, the top one under the window's top edge, the bottom one over its bottom edge, each letter lifted a
		/// display pixel at a time; every unlit segment's ghost; off, only the ghosts.
		/// </summary>
		internal Art.Canvas Display(string top, string bottom, bool on)
		{
			int d = Art.LcdDetail;
			DisplaySize(out int w, out int h);
			DisplayCorner(out int cx, out int cy);
			Art.Canvas c = new Art.Canvas(w, h);
			int[] lo = new int[w], hi = new int[w];
			for (int i = 0; i < w; i++)
			{
				WindowRows(cx + i / d, out int y0, out int y1);
				lo[i] = (y0 - cy) * d;
				hi[i] = (y1 - cy) * d + d - 1;
				c.Rect(i, lo[i], i, hi[i], Art.LcdGlass);
			}
			int cols = DisplayColumns;
			string ghost = new string('█', cols);
			string a = Fit(top, cols), b = Fit(bottom, cols);
			// Each line's bottom row per display column, following the window's edges continuously (in display pixels): the top line under
			// the top edge, the bottom line over the bottom edge. The letters lean with the face, a display pixel per column: upright
			// letters climbed in stairs on a face this steep.
			int[] topLine = new int[w], lowLine = new int[w];
			for (int i = 0; i < w; i++)
			{
				float fx = cx + (i + 0.5f) / d;
				int t = Mathf.FloorToInt((Y(fx, Frame.yMax) - cy) * d) - d * 2 - 1 - Art.GlyphHeight;
				int l = Mathf.CeilToInt((Y(fx, Frame.yMin) - cy) * d) + d * 2 + 1;
				t = Mathf.Clamp(t, lo[i] + 1, hi[i] - Art.GlyphHeight);
				l = Mathf.Clamp(l, lo[i] + 1, hi[i] - Art.GlyphHeight);
				topLine[i] = Mathf.Max(t, l + Art.GlyphHeight + 1);
				lowLine[i] = l;
			}
			for (int n = 0; n < cols; n++)
			{
				int gx = 1 + n * Art.Advance;
				Leaning(c, '█', gx, topLine, Art.LcdGhost);
				Leaning(c, '█', gx, lowLine, Art.LcdGhost);
				if (on)
				{
					if (n < a.Length)
					{
						Leaning(c, a[n], gx, topLine, Art.LcdDim);
					}
					if (n < b.Length)
					{
						Leaning(c, b[n], gx, lowLine, Art.LcdLit);
					}
				}
			}
			// Nothing outside the window's own rows.
			for (int i = 0; i < w; i++)
			{
				for (int y = 0; y < h; y++)
				{
					if (y < lo[i] || y > hi[i])
					{
						c.Set(i, y, Art.Clear);
					}
				}
			}
			return c;
		}

		private static string Fit(string s, int n) => s == null ? "" : s.Length > n ? s.Substring(0, n) : s;

		/// <summary>A letter whose every column stands on its own line's row there (<paramref name="line"/>, per canvas column).</summary>
		private static void Leaning(Art.Canvas c, char ch, int x, int[] line, Color32 colour)
		{
			Art.Canvas g = new Art.Canvas(Art.GlyphWidth, Art.GlyphHeight);
			Art.Text(g, ch.ToString(), 0, 0, colour);
			for (int col = 0; col < Art.GlyphWidth; col++)
			{
				int cx = x + col;
				if (cx < 0 || cx >= line.Length)
				{
					continue;
				}
				for (int row = 0; row < Art.GlyphHeight; row++)
				{
					Color32 p = g.Get(col, row);
					if (p.a > 0)
					{
						c.Set(cx, line[cx] + row, p);
					}
				}
			}
		}

		// ---------------------------------------------------------------------------------------------------------------------------
		// A disc in the slot.

		/// <summary>A disc in the slot is seen edge-on as the face is: an oval as wide as fits the slot's length.</summary>
		internal int SlotDiscWidth
		{
			get
			{
				SlotColumns(out int x0, out int x1);
				return Mathf.Clamp(x1 - x0 - 1, 9, Art.DiscSize);
			}
		}

		/// <summary>The face column of the disc canvas's first column when it is in the slot (the canvas is 31 wide, the oval centred).</summary>
		internal int SlotDiscLeft
		{
			get
			{
				SlotColumns(out int x0, out int x1);
				return (x0 + x1) / 2 - Art.DiscSize / 2;
			}
		}

		/// <summary>Per column of the disc canvas with its bottom on <paramref name="bottom"/>: the rows inside the slot, hidden.</summary>
		internal int[] SlotClip(int bottom)
		{
			int[] clip = new int[Art.DiscSize];
			int left = SlotDiscLeft;
			SlotColumns(out int x0, out int x1);
			for (int j = 0; j < clip.Length; j++)
			{
				int x = Mathf.Clamp(left + j, x0, x1);
				clip[j] = SlotRowAt(x) + 1 - bottom;
			}
			return clip;
		}

		/// <summary>The disc's bottom row when it is all the way in (none of it shows) and when it sticks out after an eject.</summary>
		internal int DiscInRow
		{
			get
			{
				SlotColumns(out int x0, out int x1);
				int lo = int.MaxValue;
				for (int x = x0; x <= x1; x++)
				{
					lo = Mathf.Min(lo, SlotRowAt(x));
				}
				return lo + 1 - Art.DiscSize;
			}
		}

		internal int DiscOutRow => SlotRowAt(SlotDiscLeft + Art.DiscSize / 2) + 1 - 20;
	}
}

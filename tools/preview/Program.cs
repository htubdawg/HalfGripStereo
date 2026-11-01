using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HalfGripStereo
{
	/// <summary>
	/// Draws the stereo's pictures with the mod's own Art.cs, outside the game:
	///   dotnet run -c Release --project tools/preview [-p:Refs=&lt;HalfGripRefs&gt;] -- &lt;out dir&gt;
	/// writes, at one pixel per cockpit pixel (for laying over a screenshot) and 4x:
	///   unit.png / unit-4x.png    the head unit with its display (the display at its own double resolution in the 4x one)
	///   visor.png / visor-4x.png  the CD wallet with seven discs, the second in the stereo
	///   icon.png                  the mod's picture, 128x128 (copy to Stereo/icon.png)
	/// </summary>
	internal static class Program
	{
		private static int Main(string[] args)
		{
			string dir = args.Length > 0 ? args[0] : "preview-out";
			Directory.CreateDirectory(dir);

			Art.Canvas unit = Unit(Art.Part.Play, "CD2 ▶ 1:23", "NEON", 1);
			Png.Save(Path.Combine(dir, "unit.png"), Half(unit), Art.UnitWidth, Art.UnitHeight, 1);
			Png.Save(Path.Combine(dir, "unit-4x.png"), unit.Px, unit.W, unit.H, 2);

			// As in the cockpit: square on, its sides leaning toward the vanishing point (as in the owner's 0.2.1 screenshot, in cockpit
			// pixels from the face's bottom left; the optional second and third arguments) by Stereo.Lean (the fourth, default 3), with
			// and without a disc half out of the slot.
			float vx = args.Length > 2 ? float.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 201f;
			float vy = args.Length > 2 ? float.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture) : 137f;
			float lean = args.Length > 3 ? float.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture) : 3f;
			Face face = new Face(new Vector2(vx, vy), lean);
			Art.Canvas onFace = OnFace(face, "CD2 ▶ 1:23", "NEON 2", false);
			Png.Save(Path.Combine(dir, "unit-face-4x.png"), onFace.Px, onFace.W, onFace.H, 2);
			Art.Canvas withDisc = OnFace(face, "CD2 ▶ 1:23", "NEON 2", true);
			Png.Save(Path.Combine(dir, "unit-face-disc-4x.png"), withDisc.Px, withDisc.W, withDisc.H, 2);
			Art.Canvas flatFace = OnFace(new Face(new Vector2(vx, vy), 0f), "CD2 ▶ 1:23", "NEON 2", false);
			Png.Save(Path.Combine(dir, "unit-flat-4x.png"), flatFace.Px, flatFace.W, flatFace.H, 2);
			Art.Canvas loose = Art.LooseDisc(Art.LabelFor("MIX"), "MY", null);
			Png.Save(Path.Combine(dir, "disc-4x.png"), loose.Px, loose.W, loose.H, 4);

			string[] titles = { "FOREST", "NEON", "NEON 2", "FACTORY", "STADION", "WINTER", "MIX" };
			List<Art.DiscLook> looks = new List<Art.DiscLook>();
			for (int i = 0; i < titles.Length; i++)
			{
				looks.Add(new Art.DiscLook { Label = Art.LabelFor(titles[i]), Mark = titles[i] == "MIX" ? "MY" : null, Out = i == 1 });
			}
			Art.Canvas visor = Art.VisorWallet(looks);
			Png.Save(Path.Combine(dir, "visor.png"), visor.Px, visor.W, visor.H, 1);
			Png.Save(Path.Combine(dir, "visor-4x.png"), visor.Px, visor.W, visor.H, 4);

			// The mod's picture: the head unit with a disc going in, on 64x64, shown at 128.
			Art.Canvas icon = new Art.Canvas(64, 64);
			Art.DrawDisc(icon, 31.5f, 40f, 15.5f, Art.LabelFor("NEON"));
			Art.Canvas small = Unit(null, "HALF GRIP", "STEREO", 1);
			Color32[] half = Half(small);
			Art.Canvas u = new Art.Canvas(Art.UnitWidth, Art.UnitHeight);
			Array.Copy(half, u.Px, half.Length);
			// Hide the disc's lower half inside the stereo: the stereo is drawn over it from the slot down.
			icon.Paste(u, 1, 8);
			Png.Save(Path.Combine(dir, "icon.png"), icon.Px, 64, 64, 2);
			Console.WriteLine("written to " + Path.GetFullPath(dir));
			return 0;
		}

		/// <summary>The head unit at twice its size (the display's resolution), with the display laid in its window.</summary>
		private static Art.Canvas Unit(Art.Part? pressed, string top, string bottom, float knob)
		{
			Art.Canvas casing = Art.HeadUnit(pressed, true, 0.7f);
			int d = Art.LcdDetail;
			Art.Canvas big = new Art.Canvas(casing.W * d, casing.H * d);
			for (int y = 0; y < big.H; y++)
			{
				for (int x = 0; x < big.W; x++)
				{
					big.Set(x, y, casing.Get(x / d, y / d));
				}
			}
			Color32[] lcd = new Color32[Art.LcdWidth * Art.LcdHeight];
			Art.Lcd(lcd, top, bottom, true);
			for (int y = 0; y < Art.LcdHeight; y++)
			{
				for (int x = 0; x < Art.LcdWidth; x++)
				{
					big.Set(Art.LcdWindow.x * d + x, Art.LcdWindow.y * d + y, lcd[y * Art.LcdWidth + x]);
				}
			}
			return big;
		}

		/// <summary>The head unit on its face as StereoDash builds it, at the display's resolution: casing doubled, display laid on.</summary>
		private static Art.Canvas OnFace(Face face, string top, string bottom, bool disc)
		{
			int d = Art.LcdDetail, pad = 4, above = disc ? 24 : 0;
			Art.Canvas casing = face.Casing(null, true, 0.7f);
			Art.Canvas big = new Art.Canvas((casing.W + 2 * pad) * d, (casing.H + 2 * pad + above) * d);
			void Blit(Art.Canvas src, int x0, int y0, int scale)
			{
				for (int y = 0; y < src.H * scale; y++)
				{
					for (int x = 0; x < src.W * scale; x++)
					{
						Color32 p = src.Get(x / scale, y / scale);
						if (p.a > 0)
						{
							big.Set(x0 + x, y0 + y, p);
						}
					}
				}
			}
			Blit(casing, pad * d, pad * d, d);
			face.DisplayCorner(out int cx, out int cy);
			Blit(face.Display(top, bottom, true), (pad + cx) * d, (pad + cy) * d, 1);
			if (disc)
			{
				int b = face.DiscOutRow;
				Blit(Art.LooseDisc(Art.LabelFor("NEON"), "2", face.SlotClip(b), face.SlotDiscWidth), (pad + face.SlotDiscLeft) * d, (pad + b) * d, d);
			}
			return big;
		}

		/// <summary>Back down to one pixel per cockpit pixel (the display's pixels averaged away: only for the overlay).</summary>
		private static Color32[] Half(Art.Canvas big)
		{
			int d = Art.LcdDetail, w = big.W / d, h = big.H / d;
			Color32[] px = new Color32[w * h];
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					px[y * w + x] = big.Get(x * d + 1, y * d + 1);
				}
			}
			return px;
		}
	}
}

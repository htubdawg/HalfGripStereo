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

			// As in the cockpit: slanted along the dash, with a disc half out of the slot.
			float slant = args.Length > 1 ? float.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 0.33f;
			int[] lift = Art.Lifts(Art.UnitWidth, slant);
			int[] lift2 = new int[unit.W];
			for (int x = 0; x < unit.W; x++)
			{
				lift2[x] = 2 * lift[x / 2];
			}
			Art.Canvas slanted2 = Art.Lift(unit, lift2);
			Art.Canvas slanted = new Art.Canvas(slanted2.W / 2, slanted2.H / 2);
			Art.Canvas dash = new Art.Canvas(slanted.W + 8, slanted.H + 24);
			int left = Art.SlotDiscLeft, bottom = 13 + lift[left + Art.DiscSize / 2];
			int[] clip = new int[Art.DiscSize];
			for (int x = 0; x < clip.Length; x++)
			{
				clip[x] = Art.SlotRow + 1 + lift[left + x] - bottom;
			}
			// At the display's resolution, the rest doubled.
			Art.Canvas big = new Art.Canvas(dash.W * 2, dash.H * 2);
			big.Paste(slanted2, 8, 8);
			Art.Canvas disc = Art.LooseDisc(Art.LabelFor("NEON"), "2", clip);
			for (int y = 0; y < disc.H * 2; y++)
			{
				for (int x = 0; x < disc.W * 2; x++)
				{
					Color32 p = disc.Get(x / 2, y / 2);
					if (p.a > 0)
					{
						big.Set(8 + 2 * left + x, 8 + 2 * bottom + y, p);
					}
				}
			}
			Png.Save(Path.Combine(dir, "unit-slanted-4x.png"), big.Px, big.W, big.H, 2);
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

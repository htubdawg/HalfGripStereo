using System;
using System.Collections.Generic;
using UnityEngine;

namespace HalfGripStereo
{
	/// <summary>
	/// The stereo in the cockpit, put on the cockpit's root (CarInterior) by <see cref="Fitter"/>. It builds two things there:
	///
	/// - the **head unit**, standing just left of the steering wheel (sized from the wheel's own sprite, so it follows the wheel), with
	///   its display as a glowing sprite of its own and a click target on each knob and key;
	/// - the **CD wallet on the sun visor**, at the top left of the view (placed through the cockpit camera, at the wheel's depth), one
	///   pocket per disc, each a click target that puts that disc in.
	///
	/// The cockpit is flat sprites drawn by its own camera; these are the same, at the cockpit's own pixel size (the middle value over
	/// its sprites, as the MiniMap's GPS measures it), in a copy of the wheel's lit material, so they are lit as the cockpit is. The
	/// display is unlit, so it glows. Click targets are the game's own (InteriorSquareCollider and Clickable), so the cockpit hand points
	/// at them and clicks them like any control; they sit at the lowest hover priority, so they never take the hand from a real control.
	/// Changing the Stereo settings in the config file rebuilds it, so its place can be tuned with the game running.
	/// </summary>
	internal sealed class StereoDash : MonoBehaviour
	{
		/// <summary>World distance the display and keys stand in front of the casing, and the click targets in front of them (as the GPS).</summary>
		private const float Lift = 0.02f;
		private const float TouchLift = 0.1f;
		/// <summary>Cockpit units between the head unit's right edge and the wheel's rim.</summary>
		private const float WheelGap = 0.15f;
		private const float FallbackTexel = 0.05f;
		/// <summary>How long a key shows pushed in.</summary>
		private const float PressTime = 0.15f;
		/// <summary>Seconds per letter the song's name scrolls along the display.</summary>
		private const float ScrollStep = 0.3f;

		private Deck deck;
		private Transform interior;
		private float texel = FallbackTexel;
		private Material lit;
		private Material unlit;
		private int sortingLayer;
		private int sortingOrder;
		private float toward = -1f;
		private int layer;
		private bool failed;
		private string built;
		private int builtDiscs = -1;

		private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
		private GameObject unit;
		private GameObject visor;
		private Texture2D unitTexture;
		private Texture2D visorTexture;
		private Texture2D lcdTexture;
		private Color32[] lcdPixels;
		private string lcdShown;
		private Art.Part? pressed;
		private float pressedAt;
		private string unitShown;
		private string visorShown;

		private void Start()
		{
			try
			{
				deck = GetComponent<Deck>();
				interior = FindInterior();
				if (interior == null)
				{
					throw new Exception("not under the cockpit (no CarInterior above it)");
				}
				// On the cockpit's root, in its units.
				transform.SetParent(interior, false);
				transform.localPosition = Vector3.zero;
				transform.localRotation = Quaternion.identity;
				transform.localScale = Vector3.one;
				layer = interior.gameObject.layer;
				gameObject.layer = layer;
				texel = MeasureDash();
				Build();
			}
			catch (Exception e)
			{
				failed = true;
				Mod.Log.LogWarning("Stereo: could not put itself in the cockpit: " + e);
			}
		}

		private Transform FindInterior()
		{
			for (Transform t = transform; t != null; t = t.parent)
			{
				if (t.name == "CarInterior" || t.CompareTag("carInterior"))
				{
					return t;
				}
			}
			NSteeringWheel wheel = FindFirstObjectByType<NSteeringWheel>();
			return wheel != null ? wheel.transform.parent : null;
		}

		/// <summary>The settings the stereo was last built from.</summary>
		private static string Settings() => string.Join("|", Mod.UnitX.Value, Mod.UnitY.Value, Mod.VisorX.Value, Mod.VisorY.Value, Mod.ShowVisor.Value, Mod.LitParts.Value);

		/// <summary>
		/// The cockpit's pixel size in its own units: the middle value over every sprite showing in it (one sprite is not enough: some are
		/// drawn much finer than the rest).
		/// </summary>
		private float MeasureDash()
		{
			List<float> sizes = new List<float>();
			float own = Mathf.Max(1e-6f, Mathf.Abs(interior.lossyScale.x));
			foreach (SpriteRenderer r in interior.GetComponentsInChildren<SpriteRenderer>(false))
			{
				if (r.sprite == null || r.sprite.pixelsPerUnit <= 0f || r.transform.IsChildOf(transform))
				{
					continue;
				}
				float t = Mathf.Abs(r.transform.lossyScale.x) / r.sprite.pixelsPerUnit / own;
				if (t > 0f)
				{
					sizes.Add(t);
				}
			}
			if (sizes.Count < 5)
			{
				Mod.Log.LogWarning("Stereo: only " + sizes.Count + " sprites in the cockpit to measure its pixels by; using " + FallbackTexel);
				return FallbackTexel;
			}
			sizes.Sort();
			float middle = sizes[sizes.Count / 2];
			Mod.Info("Stereo: cockpit pixel " + middle.ToString("0.0000") + " units, the middle of " + sizes.Count + " sprites");
			return middle < 0.005f || middle > 0.5f ? FallbackTexel : middle;
		}

		/// <summary>The steering wheel's biggest sprite: what the head unit is placed by, and whose material and sorting it borrows.</summary>
		private SpriteRenderer Wheel()
		{
			NSteeringWheel wheel = interior.GetComponentInChildren<NSteeringWheel>(true);
			if (wheel == null)
			{
				return null;
			}
			SpriteRenderer best = null;
			float area = 0f;
			foreach (SpriteRenderer r in wheel.GetComponentsInChildren<SpriteRenderer>(true))
			{
				if (r.sprite == null)
				{
					continue;
				}
				Vector3 s = r.transform.lossyScale;
				float a = r.sprite.bounds.size.x * r.sprite.bounds.size.y * Mathf.Abs(s.x * s.y);
				if (a > area)
				{
					area = a;
					best = r;
				}
			}
			return best;
		}

		private void Build()
		{
			Clear();
			built = Settings();
			builtDiscs = Discs.Version;
			SpriteRenderer wheel = Wheel();
			Camera cam = CockpitCamera();
			// Where the wheel is, in the cockpit's units: its middle, its radius (from the sprite itself, so the wheel's turn does not
			// matter) and its depth, the plane both parts stand in.
			Vector3 centre;
			float radius;
			if (wheel != null)
			{
				centre = interior.InverseTransformPoint(wheel.transform.TransformPoint(wheel.sprite.bounds.center));
				Vector3 ext = wheel.sprite.bounds.extents;
				radius = Mathf.Max(ext.x, ext.y) * Mathf.Abs(wheel.transform.lossyScale.x) / Mathf.Max(1e-6f, Mathf.Abs(interior.lossyScale.x));
				sortingLayer = wheel.sortingLayerID;
				sortingOrder = wheel.sortingOrder;
			}
			else
			{
				// The game's cockpit in 0.418: the wheel at (-1.8, -0.36, 1.14), about 2.2 units round.
				Mod.Log.LogWarning("Stereo: no steering wheel in the cockpit; placing the stereo where the game's wheel would be");
				centre = new Vector3(-1.8f, -0.36f, 1.14f);
				radius = 2.2f;
			}
			Materials(wheel);
			toward = Toward(cam);

			// The head unit: its right edge a little clear of the rim, its middle a little above the wheel's.
			float uw = Art.UnitWidth * texel, uh = Art.UnitHeight * texel;
			Vector3 unitAt = new Vector3(centre.x - radius - WheelGap - uw * 0.5f + Mod.UnitX.Value, centre.y + 0.5f + Mod.UnitY.Value, centre.z);
			unit = Part("StereoUnit", transform, unitAt);
			unitTexture = MakeTexture(Art.UnitWidth, Art.UnitHeight, "StereoUnit");
			Sprite(unit, "Casing", unitTexture, Vector3.zero, 1f / texel, lit, 0);
			lcdTexture = MakeTexture(Art.LcdWidth, Art.LcdHeight, "StereoLcd");
			lcdPixels = new Color32[Art.LcdWidth * Art.LcdHeight];
			lcdShown = null;
			Vector3 lcdAt = Local(Art.LcdWindow.center, Art.UnitWidth, Art.UnitHeight) + new Vector3(0f, 0f, toward * Lift);
			Sprite(unit, "Display", lcdTexture, lcdAt, Art.LcdDetail / texel, unlit, 1);
			foreach (Art.Part p in Art.Parts)
			{
				RectInt r = Art.Place(p);
				Art.Part part = p;
				Touch(unit, p.ToString(), r, Art.UnitWidth, Art.UnitHeight, () => Press(part, false), () => Press(part, true));
			}
			unitShown = null;
			DrawUnit();

			if (Mod.ShowVisor.Value)
			{
				BuildVisor(cam, centre);
			}
			Mod.Info("Stereo: head unit at " + unitAt.ToString("0.00") + " (" + uw.ToString("0.00") + " x " + uh.ToString("0.00") + " units), wheel at " + centre.ToString("0.00") + " radius " + radius.ToString("0.00") + ", sorting " + sortingLayer + "/" + sortingOrder + ", lit '" + (lit != null ? lit.shader.name : "none") + "', display '" + (unlit != null ? unlit.shader.name : "none") + "', camera '" + (cam != null ? cam.name : "none") + "' toward " + toward);
		}

		private void BuildVisor(Camera cam, Vector3 wheelCentre)
		{
			int n = Discs.All.Count;
			int vw = Art.VisorWidth(n), vh = Art.VisorHeight;
			// The visor's middle where the camera sees the asked-for point of the screen, in the wheel's plane.
			Vector3 at = new Vector3(wheelCentre.x - 2.6f, wheelCentre.y + 8.9f, wheelCentre.z);
			if (cam != null)
			{
				Ray ray = cam.ViewportPointToRay(new Vector3(Mod.VisorX.Value, Mod.VisorY.Value, 0f));
				Vector3 o = interior.InverseTransformPoint(ray.origin);
				Vector3 d = interior.InverseTransformDirection(ray.direction);
				if (Mathf.Abs(d.z) > 1e-4f)
				{
					float k = (wheelCentre.z - o.z) / d.z;
					if (k > 0f)
					{
						at = o + d * k;
					}
				}
			}
			visor = Part("StereoVisor", transform, at);
			visorTexture = MakeTexture(vw, vh, "StereoVisor");
			Sprite(visor, "Wallet", visorTexture, Vector3.zero, 1f / texel, lit, 0);
			for (int i = 0; i < n; i++)
			{
				int index = i;
				Touch(visor, "Disc" + (i + 1), Art.PocketPlace(i), vw, vh, () => Pocket(index), () => Pocket(index));
			}
			visorShown = null;
			DrawVisor();
			Mod.Info("Stereo: CD wallet with " + n + " disc(s) at " + at.ToString("0.00") + " (" + (vw * texel).ToString("0.00") + " units wide)");
		}

		/// <summary>The cockpit's own camera (the hand's), or the one named InteriorCamera.</summary>
		private Camera CockpitCamera()
		{
			Grabber hand = FindFirstObjectByType<Grabber>();
			if (hand != null && hand.viewCamera != null)
			{
				return hand.viewCamera;
			}
			foreach (Camera c in interior.GetComponentsInChildren<Camera>(true))
			{
				return c;
			}
			return null;
		}

		/// <summary>Which way along the cockpit's z its camera is: toward it is in front. The game's cockpit has it at -z.</summary>
		private float Toward(Camera cam)
		{
			if (cam == null)
			{
				return -1f;
			}
			return interior.InverseTransformPoint(cam.transform.position).z > 0f ? 1f : -1f;
		}

		/// <summary>
		/// The casing's material: a copy of the wheel's lit one with our texture set (the cockpit's lit shader reads the material's own
		/// texture, not the sprite's, so each cockpit sprite has its own copy). The display's: an unlit sprite material from the cockpit,
		/// or the game's unlit sprite shader.
		/// </summary>
		private void Materials(SpriteRenderer wheel)
		{
			Material basis = wheel != null ? wheel.sharedMaterial : null;
			lit = Mod.LitParts.Value ? basis : null;
			unlit = null;
			foreach (SpriteRenderer r in interior.GetComponentsInChildren<SpriteRenderer>(true))
			{
				Material m = r.sharedMaterial;
				if (m != null && m.shader != null && m.shader.name.IndexOf("Unlit", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					unlit = m;
					break;
				}
			}
			if (unlit == null)
			{
				Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
				if (s != null)
				{
					unlit = Own(new Material(s) { name = "StereoUnlit" });
				}
			}
			if (lit == null)
			{
				lit = unlit;
			}
		}

		private T Own<T>(T thing) where T : UnityEngine.Object
		{
			thing.hideFlags = HideFlags.HideAndDontSave;
			owned.Add(thing);
			return thing;
		}

		private Texture2D MakeTexture(int w, int h, string name)
		{
			Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
			return Own(tex);
		}

		private GameObject Part(string name, Transform parent, Vector3 local)
		{
			GameObject go = new GameObject(name);
			go.layer = layer;
			go.transform.SetParent(parent, false);
			go.transform.localPosition = local;
			owned.Add(go);
			return go;
		}

		/// <summary>A sprite of the whole texture, centred on its object; a lit material gets its own copy holding the texture.</summary>
		private void Sprite(GameObject on, string name, Texture2D tex, Vector3 local, float ppu, Material material, int above)
		{
			GameObject go = new GameObject(name);
			go.layer = layer;
			go.transform.SetParent(on.transform, false);
			go.transform.localPosition = local;
			SpriteRenderer r = go.AddComponent<SpriteRenderer>();
			r.sprite = Own(UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect));
			if (material != null)
			{
				if (material != unlit)
				{
					Material copy = Own(new Material(material) { name = name + "Lit" });
					foreach (string p in new[] { "_BaseMap", "_MainTex" })
					{
						if (copy.HasProperty(p))
						{
							copy.SetTexture(p, tex);
							copy.SetTextureScale(p, Vector2.one);
							copy.SetTextureOffset(p, Vector2.zero);
						}
					}
					foreach (string p in new[] { "_BaseColor", "_Color" })
					{
						if (copy.HasProperty(p))
						{
							copy.SetColor(p, Color.white);
						}
					}
					material = copy;
				}
				r.sharedMaterial = material;
			}
			r.sortingLayerID = sortingLayer;
			r.sortingOrder = sortingOrder + above;
		}

		/// <summary>From a point in a picture's pixels (from its bottom left) to its sprite's local units (the sprite is centred).</summary>
		private Vector3 Local(Vector2 px, int w, int h) => new Vector3((px.x - w * 0.5f) * texel, (px.y - h * 0.5f) * texel, 0f);

		/// <summary>A click target over a box of a picture's pixels, in front of it.</summary>
		private void Touch(GameObject on, string name, RectInt box, int w, int h, Action left, Action right)
		{
			GameObject go = new GameObject("Touch" + name);
			go.layer = layer;
			go.transform.SetParent(on.transform, false);
			go.transform.localPosition = Local(box.center, w, h) + new Vector3(0f, 0f, toward * TouchLift);
			InteriorSquareCollider target = go.AddComponent<InteriorSquareCollider>();
			target.width = box.width * texel;
			target.height = box.height * texel;
			target.hitBoxExpand = 0f;
			target.localNormalPlane = Vector3.forward;
			target.sortingLayer = StereoButton.SortingLayer;
			StereoButton button = go.AddComponent<StereoButton>();
			button.attachedCollider = target;
			button.Left = left;
			button.Right = right;
		}

		// ---------------------------------------------------------------------------------------------------------------------------
		// The hand.

		/// <summary>
		/// A key or the knob pressed. The knob: left click louder, right click quieter. The keys do the same on either click: power,
		/// previous, play / pause, next, eject.
		/// </summary>
		private void Press(Art.Part part, bool right)
		{
			if (deck == null)
			{
				return;
			}
			pressed = part;
			pressedAt = Time.unscaledTime;
			Sounds.Click(gameObject);
			switch (part)
			{
				case Art.Part.Knob: deck.Volume(right ? -1 : 1); break;
				case Art.Part.Power: deck.TogglePower(); break;
				case Art.Part.Prev: deck.Skip(-1); break;
				case Art.Part.Next: deck.Skip(1); break;
				case Art.Part.Play: deck.TogglePause(); break;
				case Art.Part.Eject:
					if (deck.DiscIndex >= 0)
					{
						Sounds.Disc(gameObject);
					}
					deck.Eject();
					break;
			}
		}

		/// <summary>A disc in the wallet clicked: in it goes (clicking the one already in does nothing).</summary>
		private void Pocket(int i)
		{
			if (deck == null || i == deck.DiscIndex)
			{
				return;
			}
			Sounds.Disc(gameObject);
			deck.Insert(i);
		}

		// ---------------------------------------------------------------------------------------------------------------------------
		// Every frame.

		private void Update()
		{
			if (failed || unit == null)
			{
				return;
			}
			try
			{
				Mod.WatchConfig();
				if (built != Settings() || (builtDiscs != Discs.Version && Mod.ShowVisor.Value))
				{
					Build();
				}
				if (pressed != null && Time.unscaledTime - pressedAt > PressTime)
				{
					pressed = null;
				}
				DrawUnit();
				DrawVisor();
				DrawLcd();
			}
			catch (Exception e)
			{
				failed = true;
				Mod.Log.LogWarning("Stereo: stopped after an error: " + e);
			}
		}

		private void DrawUnit()
		{
			bool power = deck == null || deck.Power;
			float turn = deck != null ? deck.VolumeTurn : 0.7f;
			string key = pressed + "|" + power + "|" + turn;
			if (key == unitShown)
			{
				return;
			}
			unitShown = key;
			unitTexture.SetPixels32(Art.HeadUnit(pressed, power, turn).Px);
			unitTexture.Apply(false);
		}

		private void DrawVisor()
		{
			if (visorTexture == null)
			{
				return;
			}
			int inside = deck != null ? deck.DiscIndex : -1;
			string key = inside + "|" + Discs.All.Count;
			if (key == visorShown)
			{
				return;
			}
			visorShown = key;
			List<Art.DiscLook> looks = new List<Art.DiscLook>();
			for (int i = 0; i < Discs.All.Count; i++)
			{
				Disc d = Discs.All[i];
				looks.Add(new Art.DiscLook { Label = Art.LabelFor(d.Mix ? "MIX" : d.Title), Mark = d.Mix ? "MY" : null, Out = i == inside });
			}
			visorTexture.SetPixels32(Art.VisorWallet(looks).Px);
			visorTexture.Apply(false);
		}

		private void DrawLcd()
		{
			string top = "", bottom = "";
			bool on = deck != null && deck.Power;
			if (on)
			{
				deck.Lines(out top, out bottom);
				bottom = Scroll(bottom ?? "");
			}
			string key = on + "|" + top + "|" + bottom;
			if (key == lcdShown)
			{
				return;
			}
			lcdShown = key;
			Art.Lcd(lcdPixels, top, bottom, on);
			lcdTexture.SetPixels32(lcdPixels);
			lcdTexture.Apply(false);
		}

		/// <summary>A name too long for the display runs along it, with a gap before it comes round again.</summary>
		private static string Scroll(string text)
		{
			int cols = Art.LcdColumns;
			if (text.Length <= cols)
			{
				return text;
			}
			string loop = text + "    ";
			int at = Mathf.FloorToInt(Time.unscaledTime / ScrollStep) % loop.Length;
			string twice = loop + loop;
			return twice.Substring(at, cols);
		}

		private void Clear()
		{
			foreach (UnityEngine.Object o in owned)
			{
				if (o != null)
				{
					Destroy(o);
				}
			}
			owned.Clear();
			unit = visor = null;
			unitTexture = visorTexture = lcdTexture = null;
		}

		private void OnDestroy() => Clear();
	}

	/// <summary>
	/// A key, the knob or a disc as the cockpit hand sees it: the game's own click target (the hand clicks the Clickable over the interior
	/// collider it hovers, left and right), as the MiniMap's GPS does; HalfGripTools has no interaction API of its own.
	/// </summary>
	internal sealed class StereoButton : Clickable
	{
		/// <summary>
		/// The hand hovers the highest sortingLayer under it, then the nearest. The game's controls use -1 to 2; the stereo is far below
		/// (with the GPS, at -100), so it only gets the hand when no control wants it.
		/// </summary>
		internal const int SortingLayer = -100;

		internal Action Left;
		internal Action Right;

		protected override void OnClick() => Left?.Invoke();

		protected override void OnRightClick() => Right?.Invoke();
	}
}

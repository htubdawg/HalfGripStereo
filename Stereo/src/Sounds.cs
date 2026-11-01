using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HalfGripStereo
{
	/// <summary>
	/// The stereo's clicks, borrowed from the game's own sounds (found by name among the loaded clips at each scene load until found, and
	/// kept): the hand's grab click for a key, the gun's reload, higher, for a disc going in or out. Not found, it works in silence.
	/// </summary>
	internal static class Sounds
	{
		private const string KeyName = "Grab1";
		private const string DiscName = "Reload";

		private static AudioClip key;
		private static AudioClip disc;

		internal static void OnSceneLoaded(Scene scene) => Find();

		internal static void Find()
		{
			if (key != null && disc != null)
			{
				return;
			}
			try
			{
				foreach (AudioClip c in Resources.FindObjectsOfTypeAll<AudioClip>())
				{
					if (c == null)
					{
						continue;
					}
					if (key == null && c.name == KeyName)
					{
						key = c;
						Mod.Info("Stereo: kept the sound '" + KeyName + "' for the keys");
					}
					else if (disc == null && c.name == DiscName)
					{
						disc = c;
						Mod.Info("Stereo: kept the sound '" + DiscName + "' for the discs");
					}
				}
			}
			catch (Exception e)
			{
				Mod.Log.LogWarning("Stereo: could not look for its sounds: " + e.Message);
			}
		}

		internal static void Click(GameObject on) => Play(key, on, 0.5f, 1.5f);

		internal static void Disc(GameObject on) => Play(disc, on, 0.45f, 1.35f);

		/// <summary>From <paramref name="on"/>, so it goes to the mixer group of the cockpit it is in.</summary>
		private static void Play(AudioClip clip, GameObject on, float volume, float pitch)
		{
			if (clip == null)
			{
				return;
			}
			AudioSource s = SoundController.Create2DAudioSource(clip, on, volume, pitch * (1f + UnityEngine.Random.Range(-0.04f, 0.04f)));
			if (s != null)
			{
				s.Play();
				UnityEngine.Object.Destroy(s.gameObject, clip.length / s.pitch + 0.1f);
			}
		}
	}
}

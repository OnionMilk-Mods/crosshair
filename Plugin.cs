using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace OnionMilk_crosshair
{
	[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin
	{
		private static Plugin instance;
		private readonly Harmony _harmony = new Harmony("OnionMilk.Fourth");

		public static ConfigEntry<bool> cfgEnabled;
		public static ConfigEntry<string> cfgImage;
		public static ConfigEntry<float> cfgWidth;
		public static ConfigEntry<float> cfgHeight;

		public static ConfigEntry<int> cfgR;
		public static ConfigEntry<int> cfgG;
		public static ConfigEntry<int> cfgB;
		public static ConfigEntry<int> cfgA;

		public static Color32 color;
		public static Sprite icon;

		public static void ListChildren(Transform t, string prefix = "")
		{
			foreach(Transform child in t)
			{
				Log(prefix + child.name);
				if(t.childCount > 0)
					ListChildren(child, prefix + "-");
			}
		}

		public static void Log(string msg)
		{
			instance.Logger.LogInfo($"[{PluginInfo.PLUGIN_GUID}] {msg}");
		}
		private void Awake()
		{
			instance = this;

			cfgR = Config.Bind(
				"Color",
				"r",
				255,
				"Red value of crosshair (0-255)"
			);
			cfgG = Config.Bind(
				"Color",
				"g",
				255,
				"Green value of crosshair (0-255)"
			);
			cfgB = Config.Bind(
				"Color",
				"b",
				255,
				"Blue value of crosshair (0-255)"
			);
			cfgA = Config.Bind(
				"Color",
				"a",
				255,
				"Alpha value of crosshair (0-255)"
			);
			
			cfgWidth = Config.Bind(
				"Size",
				"width",
				10f,
				"Width of crosshair"
			);
			cfgHeight = Config.Bind(
				"Size",
				"height",
				10f,
				"Height of crosshair"
			);
			
			cfgEnabled = Config.Bind(
				"General",
				"enabled",
				true,
				"Is crosshair visible?"
			);
			cfgImage = Config.Bind(
				"General",
				"image",
				"crosshair.png",
				"Crosshair image, leave empty for square"
			);

			Log($"Plugin is loaded!");

			if(!cfgEnabled.Value)
				return;

			if(cfgImage.Value != string.Empty)
			{
				string imgPath = Path.Combine(GetPath, cfgImage.Value);
				LoadIcon(imgPath);
			}

			color = new Color32(
				(byte)cfgR.Value,
				(byte)cfgG.Value,
				(byte)cfgB.Value,
				(byte)cfgA.Value
			);

			_harmony.PatchAll();
		}

		public static string GetPath
		{
			get
			{
				if(getPath == null)
				{
					var cd = Assembly.GetExecutingAssembly().CodeBase;
					UriBuilder uri = new UriBuilder(cd);
					string path = Uri.UnescapeDataString(uri.Path);
					getPath = Path.GetDirectoryName(path);
				}
				return getPath;
			}
		}
		private static string getPath = null;

		internal static void LoadIcon(string filePath)
		{
			if(!File.Exists(filePath))
			{
				Log($"Crosshair image not found: {filePath}, using default");
				return;
			}
			try
			{
				var data = File.ReadAllBytes(filePath);
				var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
				tex.LoadImage(data);
				tex.filterMode = FilterMode.Point;
				icon = Sprite.Create(
					tex,
					new Rect(0.0f, 0.0f, tex.width, tex.height),
					new Vector2(0.5f, 0.5f),
					Mathf.Min(cfgWidth.Value, cfgHeight.Value)
				);
				Log($"Image {filePath} loaded as crosshair!");
			}
			catch(Exception ex)
			{
				Log($"Crosshair image is not readable PNG: {filePath}, using default");
			}
		}
	}
}

namespace HealthMetrics.Patches
{
	[HarmonyPatch(typeof(HUDManager))]
	internal class HealthHUDPatches
	{
		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		private static void Start(ref HUDManager __instance)
		{
			GameObject go = new GameObject("Crosshair");
			var rct = go.AddComponent<RectTransform>();
			UnityEngine.UI.Image img = go.AddComponent<UnityEngine.UI.Image>();
			go.transform.SetParent(__instance.HUDContainer.transform, false);
			go.transform.SetAsLastSibling();

			img.sprite = OnionMilk_crosshair.Plugin.icon;
			img.color = OnionMilk_crosshair.Plugin.color;
			rct.sizeDelta = new Vector2(
				OnionMilk_crosshair.Plugin.cfgWidth.Value,
				OnionMilk_crosshair.Plugin.cfgHeight.Value
			);
		}
	}

	[HarmonyPatch(typeof(MenuManager))]
	public static class MenuManagerOverridePatch
	{
		private static UnityEngine.UI.Image menuButtonCrosshair;

		private static AudioSource aud = null;
		private static AudioClip sfx = null;

		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		public static void Awake(ref MenuManager __instance)
		{
			if(__instance?.versionNumberText == null)
				return;

			var canvas = __instance.versionNumberText.GetComponentInParent<Canvas>();
			OnionMilk_crosshair.Plugin.ListChildren(canvas.transform);

			GameObject go = new GameObject("CrosshairSettings");
			var rct = go.AddComponent<RectTransform>();
			menuButtonCrosshair = go.AddComponent<UnityEngine.UI.Image>();
			UnityEngine.UI.Button btn = go.AddComponent<UnityEngine.UI.Button>();
			
			btn.targetGraphic = menuButtonCrosshair;
			btn.onClick.AddListener(OnClick);
			
			go.transform.SetParent(canvas.transform, false);
			go.transform.SetAsLastSibling();

			menuButtonCrosshair.sprite = OnionMilk_crosshair.Plugin.icon;
			menuButtonCrosshair.color = OnionMilk_crosshair.Plugin.color;
			rct.sizeDelta = rct.sizeDelta / 3f;
			rct.anchorMin = rct.anchorMax = rct.pivot = Vector2.one;
			rct.anchoredPosition = new(-50, -20);

			aud = __instance.MenuAudio;
			sfx = __instance.openMenuSound;

			
			go = new GameObject("CrosshairLabel");
			rct = go.AddComponent<RectTransform>();
			var tmp = go.AddComponent<TextMeshProUGUI>();
			tmp.alignment = TextAlignmentOptions.Top;
			tmp.SetText("Set Crosshair");
			tmp.fontSize = tmp.fontSize / 3f;
			go.transform.SetParent(btn.transform, false);
			rct.anchorMin = rct.anchorMax = new Vector2(0.5f, 0);
			rct.pivot = new Vector2(0.5f, 1);
			rct.anchoredPosition = new(0, -5);
		}

		private static void OnClick()
		{
			aud.PlayOneShot(sfx, 1f);
			
			var available = Directory.EnumerateFiles(OnionMilk_crosshair.Plugin.GetPath, "*.png").ToList();
			int idx = available.FindIndex(f => f.Contains(OnionMilk_crosshair.Plugin.cfgImage.Value));
			int next = 0;
			if(idx > -1 && idx < available.Count - 1)
				next = idx + 1;
			else
				next = 0;

			OnionMilk_crosshair.Plugin.cfgImage.SetSerializedValue(
				available[next].Replace(OnionMilk_crosshair.Plugin.GetPath, string.Empty).TrimStart('\\')
			);
			OnionMilk_crosshair.Plugin.Log($"Changed crosshair to: {available[next]}");
			OnionMilk_crosshair.Plugin.LoadIcon(available[next]);

			menuButtonCrosshair.sprite = OnionMilk_crosshair.Plugin.icon;
		}
	}
}
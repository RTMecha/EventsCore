﻿using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using BepInEx;
using BepInEx.Configuration;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;

using SimpleJSON;
using LSFunctions;
using TMPro;
using DG.Tweening;

using EventsCore.Functions;

using RTFunctions.Functions;
using RTFunctions.Functions.Data.Player;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Optimization;

namespace EventsCore
{
	[BepInPlugin("com.mecha.eventscore", "Events Core", " 1.7.0")]
	[BepInDependency("com.mecha.rtfunctions")]
	[BepInProcess("Project Arrhythmia.exe")]
	public class EventsCorePlugin : BaseUnityPlugin
	{
		public static EventsCorePlugin inst;
		public static string className = "[<color=#FFFFFF>EventsCore</color>] " + PluginInfo.PLUGIN_VERSION + "\n";
		readonly Harmony harmony = new Harmony("EventsCore");

        #region Variables

		public static Color bgColorToLerp;
		public static Color timelineColorToLerp;

        #endregion

        #region Configs

        public static ConfigEntry<bool> EditorCamEnabled { get; set; }
        public static ConfigEntry<bool> EditorCamResetValues { get; set; }

		public static ConfigEntry<float> EditorCamSpeed { get; set; }

		public static ConfigEntry<KeyCode> EditorCamToggle { get; set; }

		public static ConfigEntry<bool> EditorCamUseKeys { get; set; }
		public static ConfigEntry<float> EditorCamSlowSpeed { get; set; }
		public static ConfigEntry<float> EditorCamFastSpeed { get; set; }

		public static ConfigEntry<bool> ShowGUI { get; set; }

		public static ConfigEntry<KeyCode> ShowGUIToggle { get; set; }

		public static ConfigEntry<bool> ShowIntro { get; set; }

		public static ConfigEntry<bool> ShowFX { get; set; }

		public static ConfigEntry<ShakeType> ShakeEventMode { get; set; }

		public enum ShakeType
        {
			Original,
			Catalyst
        }

		#endregion

		public static Shader analogGlitchShader;
		public static Material analogGlitchMaterial;
		public static Shader digitalGlitchShader;
		public static Material digitalGlitchMaterial;
		public static void GetKinoGlitch()
		{
			var assetBundle = AssetBundle.LoadFromFile($"{RTFile.ApplicationDirectory}{RTFunctions.FunctionsPlugin.BepInExAssetsPath}effects.asset"); // Get AssetBundle from assets folder.
			analogGlitchMaterial = assetBundle.LoadAsset<Material>("analogglitchmaterial.mat"); // Load asset
			digitalGlitchMaterial = assetBundle.LoadAsset<Material>("digitalglitchmaterial.mat"); // Load asset
			analogGlitchShader = assetBundle.LoadAsset<Shader>("analogglitch.shader"); // Load asset
			digitalGlitchShader = assetBundle.LoadAsset<Shader>("digitalglitch.shader"); // Load asset
		}

		void Awake()
		{
			inst = this;

			Logger.LogInfo($"Plugin Events Core is loaded!");

			EditorCamEnabled = Config.Bind("Camera", "Editor Camera Offset", false, "Enabling this will disable all regular Camera events (move, zoom, etc) and allow you to move the camera around freely. WASD to move, + and - to zoom and numpad 4 / numpad 6 to rotate.");
			EditorCamSpeed = Config.Bind("Camera", "Editor Camera Speed", 1f, "How fast the editor camera moves");
			EditorCamToggle = Config.Bind("Camera", "Editor Camera Toggle Key", KeyCode.F3, "Press this key to toggle the Editor Camera on or off.");
			EditorCamSlowSpeed = Config.Bind("Camera", "Editor Camera Slow Speed", 0.5f, "How slow the editor camera is when left trigger is held.");
			EditorCamFastSpeed = Config.Bind("Camera", "Editor Camera Fast Speed", 2f, "How fast the editor camera is when right trigger is held.");
			EditorCamUseKeys = Config.Bind("Camera", "Editor Camera Use Keys", true, "If the editor camera should use your keyboard or not.");
			EditorCamResetValues = Config.Bind("Camera", "Editor Camera Reset Values", true, "If the offset values should reset when the editor camera is disabled.");

			ShowGUI = Config.Bind("Game", "Players & GUI Active", true, "Sets the players and GUI elements active / inactive.");
			ShowGUIToggle = Config.Bind("Game", "Players & GUI Toggle Key", KeyCode.F9, "Press this key to toggle the players / GUI on or off.");
			ShowIntro = Config.Bind("Game", "Show Intro", true, "Sets whether the Intro GUI is active / inactive.");

			ShowFX = Config.Bind("Events", "Show Effects", true, "If disabled, effects like chroma, bloom, etc will be disabled.");

			ShakeEventMode = Config.Bind("Events", "Shake Mode", ShakeType.Original, "Original is for the original shake method, while Catalyst is for the new shake method.");

			Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateSettings);

			ModCompatibility.sharedFunctions.Add("EventsCoreEditorOffset", EditorCamEnabled.Value);

			if (!ModCompatibility.mods.ContainsKey("EventsCore"))
            {
				var mod = new ModCompatibility.Mod(inst, GetType());
				ModCompatibility.mods.Add("EventsCore", mod);
            }

			ModCompatibility.sharedFunctions.Add("EventsCoreConfigs", new List<ConfigEntryBase>
			{
				EditorCamEnabled,
				EditorCamSpeed,
				EditorCamToggle,
				EditorCamSlowSpeed,
				EditorCamFastSpeed,
				EditorCamUseKeys,
				EditorCamResetValues,
				ShowGUI,
				ShowGUIToggle,
				ShowIntro,
				ShowFX,
				ShakeEventMode,
			});

			RTFunctions.FunctionsPlugin.EventsCoreGameThemePrefix = UpdateThemePrefix;
			RTFunctions.FunctionsPlugin.EventsCoreUpdateThemePrefix = EventManagerThemePrefix;

			harmony.PatchAll(typeof(EventsCorePlugin));
			harmony.PatchAll(typeof(EventsInstance));

            try
            {
				RTFunctions.Patchers.ObjectManagerPatch.LevelTick -= Updater.OnLevelTick;
				RTFunctions.Patchers.ObjectManagerPatch.LevelTick += RTEventManager.OnLevelTick;
				RTFunctions.Patchers.ObjectManagerPatch.LevelTick += Updater.OnLevelTick;
            }
            catch (Exception ex)
            {
				Debug.LogException(ex);
            }

            try
			{
				GetKinoGlitch();
			}
            catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		void Update()
        {
			if (Input.GetKeyDown(EditorCamToggle.Value) && !LSHelpers.IsUsingInputField())
				EditorCamEnabled.Value = !EditorCamEnabled.Value;

			if (Input.GetKeyDown(ShowGUIToggle.Value) && !LSHelpers.IsUsingInputField())
				ShowGUI.Value = !ShowGUI.Value;
		}

		static void UpdateSettings(object sender, EventArgs e)
        {
			if (ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEditorOffset"))
				ModCompatibility.sharedFunctions["EventsCoreEditorOffset"] = EditorCamEnabled.Value;
			else
				ModCompatibility.sharedFunctions.Add("EventsCoreEditorOffset", EditorCamEnabled.Value);

			if (EventManager.inst)
				EventManager.inst.updateEvents();
		}

		public static DataManager.GameData.EventKeyframe CurrentKeyframeSelection => DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent];

		#region Patchers

		[HarmonyPatch(typeof(AudioManager), "Update")]
		[HarmonyPrefix]
		static bool AudioUpdatePrefix(AudioManager __instance)
        {
			float masterVol = (float)DataManager.inst.GetSettingInt("MasterVolume", 9) / 9f;

			if (RTEventManager.inst != null)
				__instance.masterVol = masterVol * RTEventManager.inst.audioVolume;
			else
				__instance.masterVol = masterVol;
			__instance.musicVol = (float)DataManager.inst.GetSettingInt("MusicVolume", 9) / 9f * __instance.masterVol;
			__instance.sfxVol = (float)DataManager.inst.GetSettingInt("EffectsVolume", 9) / 9f * __instance.masterVol;
			if (!__instance.isFading)
			{
				__instance.musicSources[__instance.activeMusicSourceIndex].volume = __instance.musicVol;
			}
			__instance.musicSources[__instance.activeMusicSourceIndex].pitch = __instance.pitch;

			return false;
		}

		[HarmonyPatch(typeof(AudioManager), "SetPitch")]
		[HarmonyPrefix]
		static bool SetPitchPrefix(AudioManager __instance, float __0)
		{
			if (RTEventManager.inst != null)
				RTEventManager.inst.pitchOffset = __0;
			else
				__instance.pitch = __0;

			return false;
		}

		[HarmonyPatch(typeof(GameManager), "Awake")]
		[HarmonyPrefix]
		static void AddRTEffectsManager()
		{
			if (!GameObject.Find("Game Systems/EffectsManager").GetComponent<RTEffectsManager>())
			{
				GameObject.Find("Game Systems/EffectsManager").AddComponent<RTEffectsManager>();
			}
			if (!GameObject.Find("Game Systems/EventManager").GetComponent<RTEventManager>())
            {
				GameObject.Find("Game Systems/EventManager").AddComponent<RTEventManager>();
			}

			var camBase = new GameObject("Camera Base");

			//camBase.transform.SetParent(EventManager.inst.camParent);
			//EventManager.inst.cam.transform.SetParent(camBase.transform);
			//EventManager.inst.camPer.transform.SetParent(camBase.transform);

            EventManager.inst.camParentTop.SetParent(camBase.transform);

			RTEventManager.inst.delayTracker = camBase.AddComponent<DelayTracker>();
        }

		[HarmonyPatch(typeof(EventManager), "Update")]
		[HarmonyPrefix]
		static bool EventManagerUpdatePrefix()
        {
			return false;
        }
		
		[HarmonyPatch(typeof(EventManager), "LateUpdate")]
		[HarmonyPrefix]
		static bool EventManagerLateUpdatePrefix()
        {
			return false;
        }

		static void EventManagerThemePrefix(EventManager __instance, float __0)
        {
			RTEventManager.updateTheme(__0);
        }

		[HarmonyPatch(typeof(EventManager), "updateShake")]
		[HarmonyPrefix]
		static bool EventManagerShakePrefix()
        {
			RTEventManager.inst.updateShake();
			return false;
        }

		[HarmonyPatch(typeof(EventManager), "updateEvents", new[] { typeof(int) })]
		[HarmonyPrefix]
		static bool EventManagerUpdateEventsPrefix1(int __0)
        {
			RTEventManager.inst.updateEvents(__0);
			return false;
        }

		[HarmonyPatch(typeof(EventManager), "updateEvents", new Type[] { })]
		[HarmonyPrefix]
		static bool EventManagerUpdateEventsPrefix2(EventManager __instance)
        {
			__instance.StartCoroutine(RTEventManager.inst.updateEvents());

			return false;
        }

		static void UpdateThemePrefix(GameManager __instance)
		{
			var beatmapTheme = RTHelpers.BeatmapTheme;

			RTFunctions.Patchers.BackgroundManagerPatch.bgColorToLerp = bgColorToLerp;

			if (GameStorageManager.inst)
			{
				GameStorageManager.inst.perspectiveCam.backgroundColor = bgColorToLerp;
				if (GameStorageManager.inst.checkpointImages.Count > 0)
					foreach (var image in GameStorageManager.inst.checkpointImages)
					{
						image.color = timelineColorToLerp;
					}

				GameStorageManager.inst.timelinePlayer.color = timelineColorToLerp;
				GameStorageManager.inst.timelineLeftCap.color = timelineColorToLerp;
				GameStorageManager.inst.timelineRightCap.color = timelineColorToLerp;
				GameStorageManager.inst.timelineLine.color = timelineColorToLerp;
			}

			if (EditorManager.inst == null && AudioManager.inst.CurrentAudioSource.time < 15f)
			{
				if (__instance.introTitle.color != timelineColorToLerp)
					__instance.introTitle.color = timelineColorToLerp;
				if (__instance.introArtist.color != timelineColorToLerp)
					__instance.introArtist.color = timelineColorToLerp;
			}
			if (__instance.guiImages.Length > 0)
			{
				foreach (var image in __instance.guiImages)
				{
					image.color = timelineColorToLerp;
				}
			}

			var componentsInChildren2 = __instance.menuUI.GetComponentsInChildren<TextMeshProUGUI>();
			for (int i = 0; i < componentsInChildren2.Length; i++)
			{
				componentsInChildren2[i].color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(bgColorToLerp));
			}
		}

		#endregion
	}
}

using System;
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
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;

namespace EventsCore
{
	[BepInPlugin("com.mecha.eventscore", "Events Core", " 1.4.1")]
	[BepInDependency("com.mecha.rtfunctions")]
	[BepInProcess("Project Arrhythmia.exe")]
	public class EventsCorePlugin : BaseUnityPlugin
	{
		public static string className = "[<color=#FFFFFF>EventsCore</color>] " + PluginInfo.PLUGIN_VERSION + "\n";
		public static ConfigEntry<bool> SetCanvas { get; set; }

		public static ConfigEntry<bool> AllowCameraEvent { get; set; }
		public static ConfigEntry<float> EditorSpeed { get; set; }

		public static ConfigEntry<KeyCode> EditorCamToggle { get; set; }

		public static ConfigEntry<bool> ShowGUI { get; set; }

		public static ConfigEntry<KeyCode> ShowGUIToggle { get; set; }

		void Awake()
		{
			Logger.LogInfo($"Plugin Events Core is loaded!");

			SetCanvas = Config.Bind("Optimization", "Show Overlay in Editor", false, "If you want to show the overlay in the editor, turn this on. Do note this will cause some issues with selecting UI elements on editor startup.");
			AllowCameraEvent = Config.Bind("Camera", "Editor Camera Offset", false, "Enabling this will disable all regular Camera events (move, zoom, etc) and allow you to move the camera around freely. WASD to move, + and - to zoom and numpad 4 / numpad 6 to rotate.");
			EditorSpeed = Config.Bind("Camera", "Editor Camera Speed", 1f, "How fast the editor camera moves");
			EditorCamToggle = Config.Bind("Camera", "Editor Camera Toggle Key", KeyCode.F2, "Press this key to toggle the Editor Camera on or off.");

			ShowGUI = Config.Bind("Game", "Players & GUI Active", true, "Sets the players and GUI elements active / inactive.");
			ShowGUIToggle = Config.Bind("Game", "Players & GUI Toggle Key", KeyCode.F9, "Press this key to toggle the players / GUI on or off.");

			Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateSettings);

			ModCompatibility.sharedFunctions.Add("EventsCoreEditorOffset", AllowCameraEvent.Value);

			harmony.PatchAll(typeof(EventsCorePlugin));
			harmony.PatchAll(typeof(EventsInstance));
		}

		void Update()
        {
			if (Input.GetKeyDown(EditorCamToggle.Value) && !LSHelpers.IsUsingInputField())
				AllowCameraEvent.Value = !AllowCameraEvent.Value;

			if (Input.GetKeyDown(ShowGUIToggle.Value) && !LSHelpers.IsUsingInputField())
				ShowGUI.Value = !ShowGUI.Value;

			if (EditorManager.inst != null && overlay != null)
			{
				canvas.gameObject.SetActive(EditorManager.inst.hasLoadedLevel);

				if (EditorManager.inst.isEditing)
				{
					if (canvas != null && SetCanvas.Value)
					{
						overlay.transform.SetParent(canvas);
					}
					if (canvas != null && !SetCanvas.Value)
					{
						overlay.transform.SetParent(GameManager.inst.playerGUI.transform);
					}
				}
				else
				{
					overlay.transform.SetParent(GameManager.inst.playerGUI.transform);
				}
			}
		}

		private static void UpdateSettings(object sender, EventArgs e)
        {
			if (ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEditorOffset"))
				ModCompatibility.sharedFunctions["EventsCoreEditorOffset"] = AllowCameraEvent.Value;
			else
				ModCompatibility.sharedFunctions.Add("EventsCoreEditorOffset", AllowCameraEvent.Value);
		}

		public static DataManager.GameData.EventKeyframe currentKeyframeSelection
        {
			get
            {
				return DataManager.inst.gameData.eventObjects.allEvents[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent];
			}
        }

		public static Harmony harmony = new Harmony("EventsCore");

		[HarmonyPatch(typeof(AudioManager), "Update")]
		[HarmonyPrefix]
		private static bool AudioUpdatePrefix(AudioManager __instance)
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
		private static bool SetPitchPrefix(AudioManager __instance, float __0)
		{
			Debug.LogFormat("{0}Set Pitch : {1}", className, __0);
			if (RTEventManager.inst != null)
			{
				RTEventManager.inst.pitchOffset = __0;
			}
			else
				__instance.pitch = __0;

			return false;
		}

		[HarmonyPatch(typeof(GameManager), "Awake")]
		[HarmonyPrefix]
		private static void AddRTEffectsManager()
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
		private static bool EventManagerUpdatePrefix()
        {
			return false;
        }

		[HarmonyPatch(typeof(EventManager), "updateTheme")]
		[HarmonyPrefix]
		private static bool EventManagerThemePrefix(float __0)
        {
			RTEventManager.inst.updateTheme(__0);
			return false;
        }

		[HarmonyPatch(typeof(EventManager), "updateShake")]
		[HarmonyPrefix]
		private static bool EventManagerShakePrefix()
        {
			RTEventManager.inst.updateShake();
			return false;
        }

		//[HarmonyPatch(typeof(EventManager), "updateEvents", new[] { typeof(int) })]
		//[HarmonyPrefix]
		private static bool EventManagerUpdateEventsPrefix1(int __0)
        {
			RTEventManager.inst.updateEvents(__0);
			return false;
        }

		//[HarmonyPatch(typeof(EventManager), "updateEvents", new System.Type[] { })]
		//[HarmonyPrefix]
		private static bool EventManagerUpdateEventsPrefix2()
        {
			for (int i = 0; i < DataManager.inst.gameData.eventObjects.allEvents.Count; i++)
            {
				RTEventManager.inst.updateEvents(i);
            }

			return false;
        }

		[HarmonyPatch(typeof(GameManager), "Start")]
		[HarmonyPostfix]
		private static void StartPatch(GameManager __instance)
        {
			SetTypes();
			perspectiveCam = __instance.CameraPerspective.GetComponent<Camera>();
			overlay = new GameObject("Overlay");
			if (EditorManager.inst != null)
			{
				canvas = CreateCanvas().transform;
				overlay.transform.SetParent(canvas);
			}
			else
            {
				overlay.transform.SetParent(__instance.playerGUI.transform);
            }

			var rt = overlay.AddComponent<RectTransform>();
			rt.anchoredPosition = Vector2.zero;
			rt.sizeDelta = new Vector2(10000f, 10000f);

			overlay.AddComponent<CanvasRenderer>();
			overlayRenderer = overlay.AddComponent<Image>();
		}

		public static System.Type modifiers;
		public static System.Type catalyst;

		public static void SetTypes()
        {
			if (GameObject.Find("BepInEx_Manager").GetComponentByName("ObjectModifiersPlugin"))
				modifiers = GameObject.Find("BepInEx_Manager").GetComponentByName("ObjectModifiersPlugin").GetType();
			if (GameObject.Find("BepInEx_Manager").GetComponentByName("CatalystBase"))
				catalyst = GameObject.Find("BepInEx_Manager").GetComponentByName("CatalystBase").GetType();
		}

		public static Transform canvas;
		public static Image overlayRenderer;
		public static GameObject overlay;
		public static Camera perspectiveCam;

		[HarmonyPatch(typeof(GameManager), "EndOfLevel")]
		[HarmonyPrefix]
		private static void EndOfLevelPrefix()
        {
			overlay.SetActive(false);
        }

		[HarmonyPatch(typeof(GameManager), "UpdateTheme")]
		[HarmonyPrefix]
		private static bool UpdateThemePrefix(GameManager __instance)
		{
			DataManager.BeatmapTheme beatmapTheme = __instance.LiveTheme;
			if (EditorManager.inst != null && EventEditor.inst.showTheme)
			{
				beatmapTheme = EventEditor.inst.previewTheme;
			}
			overlayRenderer.color = LSColors.fadeColor(overlayColorToLerp, RTEventManager.inst.overlayAlpha);
			perspectiveCam.backgroundColor = bgColorToLerp;
			Image[] componentsInChildren = __instance.timeline.GetComponentsInChildren<Image>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].color = timelineColorToLerp;
			}
			//int num = 0;
			//foreach (InputDataManager.CustomPlayer customPlayer in InputDataManager.inst.players)
			//{
			//	if (customPlayer != null && customPlayer.player != null)
			//	{
			//		customPlayer.player.SetColor(beatmapTheme.GetPlayerColor(num % 4), beatmapTheme.guiColor);
			//	}
			//	num++;
			//}
			if (InputDataManager.inst.players.Count > 0)
            {
				for (int i = 0; i < InputDataManager.inst.players.Count; i++)
				{
					if (InputDataManager.inst.players[i].player != null)
					{
						var player = InputDataManager.inst.players[i].player;
						player.SetColor(beatmapTheme.GetPlayerColor(i), beatmapTheme.guiColor);
					}
				}
			}
			if (EditorManager.inst == null && AudioManager.inst.CurrentAudioSource.time < 15f)
			{
				if (__instance.introTitle.color != timelineColorToLerp)
				{
					__instance.introTitle.color = timelineColorToLerp;
				}
				if (__instance.introArtist.color != timelineColorToLerp)
				{
					__instance.introArtist.color = timelineColorToLerp;
				}
			}
			if (__instance.guiImages.Length > 0)
			{
				foreach (var image in __instance.guiImages)
				{
					image.color = timelineColorToLerp;
				}
			}
			TextMeshProUGUI[] componentsInChildren2 = __instance.menuUI.GetComponentsInChildren<TextMeshProUGUI>();
			for (int i = 0; i < componentsInChildren2.Length; i++)
			{
				//Change this to InvertColorHue(InvertColorValue(bgColorToLerp));
				componentsInChildren2[i].color = EventExtensions.InvertColorHue(EventExtensions.InvertColorValue(bgColorToLerp));
			}
			for (int j = 0; j < DataManager.inst.gameData.beatmapObjects.Count; j++)
			{
				if (ObjectManager.inst.beatmapGameObjects.ContainsKey(DataManager.inst.gameData.beatmapObjects[j].id))
				{
					ObjectManager.GameObjectRef gameObjectRef = ObjectManager.inst.beatmapGameObjects[DataManager.inst.gameData.beatmapObjects[j].id];
					if (gameObjectRef.obj != null && gameObjectRef.rend != null && gameObjectRef.rend.enabled && modifiers == null)
					{
						Color color = Color.Lerp(beatmapTheme.GetObjColor(gameObjectRef.sequence.LastColor), beatmapTheme.GetObjColor(gameObjectRef.sequence.NewColor), gameObjectRef.sequence.ColorValue);
						if (DataManager.inst.gameData.beatmapObjects[j].objectType == DataManager.GameData.BeatmapObject.ObjectType.Helper)
						{
							color = LSColors.fadeColor(color, 0.35f);
						}
						if (gameObjectRef.obj.GetComponentInChildren<TextMeshPro>())
						{
							gameObjectRef.obj.GetComponentInChildren<TextMeshPro>().color = color;
						}
						if (gameObjectRef.obj.GetComponentInChildren<SpriteRenderer>())
						{
							gameObjectRef.obj.GetComponentInChildren<SpriteRenderer>().material.color = color;
						}
						else
						{
							if (gameObjectRef.mat.HasProperty("_Color"))
							{
								if (!showOnlyOnLayer)
								{
									if (highlightObjects && EditorManager.inst != null && EditorManager.inst.isEditing)
                                    {
										var list = new List<Transform>();

										var tf = gameObjectRef.obj.transform;
										list.Add(tf);

										while (tf.childCount != 0 && tf.GetChild(0) != null)
										{
											tf = tf.GetChild(0);
											list.Add(tf);
										}

										var rt = list[list.Count - 1].gameObject.GetComponentByName("RTObject");
										var b = (bool)rt.GetType().GetField("selected", BindingFlags.Public | BindingFlags.Instance).GetValue(rt);

										if (b)
                                        {
											if (Input.GetKey(KeyCode.LeftShift))
											{
												Color colorHover = new Color(highlightObjectsDoubleColor.r, highlightObjectsDoubleColor.g, highlightObjectsDoubleColor.b);

												if (color.r > 0.9f && color.g > 0.9f && color.b > 0.9f)
												{
													colorHover = new Color(-highlightObjectsDoubleColor.r, -highlightObjectsDoubleColor.g, -highlightObjectsDoubleColor.b);
												}

												gameObjectRef.mat.color = color + new Color(colorHover.r, colorHover.g, colorHover.b, 0f);
											}
											else
											{
												Color colorHover = new Color(highlightObjectsColor.r, highlightObjectsColor.g, highlightObjectsColor.b);

												if (color.r > 0.95f && color.g > 0.95f && color.b > 0.95f)
												{
													colorHover = new Color(-highlightObjectsColor.r, -highlightObjectsColor.g, -highlightObjectsColor.b);
												}

												gameObjectRef.mat.color = color + new Color(colorHover.r, colorHover.g, colorHover.b, 0f);
											}
										}
									}
									else
										gameObjectRef.mat.color = color;
								}
								else if (EditorManager.inst != null)
								{
									if (DataManager.inst.gameData.beatmapObjects[j].editorData.Layer != EditorManager.inst.layer)
									{
										gameObjectRef.mat.color = LSColors.fadeColor(color, color.a * layerOpacityOffset);
									}
								}
							}
						}
					}
				}
			}
			for (int k = 0; k < BackgroundManager.inst.backgroundObjects.Count; k++)
			{
				var backgroundObject = DataManager.inst.gameData.backgroundObjects[k];
				var gameObject = BackgroundManager.inst.backgroundObjects[k];
				Color color2 = beatmapTheme.backgroundColors[Mathf.Clamp(backgroundObject.color, 0, beatmapTheme.backgroundColors.Count - 1)];
				color2.a = 1f;
				gameObject.GetComponent<Renderer>().material.color = color2;
				if (backgroundObject.drawFade)
				{
					int num2 = 9;
					for (int l = 1; l < num2 - backgroundObject.layer; l++)
					{
						int num3 = num2 - backgroundObject.layer;
						float t = color2.a / (float)num3 * (float)l;
						Color b = beatmapTheme.backgroundColors[0];
						if (ColorMatch(b, beatmapTheme.backgroundColor, 0.01f))
						{
							b = bgColorToLerp;
							b.a = 1f;
							gameObject.transform.GetChild(l - 1).GetComponent<Renderer>().material.color = Color.Lerp(Color.Lerp(color2, b, t), b, t);
						}
						else
						{
							b.a = 1f;
							gameObject.transform.GetChild(l - 1).GetComponent<Renderer>().material.color = Color.Lerp(Color.Lerp(color2, b, t), b, t);
						}
					}
				}
			}
			return false;
		}

		public static Color bgColorToLerp;
		public static Color overlayColorToLerp;
		public static Color timelineColorToLerp;

		public static void SetShowable(bool _show, float _opacity, bool _highlightObjects, Color _highlightObjectsColor, Color _highlightObjectsDoubleColor)
		{
			showOnlyOnLayer = _show;
			layerOpacityOffset = _opacity;
			highlightObjects = _highlightObjects;
			highlightObjectsColor = _highlightObjectsColor;
			highlightObjectsDoubleColor = _highlightObjectsDoubleColor;
		}

		public static bool showOnlyOnLayer = false;
		public static float layerOpacityOffset = 0.2f;
		public static bool highlightObjects = false;
		public static Color highlightObjectsColor;
		public static Color highlightObjectsDoubleColor;

		public static GameObject CreateCanvas(string _name = "")
        {
			string n = _name;
			if (n == "")
            {
				n = "Canvas";
            }
			var inter = new GameObject(n);
			inter.transform.localScale = Vector3.one * EditorManager.inst.ScreenScale;
			var interfaceRT = inter.AddComponent<RectTransform>();
			interfaceRT.anchoredPosition = new Vector2(960f, 540f);
			interfaceRT.sizeDelta = new Vector2(1920f, 1080f);
			interfaceRT.pivot = new Vector2(0.5f, 0.5f);
			interfaceRT.anchorMin = Vector2.zero;
			interfaceRT.anchorMax = Vector2.zero;

			var canvas = inter.AddComponent<Canvas>();
			canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
			canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1;
			canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Tangent;
			canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Normal;
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.scaleFactor = EditorManager.inst.ScreenScale;

			var canvasScaler = inter.AddComponent<CanvasScaler>();
			canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);

			Debug.LogFormat("{0}Canvas Scale Factor: {1}\nResoultion: {2}", className, canvas.scaleFactor, new Vector2(Screen.width, Screen.height));

			inter.AddComponent<GraphicRaycaster>();

			return inter;
		}

		public static bool ColorMatch(Color _col1, Color _col2, float _range, bool alpha = false)
		{
			if (alpha)
				if (_col1.r < _col2.r + _range && _col1.r > _col2.r - _range && _col1.g < _col2.g + _range && _col1.g > _col2.g - _range && _col1.b < _col2.b + _range && _col1.b > _col2.b - _range && _col1.a < _col2.a + _range && _col1.a > _col2.a - _range)
				{
					return true;
				}
			else
				if (_col1.r < _col2.r + _range && _col1.r > _col2.r - _range && _col1.g < _col2.g + _range && _col1.g > _col2.g - _range && _col1.b < _col2.b + _range && _col1.b > _col2.b - _range)
				{
					return true;
				}

			return false;
        }

		public static bool ColorRange(Color _base, Color _match, DataManager.VersionComparison _range, bool opacity = false, string val = "")
		{
			if (_range == DataManager.VersionComparison.EqualTo)
			{
				if (val.ToLower() == "r")
				{
					if (_base.r == _match.r)
                    {
						return true;
                    }
				}
				if (val.ToLower() == "g")
				{
					if (_base.g == _match.g)
                    {
						return true;
                    }
				}
				if (val.ToLower() == "b")
				{
					if (_base.b == _match.b)
                    {
						return true;
                    }
				}
				if (val.ToLower() == "a")
				{
					if (_base.a == _match.a)
                    {
						return true;
                    }
				}

				if (_base.r == _match.r && _base.g == _match.g && _base.b == _match.b && (opacity && _base.a == _match.a || !opacity))
                {
					return true;
                }
            }
			else if (_range == DataManager.VersionComparison.LessThan)
			{
				if (val.ToLower() == "r")
				{
					if (_base.r < _match.r)
                    {
						return true;
                    }
				}
				if (val.ToLower() == "g")
				{
					if (_base.g < _match.g)
                    {
						return true;
                    }
				}
				if (val.ToLower() == "b")
				{
					if (_base.b < _match.b)
                    {
						return true;
                    }
				}
				if (val.ToLower() == "a")
				{
					if (_base.a < _match.a)
                    {
						return true;
                    }
				}

				if (_base.r < _match.r && _base.g < _match.g && _base.b < _match.b && (opacity && _base.a < _match.a || !opacity))
                {
					return true;
                }
            }
			else if (_range == DataManager.VersionComparison.GreaterThan)
			{
				if (val.ToLower() == "r")
				{
					if (_base.r > _match.r)
                    {
						return true;
                    }
				}
				if (val.ToLower() == "g")
				{
					if (_base.g > _match.g)
                    {
						return true;
                    }
				}
				if (val.ToLower() == "b")
				{
					if (_base.b > _match.b)
                    {
						return true;
                    }
				}
				if (val.ToLower() == "a")
				{
					if (_base.a > _match.a)
                    {
						return true;
                    }
				}

				if (_base.r > _match.r && _base.g > _match.g && _base.b > _match.b && (opacity && _base.a > _match.a || !opacity))
                {
					return true;
                }
            }
			return false;
		}
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;

using LSFunctions;

using DG.Tweening;
using DG.Tweening.Core;

using EventsCore.Functions;

using RTFunctions.Functions;
using RTFunctions.Functions.Animation;
using RTFunctions.Functions.Animation.Keyframe;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.IO;

using DOSequence = DG.Tweening.Sequence;
using Ease = RTFunctions.Functions.Animation.Ease;
using EaseFunction = RTFunctions.Functions.Animation.EaseFunction;
using Random = UnityEngine.Random;

namespace EventsCore
{
    public class RTEventManager : MonoBehaviour
    {
        public DelayTracker delayTracker;

        public static RTEventManager inst;

        void Awake()
        {
            if (inst == null)
                inst = this;
            else if (inst != this)
                Destroy(gameObject);

            Reset();

            if (!ModCompatibility.sharedFunctions.ContainsKey("EventsCoreResetOffsets"))
                ModCompatibility.sharedFunctions.Add("EventsCoreResetOffsets", (Action)Reset);
            else
                ModCompatibility.sharedFunctions["EventsCoreResetOffsets"] = (Action)Reset;
        }

        public void Reset()
        {
            ModCompatibility.sharedFunctions["EventsCoreEventOffsets"] = ResetOffsets();
        }

        //LAYER 1:
        // 00 - Move
        // 01 - Zoom
        // 02 - Rotate
        // 03 - Shake
        // 04 - Theme
        // 05 - Chromatic
        // 06 - Bloom
        // 07 - Vignette
        // 08 - Lens
        // 09 - Grain
        // 10 - ColorGrading
        // 11 - Gradient
        // 12 - RadialBlur
        // 13 - ColorSplit

        //LAYER 2:
        // 00 - Cam Offset
        // 01 - Gradient
        // 02 - DoubleVision
        // 03 - Scanlines
        // 04 - Blur
        // 05 - Pixelize
        // 06 - BG
        // 07 - Invert
        // 08 - Timeline
        // 09 - Player
        // 10 - Follow Player
        // 11 - Music
        // 12 - Glitch
        // 13 - Misc

        public static bool Playable =>
            RTEffectsManager.inst &&
            EventManager.inst &&
            GameManager.inst &&
            (GameManager.inst.gameState == GameManager.State.Playing || GameManager.inst.gameState == GameManager.State.Reversing) &&
            DataManager.inst.gameData != null &&
            DataManager.inst.gameData.eventObjects != null &&
            DataManager.inst.gameData.eventObjects.allEvents != null &&
            DataManager.inst.gameData.eventObjects.allEvents.Count > 0;

        void SpeedHandler()
        {
            if (EventsCorePlugin.EditorCamEnabled.Value)
            {
                if (!LSHelpers.IsUsingInputField())
                {
                    float multiply = 1f;

                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                        multiply = 0.5f;
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                        multiply = 2f;

                    if (Input.GetKey(KeyCode.A))
                    {
                        editorOffset.x -= 0.1f * EditorSpeed * multiply;
                    }
                    if (Input.GetKey(KeyCode.D))
                    {
                        editorOffset.x += 0.1f * EditorSpeed * multiply;
                    }
                    if (Input.GetKey(KeyCode.W))
                    {
                        editorOffset.y += 0.1f * EditorSpeed * multiply;
                    }
                    if (Input.GetKey(KeyCode.S))
                    {
                        editorOffset.y -= 0.1f * EditorSpeed * multiply;
                    }

                    //float x = InputDataManager.inst.menuActions.Move.Vector.x;
                    //float y = InputDataManager.inst.menuActions.Move.Vector.y;

                    //var vector = new Vector3(x, y, 0f);
                    //if (vector.magnitude > 1f)
                    //    vector = vector.normalized;
                    //editorOffset.x += vector.x;
                    //editorOffset.y += vector.y;

                    if (Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.Plus))
                        editorZoom += 0.1f * EditorSpeed * multiply;
                    if (Input.GetKey(KeyCode.KeypadMinus) || Input.GetKey(KeyCode.Minus))
                        editorZoom -= 0.1f * EditorSpeed * multiply;

                    if (Input.GetKey(KeyCode.Keypad4))
                        editorRotate += 0.1f * EditorSpeed * multiply;
                    if (Input.GetKey(KeyCode.Keypad6))
                        editorRotate -= 0.1f * EditorSpeed * multiply;

                    if (Input.GetKey(KeyCode.LeftArrow))
                        editorPerRotate.y += 0.1f * EditorSpeed * multiply;
                    if (Input.GetKey(KeyCode.RightArrow))
                        editorPerRotate.y -= 0.1f * EditorSpeed * multiply;

                    if (Input.GetKey(KeyCode.UpArrow))
                        editorPerRotate.x += 0.1f * EditorSpeed * multiply;
                    if (Input.GetKey(KeyCode.DownArrow))
                        editorPerRotate.x -= 0.1f * EditorSpeed * multiply;

                    if (Input.GetKeyDown(KeyCode.Keypad5))
                    {
                        editorOffset = EventManager.inst.camPos;
                        if (!float.IsNaN(EventManager.inst.camZoom))
                            editorZoom = EventManager.inst.camZoom;
                        if (!float.IsNaN(EventManager.inst.camRot))
                            editorRotate = EventManager.inst.camRot;
                        editorPerRotate = Vector2.zero;
                    }
                }
            }
            else
            {
                editorOffset = EventManager.inst.camPos;
                if (!float.IsNaN(EventManager.inst.camZoom))
                    editorZoom = EventManager.inst.camZoom;
                if (!float.IsNaN(EventManager.inst.camRot))
                    editorRotate = EventManager.inst.camRot;
                editorPerRotate = Vector2.zero;
            }
        }

        void FunctionsHandler()
        {
            if (!ModCompatibility.sharedFunctions.ContainsKey("EventsCorePitchOffset"))
            {
                ModCompatibility.sharedFunctions.Add("EventsCorePitchOffset", pitchOffset);
            }
            if (ModCompatibility.sharedFunctions.ContainsKey("EventsCorePitchOffset"))
            {
                ModCompatibility.sharedFunctions["EventsCorePitchOffset"] = pitchOffset;
            }

            if (!ModCompatibility.sharedFunctions.ContainsKey("EventsCoreFollowCamera"))
                ModCompatibility.sharedFunctions.Add("EventsCoreFollowCamera", delayTracker.move && delayTracker.active);

            if (ModCompatibility.sharedFunctions.ContainsKey("EventsCoreFollowCamera"))
                ModCompatibility.sharedFunctions["EventsCoreFollowCamera"] = delayTracker.move && delayTracker.active;

            if (!ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEventOffsets"))
            {
                ModCompatibility.sharedFunctions.Add("EventsCoreEventOffsets", offsets);
            }

            if (ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEventOffsets"))
            {
                offsets = (List<List<float>>)ModCompatibility.sharedFunctions["EventsCoreEventOffsets"];
            }
        }

        void Interpolate()
        {
            var allEvents = DataManager.inst.gameData.eventObjects.allEvents;
            var time = AudioManager.inst.CurrentAudioSource.time;

            if (shakeSequence != null && shakeSequence.keyframes != null && shakeSequence.keyframes.Length > 0 && EventsCorePlugin.ShakeEventMode.Value == EventsCorePlugin.ShakeType.Catalyst)
                EventManager.inst.shakeVector = shakeSequence.Interpolate(time);

            for (int i = 0; i < allEvents.Count; i++)
            {
                var list = allEvents[i].OrderBy(x => x.eventTime).ToList();

                var nextKFIndex = list.FindIndex(x => x.eventTime > time);

                if (nextKFIndex >= 0)
                {
                    var prevKFIndex = nextKFIndex - 1;
                    if (prevKFIndex < 0)
                        prevKFIndex = 0;

                    var nextKF = list[nextKFIndex];
                    var prevKF = list[prevKFIndex];

                    if (events.Length > i)
                    {
                        for (int j = 0; j < nextKF.eventValues.Length; j++)
                        {
                            if (events[i].Length > j && prevKF.eventValues.Length > j && events[i][j] != null)
                            {
                                //var total = 0f;
                                //for (int k = 0; k < nextKFIndex; k++)
                                //    total += allEvents[i][k].eventValues[j];

                                var next = nextKF.eventValues[j];
                                var prev = prevKF.eventValues[j];

                                bool notLerper = i == 4 || i == 6 && j == 4 || i == 7 && j == 6 || i == 15 && (j == 2 || j == 3) || i == 20 || i == 22 && j == 6;

                                if (float.IsNaN(prev) || notLerper)
                                    prev = 0f;

                                if (float.IsNaN(next))
                                    next = 0f;

                                if (notLerper)
                                    next = 1f;

                                var x = RTMath.Lerp(prev, next, Ease.GetEaseFunction(nextKF.curveType.Name)(RTMath.InverseLerp(prevKF.eventTime, nextKF.eventTime, time)));

                                if (prevKFIndex == nextKFIndex)
                                    x = next;

                                float offset = 0f;
                                if (offsets.Count > i && offsets[i].Count > j && !notLerper)
                                    offset = offsets[i][j];

                                if (float.IsNaN(offset) || float.IsInfinity(offset))
                                    offset = 0f;

                                if (float.IsNaN(x) || float.IsInfinity(x))
                                    x = next;

                                events[i][j](x + offset);
                            }

                            // Figure out how to make the camera shake AND have a smoothness value.
                            //if (i == 3)
                            //{
                            //
                            //}    
                        }
                    }
                }
                else if (list.Count > 0)
                {
                    if (events.Length > i)
                    {
                        for (int j = 0; j < list[list.Count - 1].eventValues.Length; j++)
                        {
                            if (events[i].Length > j && events[i][j] != null)
                            {
                                bool notLerper = i == 4 || i == 6 && j == 4 || i == 7 && j == 6 || i == 15 && (j == 2 || j == 3) || i == 20 || i == 22 && j == 6;

                                var x = list[list.Count - 1].eventValues[j];

                                if (float.IsNaN(x))
                                    x = 0f;

                                if (notLerper)
                                    x = 1f;

                                float offset = 0f;
                                if (offsets.Count > i && offsets[i].Count > j && !notLerper)
                                    offset = offsets[i][j];

                                if (float.IsNaN(offset) || float.IsInfinity(offset))
                                    offset = 0f;

                                if (float.IsNaN(x) || float.IsInfinity(x))
                                    x = allEvents[i][allEvents[i].Count - 1].eventValues[j];

                                events[i][j](x + offset);
                            }

                            // Figure out how to make the camera shake AND have a smoothness value.
                            //if (i == 3)
                            //{
                            //
                            //}    
                        }
                    }
                }
            }
        }

        float Lerp(float x, float y, float t) => x + (y - x) * t;

        public float fieldOfView = 50f;

        public float camPerspectiveOffset = 10f;

        void Update()
        {
            SpeedHandler(); updateShake(); FunctionsHandler();
            GameManager.inst.timeline.SetActive(timelineActive);

            if (GameManager.inst.introMain != null && AudioManager.inst.CurrentAudioSource.time < 15f)
                GameManager.inst.introMain.SetActive(EventsCorePlugin.ShowIntro.Value);

            if (Playable)
            {
                InputDataManager.inst.SetAllControllerRumble(EventManager.inst.shakeMultiplier);

                #region Lerp Colors

                if (!float.IsNaN(bloomColor))
                    LerpBloomColor();
                if (!float.IsNaN(vignetteColor))
                    LerpVignetteColor();
                if (!float.IsNaN(gradientColor1))
                    LerpGradientColor1();
                if (!float.IsNaN(gradientColor2))
                    LerpGradientColor2();
                if (!float.IsNaN(bgColor))
                    LerpBGColor();
                if (!float.IsNaN(timelineColor))
                    LerpTimelineColor();

                FindColors();

                #endregion

                #region New Sequences

                if (EventManager.inst.eventSequence == null)
                {
                    EventManager.inst.eventSequence = DOTween.Sequence();
                }
                if (EventManager.inst.themeSequence == null)
                {
                    EventManager.inst.themeSequence = DOTween.Sequence();
                }
                if (EventManager.inst.shakeSequence == null && EventsCorePlugin.ShakeEventMode.Value == EventsCorePlugin.ShakeType.Original)
                {
                    EventManager.inst.shakeSequence = DOTween.Sequence();

                    float strength = 3f;
                    int vibrato = 10;
                    float randomness = 90f;
                    EventManager.inst.shakeSequence.Insert(0f, DOTween.Shake(() => Vector3.zero, delegate (Vector3 x)
                    {
                        EventManager.inst.shakeVector = x;
                    }, AudioManager.inst.CurrentAudioSource.clip.length, strength, vibrato, randomness, true, false));
                }

                #endregion

                Interpolate();

                #region Camera

                if (float.IsNaN(EventManager.inst.camRot))
                    EventManager.inst.camRot = 0f;
                if (float.IsNaN(EventManager.inst.camZoom) || EventManager.inst.camZoom == 0f)
                    EventManager.inst.camZoom = 20f;

                if (!EventsCorePlugin.EditorCamEnabled.Value)
                    EventManager.inst.cam.orthographicSize = EventManager.inst.camZoom;
                else if (EditorSpeed != 0f)
                    EventManager.inst.cam.orthographicSize = editorZoom;

                if (!float.IsNaN(EventManager.inst.camRot) && !EventsCorePlugin.EditorCamEnabled.Value)
                    EventManager.inst.camParent.transform.rotation = Quaternion.Euler(new Vector3(EventManager.inst.camParent.transform.rotation.x, EventManager.inst.camParent.transform.rotation.y, EventManager.inst.camRot));
                else if (!float.IsNaN(editorRotate))
                    EventManager.inst.camParent.transform.rotation = Quaternion.Euler(new Vector3(editorPerRotate.x, editorPerRotate.y, editorRotate));

                if (EditorManager.inst == null || !EventsCorePlugin.EditorCamEnabled.Value)
                    EventManager.inst.camParentTop.transform.localPosition = new Vector3(EventManager.inst.camPos.x, EventManager.inst.camPos.y, -10f);
                else
                    EventManager.inst.camParentTop.transform.localPosition = new Vector3(editorOffset.x, editorOffset.y, -10f);

                EventManager.inst.camPer.fieldOfView = fieldOfView;

                if (!EventsCorePlugin.EditorCamEnabled.Value)
                    EventManager.inst.camPer.transform.position = new Vector3(EventManager.inst.camPer.transform.position.x, EventManager.inst.camPer.transform.position.y, -(EventManager.inst.camZoom) / RTHelpers.perspectiveZoom);
                else
                    EventManager.inst.camPer.transform.position = new Vector3(EventManager.inst.camPer.transform.position.x, EventManager.inst.camPer.transform.position.y, -editorZoom / RTHelpers.perspectiveZoom);

                EventManager.inst.camPer.nearClipPlane = -EventManager.inst.camPer.transform.position.z + camPerspectiveOffset;

                #endregion

                #region Updates

                bool allowFX = EventsCorePlugin.ShowFX.Value;

                if (!float.IsNaN(EventManager.inst.camChroma))
                    LSEffectsManager.inst.UpdateChroma(!allowFX ? 0f : EventManager.inst.camChroma);
                if (!float.IsNaN(EventManager.inst.camBloom))
                    LSEffectsManager.inst.UpdateBloom(!allowFX ? 0f : EventManager.inst.camBloom);
                if (!float.IsNaN(EventManager.inst.vignetteIntensity))
                    LSEffectsManager.inst.UpdateVignette(!allowFX ? 0f : EventManager.inst.vignetteIntensity, EventManager.inst.vignetteSmoothness, Mathf.RoundToInt(EventManager.inst.vignetteRounded) == 1, EventManager.inst.vignetteRoundness, EventManager.inst.vignetteCenter);
                if (!float.IsNaN(EventManager.inst.lensDistortIntensity))
                    LSEffectsManager.inst.UpdateLensDistort(!allowFX ? 0f : EventManager.inst.lensDistortIntensity);
                if (!float.IsNaN(EventManager.inst.grainIntensity))
                    LSEffectsManager.inst.UpdateGrain(!allowFX ? 0f : EventManager.inst.grainIntensity, Mathf.RoundToInt(EventManager.inst.grainColored) == 1, EventManager.inst.grainSize);
                if (!float.IsNaN(pixel))
                    LSEffectsManager.inst.pixelize.amount.Override(!allowFX ? 0f : pixel);

                //New effects
                if (!float.IsNaN(colorGradingHueShift))
                    RTEffectsManager.inst.UpdateColorGrading(
                        !allowFX ? 0f : colorGradingHueShift,
                        !allowFX ? 0f : colorGradingContrast,
                        !allowFX ? Vector4.zero : colorGradingGamma,
                        !allowFX ? 0f : colorGradingSaturation,
                        !allowFX ? 0f : colorGradingTemperature,
                        !allowFX ? 0f : colorGradingTint);
                if (!float.IsNaN(gradientIntensity))
                    RTEffectsManager.inst.UpdateGradient(!allowFX ? 0f : gradientIntensity, gradientRotation);
                if (!float.IsNaN(ripplesStrength))
                    RTEffectsManager.inst.UpdateRipples(!allowFX ? 0f : ripplesStrength, ripplesSpeed, ripplesDistance, ripplesHeight, ripplesWidth);
                if (!float.IsNaN(doubleVision))
                    RTEffectsManager.inst.UpdateDoubleVision(!allowFX ? 0f : doubleVision);
                if (!float.IsNaN(radialBlurIntensity))
                    RTEffectsManager.inst.UpdateRadialBlur(!allowFX ? 0f : radialBlurIntensity, radialBlurIterations);
                if (!float.IsNaN(scanLinesIntensity))
                    RTEffectsManager.inst.UpdateScanlines(!allowFX ? 0f : scanLinesIntensity, scanLinesAmount, scanLinesSpeed);
                if (!float.IsNaN(sharpen))
                    RTEffectsManager.inst.UpdateSharpen(!allowFX ? 0f : sharpen);
                if (!float.IsNaN(colorSplitOffset))
                    RTEffectsManager.inst.UpdateColorSplit(!allowFX ? 0f : colorSplitOffset);
                if (!float.IsNaN(dangerIntensity))
                    RTEffectsManager.inst.UpdateDanger(!allowFX ? 0f : dangerIntensity, dangerColor, dangerSize);
                if (!float.IsNaN(invertAmount))
                    RTEffectsManager.inst.UpdateInvert(!allowFX ? 0f : invertAmount);

                if (!float.IsNaN(timelineRot))
                {
                    GameManager.inst.timeline.transform.localPosition = new Vector3(timelinePos.x, timelinePos.y, 0f);
                    GameManager.inst.timeline.transform.localScale = new Vector3(timelineSca.x, timelineSca.y, 1f);
                    GameManager.inst.timeline.transform.eulerAngles = new Vector3(0f, 0f, timelineRot);
                }

                foreach (var customPlayer in PlayerManager.Players)
                {
                    if (customPlayer.Player && customPlayer.Player.playerObjects.ContainsKey("RB Parent"))
                    {
                        var player = customPlayer.Player.playerObjects["RB Parent"].gameObject.transform;
                        if (!playersCanMove)
                        {
                            player.localPosition = new Vector3(playerPositionX, playerPositionY, 0f);
                            player.localRotation = Quaternion.Euler(0f, 0f, playerRotation);
                        }
                    }
                }

                #endregion
            }

            EventManager.inst.prevCamZoom = EventManager.inst.camZoom;
        }

        void FixedUpdate()
        {
            if (delayTracker.leader == null && InputDataManager.inst.players.Count > 0 && GameManager.inst.players.transform.Find("Player 1/Player"))
            {
                delayTracker.leader = GameManager.inst.players.transform.Find("Player 1/Player");
            }
        }

        #region Lerp Colors

        public void FindColors()
        {
            var allEvents = DataManager.inst.gameData.eventObjects.allEvents;

            if (allEvents[4].Count > 0)
            {
                if (allEvents[4].Find(x => x.eventTime > AudioManager.inst.CurrentAudioSource.time + 0.0001f) != null)
                {
                    var nextKF = allEvents[4].Find(x => x.eventTime > AudioManager.inst.CurrentAudioSource.time + 0.0001f);
                    if (allEvents[4].IndexOf(nextKF) - 1 > -1)
                    {
                        if (allEvents[4][allEvents[4].IndexOf(nextKF) - 1].eventValues.Length > 0)
                        {
                            EventManager.inst.LastTheme = (int)allEvents[4][allEvents[4].IndexOf(nextKF) - 1].eventValues[0];
                        }
                    }
                    else
                    {
                        if (allEvents[4][0].eventValues.Length > 0)
                        {
                            EventManager.inst.LastTheme = (int)allEvents[4][0].eventValues[0];
                        }
                    }
                    if (nextKF.eventValues.Length > 0)
                    {
                        EventManager.inst.NewTheme = (int)nextKF.eventValues[0];
                    }
                }
                else
                {
                    var finalKF = allEvents[4][allEvents[4].Count - 1];

                    int a = allEvents[4].Count - 2;
                    if (a < 0)
                    {
                        a = 0;
                    }
                    if (allEvents[4][a].eventValues.Length > 0)
                    {
                        EventManager.inst.LastTheme = (int)allEvents[4][a].eventValues[0];
                    }

                    if (finalKF.eventValues.Length > 0)
                    {
                        EventManager.inst.NewTheme = (int)finalKF.eventValues[0];
                    }
                }
            }

            if (allEvents[6].Count > 0)
            {
                if (allEvents[6].Find(x => x.eventTime > AudioManager.inst.CurrentAudioSource.time) != null)
                {
                    var nextKF = allEvents[6].Find(x => x.eventTime > AudioManager.inst.CurrentAudioSource.time);
                    if (allEvents[6].IndexOf(nextKF) - 1 > -1)
                    {
                        if (allEvents[6][allEvents[6].IndexOf(nextKF) - 1].eventValues.Length > 4)
                        {
                            prevBloomColor = (int)allEvents[6][allEvents[6].IndexOf(nextKF) - 1].eventValues[4];
                        }
                    }
                    else
                    {
                        if (allEvents[6][0].eventValues.Length > 4)
                        {
                            prevBloomColor = (int)allEvents[6][0].eventValues[4];
                        }
                    }
                    if (nextKF.eventValues.Length > 4)
                    {
                        nextBloomColor = (int)nextKF.eventValues[4];
                    }
                }
                else
                {
                    var finalKF = allEvents[6][allEvents[6].Count - 1];

                    int a = allEvents[6].Count - 2;
                    if (a < 0)
                    {
                        a = 0;
                    }
                    if (allEvents[6][a].eventValues.Length > 4)
                    {
                        prevBloomColor = (int)allEvents[6][a].eventValues[4];
                    }

                    if (finalKF.eventValues.Length > 4)
                    {
                        nextBloomColor = (int)finalKF.eventValues[4];
                    }
                }
            }

            if (allEvents[7].Count > 0)
            {
                if (allEvents[7].Find(x => x.eventTime > AudioManager.inst.CurrentAudioSource.time) != null)
                {
                    var nextKF = allEvents[7].Find(x => x.eventTime > AudioManager.inst.CurrentAudioSource.time);
                    if (allEvents[7].IndexOf(nextKF) - 1 > -1)
                    {
                        if (allEvents[7][allEvents[7].IndexOf(nextKF) - 1].eventValues.Length > 6)
                        {
                            prevVignetteColor = (int)allEvents[7][allEvents[7].IndexOf(nextKF) - 1].eventValues[6];
                        }
                    }
                    else
                    {
                        if (allEvents[7][0].eventValues.Length > 6)
                        {
                            prevVignetteColor = (int)allEvents[7][0].eventValues[6];
                        }
                    }
                    if (nextKF.eventValues.Length > 6)
                    {
                        nextVignetteColor = (int)nextKF.eventValues[6];
                    }
                }
                else
                {
                    var finalKF = allEvents[7][allEvents[7].Count - 1];

                    int a = allEvents[7].Count - 2;
                    if (a < 0)
                    {
                        a = 0;
                    }
                    if (allEvents[7][a].eventValues.Length > 6)
                    {
                        prevVignetteColor = (int)allEvents[7][a].eventValues[6];
                    }

                    if (finalKF.eventValues.Length > 6)
                    {
                        nextVignetteColor = (int)finalKF.eventValues[6];
                    }
                }
            }

            if (allEvents.Count > 15 && allEvents[15].Count > 0)
            {
                if (allEvents[15].Find(x => x.eventTime > AudioManager.inst.CurrentAudioSource.time) != null)
                {
                    var nextKF = allEvents[15].Find(x => x.eventTime > AudioManager.inst.CurrentAudioSource.time);
                    if (allEvents[15].IndexOf(nextKF) - 1 > -1)
                    {
                        if (allEvents[15][allEvents[15].IndexOf(nextKF) - 1].eventValues.Length > 2)
                        {
                            prevGradientColor1 = (int)allEvents[15][allEvents[15].IndexOf(nextKF) - 1].eventValues[2];
                            prevGradientColor2 = (int)allEvents[15][allEvents[15].IndexOf(nextKF) - 1].eventValues[3];
                        }
                    }
                    else
                    {
                        if (allEvents[15][0].eventValues.Length > 2)
                        {
                            prevGradientColor1 = (int)allEvents[15][0].eventValues[2];
                            prevGradientColor2 = (int)allEvents[15][0].eventValues[3];
                        }
                    }
                    if (nextKF.eventValues.Length > 2)
                    {
                        nextGradientColor1 = (int)nextKF.eventValues[2];
                        nextGradientColor2 = (int)nextKF.eventValues[3];
                    }
                }
                else
                {
                    var finalKF = allEvents[15][allEvents[15].Count - 1];

                    int a = allEvents[15].Count - 2;
                    if (a < 0)
                    {
                        a = 0;
                    }
                    if (allEvents[15][a].eventValues.Length > 2)
                    {
                        prevGradientColor1 = (int)allEvents[15][a].eventValues[2];
                        prevGradientColor2 = (int)allEvents[15][a].eventValues[3];
                    }

                    if (finalKF.eventValues.Length > 2)
                    {
                        nextGradientColor1 = (int)finalKF.eventValues[2];
                        nextGradientColor2 = (int)finalKF.eventValues[3];
                    }
                }
            }

            if (allEvents.Count > 20 && allEvents[20].Count > 0)
            {
                if (allEvents[20].Find(x => x.eventTime > AudioManager.inst.CurrentAudioSource.time) != null)
                {
                    var nextKF = allEvents[20].Find(x => x.eventTime > AudioManager.inst.CurrentAudioSource.time);
                    if (allEvents[20].IndexOf(nextKF) - 1 > -1)
                    {
                        prevBGColor = (int)allEvents[20][allEvents[20].IndexOf(nextKF) - 1].eventValues[0];
                    }
                    else
                    {
                        prevBGColor = (int)allEvents[20][0].eventValues[0];
                    }
                    nextBGColor = (int)nextKF.eventValues[0];
                }
                else
                {
                    var finalKF = allEvents[20][allEvents[20].Count - 1];

                    int a = allEvents[20].Count - 2;
                    if (a < 0)
                    {
                        a = 0;
                    }
                    prevBGColor = (int)allEvents[20][a].eventValues[0];
                    nextBGColor = (int)finalKF.eventValues[0];
                }
            }

            if (allEvents.Count > 22 && allEvents[22].Count > 0)
            {
                if (allEvents[22].Find(x => x.eventTime > AudioManager.inst.CurrentAudioSource.time) != null)
                {
                    var nextKF = allEvents[22].Find(x => x.eventTime > AudioManager.inst.CurrentAudioSource.time);
                    if (allEvents[22].IndexOf(nextKF) - 1 > -1)
                    {
                        prevTimelineColor = (int)allEvents[22][allEvents[22].IndexOf(nextKF) - 1].eventValues[6];
                    }
                    else
                    {
                        prevTimelineColor = (int)allEvents[22][0].eventValues[6];
                    }
                    nextTimelineColor = (int)nextKF.eventValues[6];
                }
                else
                {
                    var finalKF = allEvents[22][allEvents[22].Count - 1];

                    int a = allEvents[22].Count - 2;
                    if (a < 0)
                    {
                        a = 0;
                    }
                    prevTimelineColor = (int)allEvents[22][a].eventValues[6];
                    nextTimelineColor = (int)finalKF.eventValues[6];
                }
            }
        }

        public void LerpBloomColor()
        {
            Color previous = RTHelpers.BeatmapTheme.effectColors.Count > prevBloomColor && prevBloomColor > -1 ? RTHelpers.BeatmapTheme.effectColors[prevBloomColor] : Color.white;
            Color next = RTHelpers.BeatmapTheme.effectColors.Count > nextBloomColor && nextBloomColor > -1 ? RTHelpers.BeatmapTheme.effectColors[nextBloomColor] : Color.white;

            LSEffectsManager.inst.bloom.color.Override(Color.Lerp(previous, next, bloomColor));
        }

        public void LerpVignetteColor()
        {
            Color previous = RTHelpers.BeatmapTheme.effectColors.Count > prevVignetteColor && prevVignetteColor > -1 ? RTHelpers.BeatmapTheme.effectColors[prevVignetteColor] : Color.black;
            Color next = RTHelpers.BeatmapTheme.effectColors.Count > nextVignetteColor && nextVignetteColor > -1 ? RTHelpers.BeatmapTheme.effectColors[nextVignetteColor] : Color.black;

            LSEffectsManager.inst.vignette.color.Override(Color.Lerp(previous, next, vignetteColor));
        }

        public void LerpGradientColor1()
        {
            Color previous = RTHelpers.BeatmapTheme.effectColors.Count > prevGradientColor1 && prevGradientColor1 > -1 ? RTHelpers.BeatmapTheme.effectColors[prevGradientColor1] : new Color(0f, 0.8f, 0.56f, 0.5f);
            Color next = RTHelpers.BeatmapTheme.effectColors.Count > nextGradientColor1 && nextGradientColor1 > -1 ? RTHelpers.BeatmapTheme.effectColors[nextGradientColor1] : new Color(0f, 0.8f, 0.56f, 0.5f);

            RTEffectsManager.inst.gradient.color1.Override(Color.Lerp(previous, next, gradientColor1));
        }

        public void LerpGradientColor2()
        {
            Color previous = RTHelpers.BeatmapTheme.effectColors.Count > prevGradientColor2 && prevGradientColor2 > -1 ? RTHelpers.BeatmapTheme.effectColors[prevGradientColor2] : new Color(0.81f, 0.37f, 1f, 0.5f);
            Color next = RTHelpers.BeatmapTheme.effectColors.Count > nextGradientColor2 && nextGradientColor2 > -1 ? RTHelpers.BeatmapTheme.effectColors[nextGradientColor2] : new Color(0.81f, 0.37f, 1f, 0.5f);

            RTEffectsManager.inst.gradient.color2.Override(Color.Lerp(previous, next, gradientColor2));
        }

        public void LerpBGColor()
        {
            var beatmapTheme = RTHelpers.BeatmapTheme;

            Color previous = beatmapTheme.effectColors.Count > prevBGColor && prevBGColor > -1 ? beatmapTheme.effectColors[prevBGColor] : beatmapTheme.backgroundColor;
            Color next = beatmapTheme.effectColors.Count > nextBGColor && nextBGColor > -1 ? beatmapTheme.effectColors[nextBGColor] : beatmapTheme.backgroundColor;

            float num = bgColor;
            if (float.IsNaN(num) || num < 0f)
            {
                num = 0f;
            }

            EventsCorePlugin.bgColorToLerp = Color.Lerp(previous, next, num);
        }

        public void LerpTimelineColor()
        {
            var beatmapTheme = RTHelpers.BeatmapTheme;

            Color previous = beatmapTheme.effectColors.Count > prevTimelineColor && prevTimelineColor > -1 ? beatmapTheme.effectColors[prevTimelineColor] : beatmapTheme.guiColor;
            Color next = beatmapTheme.effectColors.Count > nextTimelineColor && nextTimelineColor > -1 ? beatmapTheme.effectColors[nextTimelineColor] : beatmapTheme.guiColor;

            float num = timelineColor;
            if (float.IsNaN(num) || num < 0f)
            {
                num = 0f;
            }

            EventsCorePlugin.timelineColorToLerp = Color.Lerp(previous, next, num);
        }

        #endregion

        #region Update Methods

        public void updateEvents(int currentEvent)
        {
            SetupShake();
            EventManager.inst.eventSequence.Kill();
            EventManager.inst.shakeSequence.Kill();
            EventManager.inst.themeSequence.Kill();
            EventManager.inst.eventSequence = null;
            EventManager.inst.shakeSequence = null;
            EventManager.inst.themeSequence = null;
            for (int i = 0; i < DataManager.inst.gameData.eventObjects.allEvents.Count; i++)
            {
                for (int j = 0; j < DataManager.inst.gameData.eventObjects.allEvents[i].Count; j++)
                {
                    DataManager.inst.gameData.eventObjects.allEvents[i][j].active = false;
                }
            }
        }

        public IEnumerator updateEvents()
        {
            SetupShake();
            EventManager.inst.eventSequence.Kill();
            EventManager.inst.shakeSequence.Kill();
            EventManager.inst.themeSequence.Kill();
            EventManager.inst.eventSequence = null;
            EventManager.inst.shakeSequence = null;
            EventManager.inst.themeSequence = null;
            DOTween.Kill(false, false);
            for (int i = 0; i < DataManager.inst.gameData.eventObjects.allEvents.Count; i++)
            {
                for (int j = 0; j < DataManager.inst.gameData.eventObjects.allEvents[i].Count; j++)
                {
                    DataManager.inst.gameData.eventObjects.allEvents[i][j].active = false;
                }
            }
            yield break;
        }

        public void updateShake()
        {
            var vector = EventManager.inst.shakeVector * EventManager.inst.shakeMultiplier;
            vector.x *= shakeX == 0f && shakeY == 0f ? 1f : shakeX;
            vector.y *= shakeX == 0f && shakeY == 0f ? 1f : shakeY;
            vector.z = 0f;

            if (float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z))
                vector = Vector3.zero;

            if (!float.IsNaN(camOffsetX) && !float.IsNaN(camOffsetY))
                EventManager.inst.camParent.transform.localPosition = vector + new Vector3(camOffsetX, camOffsetY, 0f);
        }

        // 0 - 0
        public static void updateCameraPositionX(float x) => EventManager.inst.camPos.x = x;
        // 0 - 1
        public static void updateCameraPositionY(float x) => EventManager.inst.camPos.y = x;

        // 1 - 0
        public static void updateCameraZoom(float x) => EventManager.inst.camZoom = x;

        // 2 - 0
        public static void updateCameraRotation(float x) => EventManager.inst.camRot = x;

        // 3 - 0
        public static void updateCameraShakeMultiplier(float x) => EventManager.inst.shakeMultiplier = x;

        // 3 - 1
        public static void updateCameraShakeX(float x) => inst.shakeX = x;

        // 3 - 2
        public static void updateCameraShakeY(float x) => inst.shakeY = x;

        // 4 - 0
        public static void updateTheme(float x)
        {
            inst.themeLerp = x;
            var beatmapTheme = BeatmapTheme.DeepCopy((BeatmapTheme)GameManager.inst.LiveTheme);

            ((BeatmapTheme)GameManager.inst.LiveTheme).Lerp((BeatmapTheme)DataManager.inst.GetTheme(EventManager.inst.LastTheme), (BeatmapTheme)DataManager.inst.GetTheme(EventManager.inst.NewTheme), x);

            if (beatmapTheme != (BeatmapTheme)GameManager.inst.LiveTheme)
                GameManager.inst.UpdateTheme();
        }

        // 5 - 0
        public static void updateCameraChromatic(float x) => EventManager.inst.camChroma = x;

        // 6 - 0
        public static void updateCameraBloom(float x) => EventManager.inst.camBloom = x;

        // 6 - 1
        public static void updateCameraBloomDiffusion(float x) => LSEffectsManager.inst.bloom.diffusion.Override(x);

        // 6 - 2
        public static void updateCameraBloomThreshold(float x) => LSEffectsManager.inst.bloom.threshold.Override(x);

        // 6 - 3
        public static void updateCameraBloomAnamorphicRatio(float x) => LSEffectsManager.inst.bloom.anamorphicRatio.Override(x);

        // 6 - 4
        public static void updateCameraBloomColor(float x) => inst.bloomColor = x;

        // 7 - 0
        public static void updateCameraVignette(float x) => EventManager.inst.vignetteIntensity = x;

        // 7 - 1
        public static void updateCameraVignetteSmoothness(float x) => EventManager.inst.vignetteSmoothness = x;

        // 7 - 2
        public static void updateCameraVignetteRounded(float x) => EventManager.inst.vignetteRounded = x;

        // 7 - 3
        public static void updateCameraVignetteRoundness(float x) => EventManager.inst.vignetteRoundness = x;

        // 7 - 4
        public static void updateCameraVignetteCenterX(float x) => EventManager.inst.vignetteCenter.x = x;

        // 7 - 5
        public static void updateCameraVignetteCenterY(float x) => EventManager.inst.vignetteCenter.y = x;

        // 7 - 6
        public static void updateCameraVignetteColor(float x) => inst.vignetteColor = x;

        // 8 - 0
        public static void updateCameraLens(float x) => EventManager.inst.lensDistortIntensity = x;

        // 8 - 1
        public static void updateCameraLensCenterX(float x) => LSEffectsManager.inst.lensDistort.centerX.Override(x);

        // 8 - 2
        public static void updateCameraLensCenterY(float x) => LSEffectsManager.inst.lensDistort.centerY.Override(x);

        // 8 - 3
        public static void updateCameraLensIntensityX(float x) => LSEffectsManager.inst.lensDistort.intensityX.Override(x);

        // 8 - 4
        public static void updateCameraLensIntensityY(float x) => LSEffectsManager.inst.lensDistort.intensityY.Override(x);

        // 8 - 5
        public static void updateCameraLensScale(float x) => LSEffectsManager.inst.lensDistort.scale.Override(x);

        // 9 - 0
        public static void updateCameraGrain(float x) => EventManager.inst.grainIntensity = x;

        // 9 - 1
        public static void updateCameraGrainColored(float _colored) => EventManager.inst.grainColored = _colored;

        // 9 - 2
        public static void updateCameraGrainSize(float x) => EventManager.inst.grainSize = x;

        // 10 - 0
        public static void updateCameraHueShift(float x) => inst.colorGradingHueShift = x;

        // 10 - 1
        public static void updateCameraContrast(float x) => inst.colorGradingContrast = x;

        // 10 - 2
        public static void updateCameraGammaX(float x) => inst.colorGradingGamma.x = x;

        // 10 - 3
        public static void updateCameraGammaY(float x) => inst.colorGradingGamma.y = x;

        // 10 - 4
        public static void updateCameraGammaZ(float x) => inst.colorGradingGamma.z = x;

        // 10 - 5
        public static void updateCameraGammaW(float x) => inst.colorGradingGamma.w = x;

        // 10 - 6
        public static void updateCameraSaturation(float x) => inst.colorGradingSaturation = x;

        // 10 - 7
        public static void updateCameraTemperature(float x) => inst.colorGradingTemperature = x;

        // 10 - 8
        public static void updateCameraTint(float x) => inst.colorGradingTint = x;

        // 11 - 0
        public static void updateCameraRippleStrength(float x) => inst.ripplesStrength = x;

        // 11 - 1
        public static void updateCameraRippleSpeed(float x) => inst.ripplesSpeed = x;

        // 11 - 2
        public static void updateCameraRippleDistance(float x) => inst.ripplesDistance = Mathf.Clamp(x, 0.0001f, float.PositiveInfinity);

        // 11 - 3
        public static void updateCameraRippleHeight(float x) => inst.ripplesHeight = x;

        // 11 - 4
        public static void updateCameraRippleWidth(float x) => inst.ripplesWidth = x;

        // 12 - 0
        public static void updateCameraRadialBlurIntensity(float x) => inst.radialBlurIntensity = x;

        // 12 - 1
        public static void updateCameraRadialBlurIterations(float x) => inst.radialBlurIterations = Mathf.Clamp((int)x, 1, 30);

        // 13 - 0
        public static void updateCameraColorSplit(float x) => inst.colorSplitOffset = x;

        // 14 - 0
        public static void updateCameraOffsetX(float x) => inst.camOffsetX = x;

        // 14 - 1
        public static void updateCameraOffsetY(float x) => inst.camOffsetY = x;

        // 15 - 0
        public static void updateCameraGradientIntensity(float x) => inst.gradientIntensity = x;

        // 15 - 1
        public static void updateCameraGradientRotation(float x) => inst.gradientRotation = x;

        // 15 - 2
        public static void updateCameraGradientColor1(float x) => inst.gradientColor1 = x;

        // 15 - 3
        public static void updateCameraGradientColor2(float x) => inst.gradientColor2 = x;

        // 15 - 4
        public static void updateCameraGradientMode(float x) => RTEffectsManager.inst.gradient.blendMode.Override((SCPE.Gradient.BlendMode)(int)x);

        // 16 - 0
        public static void updateCameraDoubleVision(float x) => inst.doubleVision = x;

        // 17 - 0
        public static void updateCameraScanLinesIntensity(float x) => inst.scanLinesIntensity = x;

        // 17 - 1
        public static void updateCameraScanLinesAmount(float x) => inst.scanLinesAmount = x;

        // 17 - 2
        public static void updateCameraScanLinesSpeed(float x) => inst.scanLinesSpeed = x;

        // 18 - 0
        public static void updateCameraBlurAmount(float x) => LSEffectsManager.inst.blur.amount.Override(!EventsCorePlugin.ShowFX.Value ? 0f : x);

        // 18 - 1
        public static void updateCameraBlurIterations(float x) => LSEffectsManager.inst.blur.iterations.Override(Mathf.Clamp((int)x, 1, 30));

        // 19 - 0
        public static void updateCameraPixelize(float x) => inst.pixel = Mathf.Clamp(x, 0f, 0.99999f);

        // 20 - 0
        public static void updateCameraBGColor(float x) => inst.bgColor = x;

        // 21 - 0
        public static void updateCameraInvert(float x) => inst.invertAmount = x;

        // 22 - 0
        public static void updateTimelineActive(float x)
        {
            var active = false;

            if ((int)x == 0)
            {
                active = true;
            }
            if ((int)x == 1)
            {
                active = false;
            }

            var zen = false;
            if (DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) == 0 || EditorManager.inst != null)
            {
                zen = true;
            }

            inst.timelineActive = active && !zen || active && EventsCorePlugin.ShowGUI.Value;
        }

        // 22 - 1
        public static void updateTimelinePosX(float x) => inst.timelinePos.x = x;

        // 22 - 2
        public static void updateTimelinePosY(float x) => inst.timelinePos.y = x;

        // 22 - 3
        public static void updateTimelineScaX(float x) => inst.timelineSca.x = x;

        // 22 - 4
        public static void updateTimelineScaY(float x) => inst.timelineSca.y = x;

        // 22 - 5
        public static void updateTimelineRot(float x) => inst.timelineRot = x;

        // 22 - 6
        public static void updateTimelineColor(float x) => inst.timelineColor = x;

        // 23 - 0
        public static void updatePlayerActive(float x)
        {
            var active = false;

            if ((int)x == 0)
            {
                active = true;
            }
            else if ((int)x == 1)
            {
                active = false;
            }

            var zen = false;
            if (DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) == 0 || EditorManager.inst != null)
            {
                zen = true;
            }

            GameManager.inst.players.SetActive(active && !zen || active && EventsCorePlugin.ShowGUI.Value);
        }

        // 23 - 1
        public static void updatePlayerMoveable(float x)
        {
            //for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
            //{
            //    if (GameObject.Find(string.Format("Player {0}/Player", i + 1)))
            //    {
            //        inst.playersCanMove = (int)x == 0;
            //        var rt = GameObject.Find(string.Format("Player {0}", i + 1)).GetComponentByName("RTPlayer");
            //        if (rt != null)
            //        {
            //            rt.GetType().GetProperty("CanMove").SetValue(rt, inst.playersCanMove);
            //        }
            //        else
            //        {
            //            GameObject.Find(string.Format("Player {0}", i + 1)).GetComponent<Player>().CanMove = inst.playersCanMove;
            //        }
            //    }
            //}

            inst.playersCanMove = (int)x == 0;
            foreach (var customPlayer in PlayerManager.Players)
            {
                if (customPlayer.Player)
                {
                    customPlayer.Player.CanMove = inst.playersCanMove;
                    customPlayer.Player.CanRotate = inst.playersCanMove;
                }
            }
        }

        // 23 - 2
        public static void updatePlayerPositionX(float x) => inst.playerPositionX = x;

        // 23 - 3
        public static void updatePlayerPositionY(float x) => inst.playerPositionY = x;

        // 23 - 4
        public static void updatePlayerRotation(float x) => inst.playerRotation = x;

        // 24 - 0
        public static void updateDelayTrackerActive(float x) => inst.delayTracker.active = (int)x == 1;

        // 24 - 1
        public static void updateDelayTrackerMove(float x) => inst.delayTracker.move = (int)x == 1;

        // 24 - 2
        public static void updateDelayTrackerRotate(float x) => inst.delayTracker.rotate = (int)x == 1;

        // 24 - 3
        public static void updateDelayTrackerSharpness(float x) => inst.delayTracker.followSharpness = Mathf.Clamp(x, 0.001f, 1f);

        // 24 - 4
        public static void updateDelayTrackerOffset(float x) => inst.delayTracker.offset = x;

        // 24 - 5
        public static void updateDelayTrackerLimitLeft(float x) => inst.delayTracker.limitLeft = x;

        // 24 - 6
        public static void updateDelayTrackerLimitRight(float x) => inst.delayTracker.limitRight = x;

        // 24 - 7
        public static void updateDelayTrackerLimitUp(float x) => inst.delayTracker.limitUp = x;

        // 24 - 8
        public static void updateDelayTrackerLimitDown(float x) => inst.delayTracker.limitDown = x;

        // 24 - 9
        public static void updateDelayTrackerAnchor(float x) => inst.delayTracker.anchor = x;

        // 25 - 0
        public static void updateAudioPitch(float x) => AudioManager.inst.pitch = Mathf.Clamp(x, 0.001f, 10f) * GameManager.inst.getPitch() * inst.pitchOffset;

        // 25 - 1
        public static void updateAudioVolume(float x) => inst.audioVolume = Mathf.Clamp(x, 0f, 1f);

        #endregion

        #region Variables

        public float themeLerp;

        public float camOffsetX;
        public float camOffsetY;

        public float audioVolume = 1f;

        public float playerRotation;
        public float playerPositionX;
        public float playerPositionY;

        public bool playersCanMove = true;
        public bool playersActive = true;

        public bool timelineActive = true;
        public Vector2 timelinePos;
        public Vector2 timelineSca;
        public float timelineRot;
        public float timelineColor;
        public int prevTimelineColor = 18;
        public int nextTimelineColor = 18;

        public float bgColor;
        public int prevBGColor = 18;
        public int nextBGColor = 18;

        public float invertAmount;

        public float bloomColor;
        public int prevBloomColor = 18;
        public int nextBloomColor = 18;

        public float vignetteColor;
        public int prevVignetteColor = 18;
        public int nextVignetteColor = 18;

        public float shakeX;
        public float shakeY;

        public float pixel;

        public float colorGradingHueShift;
        public float colorGradingContrast;
        public Vector4 colorGradingGamma;
        public float colorGradingSaturation;
        public float colorGradingTemperature;
        public float colorGradingTint;

        public float gradientIntensity;
        public float gradientColor1;
        public int prevGradientColor1;
        public int nextGradientColor1;
        public float gradientColor2;
        public int prevGradientColor2;
        public int nextGradientColor2;
        public float gradientRotation;

        public float doubleVision;

        public float radialBlurIntensity;
        public int radialBlurIterations;

        public float scanLinesIntensity;
        public float scanLinesAmount;
        public float scanLinesSpeed;

        public float sharpen;

        public float colorSplitOffset;

        public float dangerIntensity;
        public Color dangerColor;
        public float dangerSize;

        public float ripplesStrength;
        public float ripplesSpeed;
        public float ripplesDistance;
        public float ripplesHeight;
        public float ripplesWidth;

        #endregion

        #region Editor Offsets

        public float EditorSpeed
        {
            get
            {
                return EventsCorePlugin.EditorCamSpeed.Value;
            }
        }

        private Vector2 editorOffset = Vector2.zero;
        public float editorZoom = 20f;
        public float editorRotate = 0f;
        public Vector2 editorPerRotate = Vector2.zero;

        #endregion

        #region Offsets

        public float pitchOffset = 1f;

        List<List<float>> ResetOffsets()
        {
            return new List<List<float>>
        {
            new List<float>
            {
                0f, // Move X
                0f, // Move Y
            },
            new List<float>
            {
                0f, // Zoom
            },
            new List<float>
            {
                0f, // Rotate
            },
            new List<float>
            {
                0f, // Shake
                0f, // Shake X
                0f, // Shake Y
            },
            new List<float>
            {
                0f, // Theme
            },
            new List<float>
            {
                0f, // Chromatic
            },
            new List<float>
            {
                0f, // Bloom Intensity
                0f, // Bloom Diffusion
                0f, // Bloom Threshold
                0f, // Bloom Anamorphic Ratio
                0f, // Bloom Color
            },
            new List<float>
            {
                0f, // Vignette Intensity
                0f, // Vignette Smoothness
                0f, // Vignette Rounded
                0f, // Vignette Roundness
                0f, // Vignette Center X
                0f, // Vignette Center Y
                0f, // Vignette Color
            },
            new List<float>
            {
                0f, // Lens Intensity
                0f, // Lens Center X
                0f, // Lens Center Y
                0f, // Lens Intensity X
                0f, // Lens Intensity Y
                0f, // Lens Scale
            },
            new List<float>
            {
                0f, // Grain Intensity
                0f, // Grain Colored
                0f, // Grain Size
            },
            new List<float>
            {
                0f, // ColorGrading Hueshift
                0f, // ColorGrading Contrast
                0f, // ColorGrading Gamma X
                0f, // ColorGrading Gamma Y
                0f, // ColorGrading Gamma Z
                0f, // ColorGrading Gamma W
                0f, // ColorGrading Saturation
                0f, // ColorGrading Temperature
                0f, // ColorGrading Tint
            },
            new List<float>
            {
                0f, // Ripples Strength
                0f, // Ripples Speed
                0f, // Ripples Distance
                0f, // Ripples Height
                0f, // Ripples Width
            },
            new List<float>
            {
                0f, // RadialBlur Intensity
                0f, // RadialBlur Iterations
            },
            new List<float>
            {
                0f, // ColorSplit Offset
            },
            new List<float>
            {
                0f, // Camera Offset X
                0f, // Camera Offset Y
            },
            new List<float>
            {
                0f, // Gradient Intensity
                0f, // Gradient Rotation
            },
            new List<float>
            {
                0f, // DoubleVision Intensity
            },
            new List<float>
            {
                0f, // ScanLines Intensity
                0f, // ScanLines Amount
                0f, // ScanLines Speed
            },
            new List<float>
            {
                0f, // Blur Amount
                0f, // Blur Iterations
            },
            new List<float>
            {
                0f, // Pixelize Amount
            },
            new List<float>
            {
                0f, // BG Color
            },
            new List<float>
            {
                0f, // Invert Amount
            },
            new List<float>
            {
                0f, // Timeline Active
                0f, // Timeline Pos X
                0f, // Timeline Pos Y
                0f, // Timeline Sca X
                0f, // Timeline Sca Y
                0f, // Timeline Rot
                0f, // Timeline Color
            },
            new List<float>
            {
                0f, // Player Active
                0f, // Player Moveable
                0f, // Player Velocity
                0f, // Player Rotation
            },
            new List<float>
            {
                0f, // Follow Player Active
                0f, // Follow Player Move
                0f, // Follow Player Rotate
                0f, // Follow Player Sharpness
                0f, // Follow Player Offset
                0f, // Follow Player Limit Left
                0f, // Follow Player Limit Right
                0f, // Follow Player Limit Up
                0f, // Follow Player Limit Down
                0f, // Follow Player Anchor
            },
            new List<float>
            {
                0f, // Audio Pitch
                0f, // Audio Volume
            },
        };
        }

        List<List<float>> offsets = new List<List<float>>
        {
            new List<float>
            {
                0f, // Move X
                0f, // Move Y
            },
            new List<float>
            {
                0f, // Zoom
            },
            new List<float>
            {
                0f, // Rotate
            },
            new List<float>
            {
                0f, // Shake
                0f, // Shake X
                0f, // Shake Y
            },
            new List<float>
            {
                0f, // Theme
            },
            new List<float>
            {
                0f, // Chromatic
            },
            new List<float>
            {
                0f, // Bloom Intensity
                0f, // Bloom Diffusion
                0f, // Bloom Threshold
                0f, // Bloom Anamorphic Ratio
                0f, // Bloom Color
            },
            new List<float>
            {
                0f, // Vignette Intensity
                0f, // Vignette Smoothness
                0f, // Vignette Rounded
                0f, // Vignette Roundness
                0f, // Vignette Center X
                0f, // Vignette Center Y
                0f, // Vignette Color
            },
            new List<float>
            {
                0f, // Lens Intensity
                0f, // Lens Center X
                0f, // Lens Center Y
                0f, // Lens Intensity X
                0f, // Lens Intensity Y
                0f, // Lens Scale
            },
            new List<float>
            {
                0f, // Grain Intensity
                0f, // Grain Colored
                0f, // Grain Size
            },
            new List<float>
            {
                0f, // ColorGrading Hueshift
                0f, // ColorGrading Contrast
                0f, // ColorGrading Gamma X
                0f, // ColorGrading Gamma Y
                0f, // ColorGrading Gamma Z
                0f, // ColorGrading Gamma W
                0f, // ColorGrading Saturation
                0f, // ColorGrading Temperature
                0f, // ColorGrading Tint
            },
            new List<float>
            {
                0f, // Ripples Strength
                0f, // Ripples Speed
                0f, // Ripples Distance
                0f, // Ripples Height
                0f, // Ripples Width
            },
            new List<float>
            {
                0f, // RadialBlur Intensity
                0f, // RadialBlur Iterations
            },
            new List<float>
            {
                0f, // ColorSplit Offset
            },
            new List<float>
            {
                0f, // Camera Offset X
                0f, // Camera Offset Y
            },
            new List<float>
            {
                0f, // Gradient Intensity
                0f, // Gradient Rotation
            },
            new List<float>
            {
                0f, // DoubleVision Intensity
            },
            new List<float>
            {
                0f, // ScanLines Intensity
                0f, // ScanLines Amount
                0f, // ScanLines Speed
            },
            new List<float>
            {
                0f, // Blur Amount
                0f, // Blur Iterations
            },
            new List<float>
            {
                0f, // Pixelize Amount
            },
            new List<float>
            {
                0f, // BG Color
            },
            new List<float>
            {
                0f, // Invert Amount
            },
            new List<float>
            {
                0f, // Timeline Active
                0f, // Timeline Pos X
                0f, // Timeline Pos Y
                0f, // Timeline Sca X
                0f, // Timeline Sca Y
                0f, // Timeline Rot
                0f, // Timeline Color
            },
            new List<float>
            {
                0f, // Player Active
                0f, // Player Moveable
                0f, // Player Velocity
                0f, // Player Rotation
            },
            new List<float>
            {
                0f, // Follow Player Active
                0f, // Follow Player Move
                0f, // Follow Player Rotate
                0f, // Follow Player Sharpness
                0f, // Follow Player Offset
                0f, // Follow Player Limit Left
                0f, // Follow Player Limit Right
                0f, // Follow Player Limit Up
                0f, // Follow Player Limit Down
                0f, // Follow Player Anchor
            },
            new List<float>
            {
                0f, // Audio Pitch
                0f, // Audio Volume
            },
        };

        #endregion

        #region Delegates

        public delegate void KFDelegate(float t);

        public KFDelegate[][] events = new KFDelegate[26][]
        {
                new KFDelegate[]
                {
                    updateCameraPositionX,
                    updateCameraPositionY,
                },
                new KFDelegate[]
                {
                    updateCameraZoom
                },
                new KFDelegate[]
                {
                    updateCameraRotation
                },
                new KFDelegate[]
                {
                    updateCameraShakeMultiplier,
                    updateCameraShakeX,
                    updateCameraShakeY
                },
                new KFDelegate[]
                {
                    updateTheme
                },
                new KFDelegate[]
                {
                    updateCameraChromatic
                },
                new KFDelegate[]
                {
                    updateCameraBloom,
                    updateCameraBloomDiffusion,
                    updateCameraBloomThreshold,
                    updateCameraBloomAnamorphicRatio,
                    updateCameraBloomColor
                },
                new KFDelegate[]
                {
                    updateCameraVignette,
                    updateCameraVignetteSmoothness,
                    updateCameraVignetteRounded,
                    updateCameraVignetteRoundness,
                    updateCameraVignetteCenterX,
                    updateCameraVignetteCenterY,
                    updateCameraVignetteColor
                },
                new KFDelegate[]
                {
                    updateCameraLens,
                    updateCameraLensCenterX,
                    updateCameraLensCenterY,
                    updateCameraLensIntensityX,
                    updateCameraLensIntensityY,
                    updateCameraLensScale
                },
                new KFDelegate[]
                {
                    updateCameraGrain,
                    updateCameraGrainColored,
                    updateCameraGrainSize
                },
                new KFDelegate[]
                {
                    updateCameraHueShift,
                    updateCameraContrast,
                    updateCameraGammaX,
                    updateCameraGammaY,
                    updateCameraGammaZ,
                    updateCameraGammaW,
                    updateCameraSaturation,
                    updateCameraTemperature,
                    updateCameraTint
                },
                new KFDelegate[]
                {
                    updateCameraRippleStrength,
                    updateCameraRippleSpeed,
                    updateCameraRippleDistance,
                    updateCameraRippleHeight,
                    updateCameraRippleWidth
                },
                new KFDelegate[]
                {
                    updateCameraRadialBlurIntensity,
                    updateCameraRadialBlurIterations
                },
                new KFDelegate[]
                {
                    updateCameraColorSplit
                },
                new KFDelegate[]
                {
                    updateCameraOffsetX,
                    updateCameraOffsetY
                },
                new KFDelegate[]
                {
                    updateCameraGradientIntensity,
                    updateCameraGradientRotation,
                    updateCameraGradientColor1,
                    updateCameraGradientColor2,
                    updateCameraGradientMode
                },
                new KFDelegate[]
                {
                    updateCameraDoubleVision
                },
                new KFDelegate[]
                {
                    updateCameraScanLinesIntensity,
                    updateCameraScanLinesAmount,
                    updateCameraScanLinesSpeed
                },
                new KFDelegate[]
                {
                    updateCameraBlurAmount,
                    updateCameraBlurIterations
                },
                new KFDelegate[]
                {
                    updateCameraPixelize
                },
                new KFDelegate[]
                {
                    updateCameraBGColor
                },
                new KFDelegate[]
                {
                    updateCameraInvert
                },
                new KFDelegate[]
                {
                    updateTimelineActive,
                    updateTimelinePosX,
                    updateTimelinePosY,
                    updateTimelineScaX,
                    updateTimelineScaY,
                    updateTimelineRot,
                    updateTimelineColor
                },
                new KFDelegate[]
                {
                    updatePlayerActive,
                    updatePlayerMoveable,
                    updatePlayerPositionX,
                    updatePlayerPositionY,
                    updatePlayerRotation
                },
                new KFDelegate[]
                {
                    updateDelayTrackerActive,
                    updateDelayTrackerMove,
                    updateDelayTrackerRotate,
                    updateDelayTrackerSharpness,
                    updateDelayTrackerOffset,
                    updateDelayTrackerLimitLeft,
                    updateDelayTrackerLimitRight,
                    updateDelayTrackerLimitUp,
                    updateDelayTrackerLimitDown,
                    updateDelayTrackerAnchor,
                },
                new KFDelegate[]
                {
                    updateAudioPitch,
                    updateAudioVolume
                }
        };

        public float shakeSpeed = 1f;
        public EaseFunction shakeEase = Ease.Linear;

        public void SetupShake()
        {
            if (shakeSequence != null)
                shakeSequence = null;

            var list = new List<IKeyframe<Vector2>>();

            float t = 0f;
            while (t < AudioManager.inst.CurrentAudioSource.clip.length)
            {
                list.Add(new Vector2Keyframe(t, new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)), shakeEase));
                t += Random.Range(0.08f, 0.14f) / Mathf.Clamp(shakeSpeed, 0.01f, 10f);
            }

            shakeSequence = new Sequence<Vector2>(list);
        }

        public Sequence<Vector2> shakeSequence;

        #endregion
    }
}

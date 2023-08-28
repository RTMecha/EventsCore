using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;
using UnityEngine.EventSystems;

using LSFunctions;

using EventsCore.Functions;

using RTFunctions.Functions;
using RTFunctions.Functions.Managers;

namespace EventsCore
{
    public class RTEventManager : MonoBehaviour
    {
        public DelayTracker delayTracker;

        public static RTEventManager inst;

        void Awake()
        {
            if (inst == null)
            {
                inst = this;
                return;
            }
            if (inst != this)
            {
                Destroy(gameObject);
            }

            LSEffectsManager.inst.vignette.color.overrideState = true;
        }

        //LAYER 1:
        //-Move
        //-Zoom
        //-Rotate
        //-Shake
        //-Theme
        //-Chromatic
        //-Bloom
        //-Vignette
        //-Lens
        //-Grain
        //-ColorGrading
        //-Gradient
        //-RadialBlur
        //-ColorSplit

        //LAYER 2:
        //00 - Cam Offset
        //01 - Gradient
        //02 - DoubleVision
        //03 - Scanlines
        //04 - Blur
        //05 - Pixelize
        //06 - BG (Color with default event color as regular bg color)
        //07 - Screen Overlay
        //08 - Timeline (enabled, pos, sca, rot, col)
        //09 - Player (enabled / active, can move (if can move is false then player can be animated)) & (rotation / direction, move / velocity in direction) & (move speed, boost speed)
        //10 - Follow Player (Use original & delayTracker / homing logic, have to decide which to go for)
        //11 - Music
        //12 - AnalogGlitch
        //13 - DigitalGlitch

        public bool dont = false;
        public float perspectiveZoom = 0.5f;

        public float EditorSpeed
        {
            get
            {
                return EventsCorePlugin.EditorSpeed.Value;
            }
        }

        private Vector2 editorOffset = Vector2.zero;
        public float editorZoom = 20f;
        public float editorRotate = 0f;
        public Vector2 editorPerRotate = Vector2.zero;

        void SpeedHandler()
        {
            if (EventsCorePlugin.AllowCameraEvent.Value)
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

            if (!ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEventOffsets"))
            {
                ModCompatibility.sharedFunctions.Add("EventsCoreEventOffsets", offsets);
            }

            if (ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEventOffsets"))
            {
                offsets = (List<List<float>>)ModCompatibility.sharedFunctions["EventsCoreEventOffsets"];
            }
        }

        void Update()
        {
            SpeedHandler(); updateShake(); FunctionsHandler();
            GameManager.inst.timeline.SetActive(timelineActive);

            if (RTEffectsManager.inst && EventManager.inst && GameManager.inst && (GameManager.inst.gameState == GameManager.State.Playing || GameManager.inst.gameState == GameManager.State.Reversing))
            {
                InputDataManager.inst.SetAllControllerRumble(EventManager.inst.shakeMultiplier);

                #region Camera

                if (float.IsNaN(EventManager.inst.camRot))
                    EventManager.inst.camRot = 0f;
                if (float.IsNaN(EventManager.inst.camZoom) || EventManager.inst.camZoom == 0f)
                    EventManager.inst.camZoom = 20f;

                if (!EventsCorePlugin.AllowCameraEvent.Value)
                    EventManager.inst.cam.orthographicSize = EventManager.inst.camZoom + offsets[1][0];
                else if (EditorSpeed != 0f)
                    EventManager.inst.cam.orthographicSize = editorZoom;

                if (!float.IsNaN(EventManager.inst.camRot) && !EventsCorePlugin.AllowCameraEvent.Value)
                    EventManager.inst.camParent.transform.rotation = Quaternion.Euler(new Vector3(EventManager.inst.camParent.transform.rotation.x, EventManager.inst.camParent.transform.rotation.y, EventManager.inst.camRot + offsets[2][0]));
                else if (!float.IsNaN(editorRotate))
                    EventManager.inst.camParent.transform.rotation = Quaternion.Euler(new Vector3(editorPerRotate.x, editorPerRotate.y, editorRotate));

                if (EditorManager.inst == null || !EventsCorePlugin.AllowCameraEvent.Value)
                    EventManager.inst.camParentTop.transform.localPosition = new Vector3(EventManager.inst.camPos.x + offsets[0][0], EventManager.inst.camPos.y + offsets[0][1], -10f);
                else
                    EventManager.inst.camParentTop.transform.localPosition = new Vector3(editorOffset.x, editorOffset.y, -10f);

                EventManager.inst.camPer.fieldOfView = 50f;

                if (!EventsCorePlugin.AllowCameraEvent.Value)
                    EventManager.inst.camPer.transform.position = new Vector3(EventManager.inst.camPer.transform.position.x, EventManager.inst.camPer.transform.position.y, -(EventManager.inst.camZoom + offsets[1][0]) / perspectiveZoom);
                else
                    EventManager.inst.camPer.transform.position = new Vector3(EventManager.inst.camPer.transform.position.x, EventManager.inst.camPer.transform.position.y, -editorZoom / perspectiveZoom);

                EventManager.inst.camPer.nearClipPlane = -EventManager.inst.camPer.transform.position.z + 10f;

                #endregion

                #region Updates

                if (!float.IsNaN(EventManager.inst.camChroma))
                    LSEffectsManager.inst.UpdateChroma(EventManager.inst.camChroma + offsets[5][0]);
                if (!float.IsNaN(EventManager.inst.camBloom))
                    LSEffectsManager.inst.UpdateBloom(EventManager.inst.camBloom + offsets[6][0]);
                if (!float.IsNaN(EventManager.inst.vignetteIntensity))
                    LSEffectsManager.inst.UpdateVignette(EventManager.inst.vignetteIntensity + offsets[7][0], EventManager.inst.vignetteSmoothness + offsets[7][1], Mathf.RoundToInt(EventManager.inst.vignetteRounded + offsets[7][2]) == 1, EventManager.inst.vignetteRoundness + offsets[7][3], EventManager.inst.vignetteCenter + new Vector2(offsets[7][4], offsets[7][5]));
                if (!float.IsNaN(EventManager.inst.lensDistortIntensity))
                    LSEffectsManager.inst.UpdateLensDistort(EventManager.inst.lensDistortIntensity + offsets[8][0]);
                if (!float.IsNaN(EventManager.inst.grainIntensity))
                    LSEffectsManager.inst.UpdateGrain(EventManager.inst.grainIntensity + offsets[9][0], Mathf.RoundToInt(EventManager.inst.grainColored + offsets[9][1]) == 1, EventManager.inst.grainSize + offsets[9][2]);
                if (!float.IsNaN(pixel))
                    LSEffectsManager.inst.pixelize.amount.Override(pixel);

                //New effects
                if (!float.IsNaN(colorGradingHueShift))
                    RTEffectsManager.inst.UpdateColorGrading(colorGradingHueShift + offsets[10][0], colorGradingContrast + offsets[10][1], colorGradingGamma + new Vector4(offsets[10][2], offsets[10][3], offsets[10][4], offsets[10][5]), colorGradingSaturation + offsets[10][6], colorGradingTemperature + offsets[10][7], colorGradingTint + offsets[10][7]);
                if (!float.IsNaN(gradientIntensity))
                    RTEffectsManager.inst.UpdateGradient(gradientIntensity + offsets[15][0], gradientRotation + offsets[15][1]);
                if (!float.IsNaN(ripplesStrength))
                    RTEffectsManager.inst.UpdateRipples(ripplesStrength + offsets[11][0], ripplesSpeed + offsets[11][1], ripplesDistance + offsets[11][2], ripplesHeight + offsets[11][3], ripplesWidth + offsets[11][4]);
                if (!float.IsNaN(doubleVision))
                    RTEffectsManager.inst.UpdateDoubleVision(doubleVision + offsets[16][0]);
                if (!float.IsNaN(radialBlurIntensity))
                    RTEffectsManager.inst.UpdateRadialBlur(radialBlurIntensity + offsets[12][0], radialBlurIterations + (int)offsets[12][1]);
                if (!float.IsNaN(scanLinesIntensity))
                    RTEffectsManager.inst.UpdateScanlines(scanLinesIntensity, scanLinesAmount, scanLinesSpeed);
                if (!float.IsNaN(sharpen))
                    RTEffectsManager.inst.UpdateSharpen(sharpen);
                if (!float.IsNaN(colorSplitOffset))
                    RTEffectsManager.inst.UpdateColorSplit(colorSplitOffset + offsets[13][0]);
                if (!float.IsNaN(dangerIntensity))
                    RTEffectsManager.inst.UpdateDanger(dangerIntensity, dangerColor, dangerSize);

                if (!float.IsNaN(timelineRot))
                {
                    GameManager.inst.timeline.transform.localPosition = new Vector3(timelinePos.x, timelinePos.y, 0f);
                    GameManager.inst.timeline.transform.localScale = new Vector3(timelineSca.x, timelineSca.y, 1f);
                    GameManager.inst.timeline.transform.eulerAngles = new Vector3(0f, 0f, timelineRot);
                }

                #endregion

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
                if (!float.IsNaN(overlayColor))
                    LerpOverlayColor();
                if (!float.IsNaN(timelineColor))
                    LerpTimelineColor();

                #endregion
            }

            if (GameManager.inst.gameState == GameManager.State.Playing && DataManager.inst.gameData != null && DataManager.inst.gameData.eventObjects != null && DataManager.inst.gameData.eventObjects.allEvents != null && DataManager.inst.gameData.eventObjects.allEvents.Count > 0)
            {
                #region New Sequences

                if (EventManager.inst.eventSequence == null)
                {
                    EventManager.inst.eventSequence = DOTween.Sequence();
                }
                if (EventManager.inst.themeSequence == null)
                {
                    EventManager.inst.themeSequence = DOTween.Sequence();
                }
                if (EventManager.inst.shakeSequence == null)
                {
                    EventManager.inst.shakeSequence = DOTween.Sequence();
                }

                #endregion

                //foreach (var sequence in eventSequences)
                //{
                //    if (sequence.sequence == null)
                //    {
                //        sequence.sequence = DOTween.Sequence();
                //    }
                //}

                var allEvents = DataManager.inst.gameData.eventObjects.allEvents;

                //for (int i = 0; i < allEvents.Count; i++)
                //{
                //    int numb = 0;
                //    for (int j = 0; j < allEvents[i].Count; j++)
                //    {
                //        if (!allEvents[i][j].active)
                //        {
                //            allEvents[i][j].active = true;
                //            int previousIndex = numb - 1;
                //            if (previousIndex < 0)
                //            {
                //                previousIndex = 0;
                //            }

                //            var currentKF = allEvents[i][j];
                //            var previousKF = allEvents[i][previousIndex];

                //            float previousTime = previousKF.eventTime;
                //            float eventDuration = currentKF.eventTime - previousTime;

                //            for (int k = 0; k < currentKF.eventValues.Length; k++)
                //            {
                //                float x = currentKF.eventValues[k];
                //                if (float.IsNaN(x))
                //                {
                //                    x = 0f;
                //                }
                //                if (numb == 0)
                //                {
                //                    eventSequences[i].sequence.Insert(0f, DOTween.To(delegate (float _val)
                //                    {
                //                        updateSequencer(i, k, _val);
                //                    }, x, x, 0f).SetEase(EventManager.inst.customInstantEase));
                //                }
                //            }
                //        }
                //        numb++;
                //    }
                //}

                #region Sequence

                //Move
                int num = 0;
                for (int i = 0; i < allEvents[0].Count; i++)
                {
                    if (!allEvents[0][i].active)
                    {
                        allEvents[0][i].active = true;
                        int previousIndex = num - 1;
                        if (previousIndex < 0)
                        {
                            previousIndex = 0;
                        }
                        var previousKF = allEvents[0][previousIndex];
                        float x = allEvents[0][i].eventValues[0];
                        float y = allEvents[0][i].eventValues[1];
                        if (float.IsNaN(x))
                        {
                            x = 0f;
                        }
                        if (float.IsNaN(y))
                        {
                            y = 0f;
                        }
                        float previousTime = previousKF.eventTime;
                        float eventDuration = allEvents[0][i].eventTime - previousKF.eventTime;
                        new Vector3(x, y, -10f);
                        if (num == 0)
                        {
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraPositionX), x, x, 0f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraPositionY), y, y, 0f).SetEase(EventManager.inst.customInstantEase));
                        }
                        EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraPositionX), previousKF.eventValues[0], x, eventDuration).SetEase(allEvents[0][i].curveType.Animation));
                        EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraPositionY), previousKF.eventValues[1], y, eventDuration).SetEase(allEvents[0][i].curveType.Animation));
                        if (num == allEvents[0].Count - 1)
                        {
                            EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraPositionX), x, x, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraPositionY), y, y, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                        }
                    }
                    num++;
                }
                //Zoom
                num = 0;
                for (int i = 0; i < allEvents[1].Count; i++)
                {
                    if (!allEvents[1][i].active)
                    {
                        allEvents[1][i].active = true;
                        int previousIndex = num - 1;
                        if (previousIndex < 0)
                        {
                            previousIndex = 0;
                        }
                        var previousKF = allEvents[1][previousIndex];
                        float zoom = allEvents[1][i].eventValues[0];
                        if (float.IsNaN(zoom))
                        {
                            zoom = 0f;
                        }
                        float previousTime = previousKF.eventTime;
                        float eventDuration = allEvents[1][i].eventTime - previousKF.eventTime;
                        if (num == 0)
                        {
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraZoom), zoom, zoom, 0f).SetEase(EventManager.inst.customInstantEase));
                        }
                        EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraZoom), previousKF.eventValues[0], zoom, eventDuration).SetEase(allEvents[1][i].curveType.Animation));
                        if (num == allEvents[1].Count - 1)
                        {
                            EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraZoom), zoom, zoom, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                        }
                    }
                    num++;
                }
                //Rotate
                num = 0;
                for (int i = 0; i < allEvents[2].Count; i++)
                {
                    if (!allEvents[2][i].active)
                    {
                        allEvents[2][i].active = true;
                        int previousIndex = num - 1;
                        if (previousIndex < 0)
                        {
                            previousIndex = 0;
                        }
                        DataManager.GameData.EventKeyframe previousKF = allEvents[2][previousIndex];
                        float num10 = allEvents[2][i].eventValues[0];
                        if (float.IsNaN(num10))
                        {
                            num10 = 0f;
                        }
                        float previousTime = previousKF.eventTime;
                        float eventDuration = allEvents[2][i].eventTime - previousKF.eventTime;
                        if (num == 0)
                        {
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraRotation), num10, num10, 0f).SetEase(EventManager.inst.customInstantEase));
                        }
                        EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraRotation), previousKF.eventValues[0], num10, eventDuration).SetEase(allEvents[2][i].curveType.Animation));
                        if (num == allEvents[2].Count - 1)
                        {
                            EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraRotation), num10, num10, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                        }
                    }
                    num++;
                }
                //Shake
                num = 0;
                for (int i = 0; i < allEvents[3].Count; i++)
                {
                    if (!allEvents[3][i].active)
                    {
                        allEvents[3][i].active = true;
                        int previousIndex = num - 1;
                        if (previousIndex < 0)
                        {
                            previousIndex = 0;
                        }
                        DataManager.GameData.EventKeyframe previousKF = allEvents[3][previousIndex];
                        float multiplier = allEvents[3][i].eventValues[0];

                        float x = 1f;
                        float y = 1f;

                        float strength = 3f;
                        int vibrato = 10;
                        float randomness = 90f;

                        if (allEvents[3][i].eventValues.Length > 1)
                        {
                            x = allEvents[3][i].eventValues[1];
                            y = allEvents[3][i].eventValues[2];
                        }
                        if (float.IsNaN(multiplier))
                        {
                            multiplier = 0f;
                        }
                        if (x == 0f && y == 0f)
                        {
                            x = 1f;
                            y = 1f;
                        }
                        float shakeTime = previousKF.eventTime;
                        float eventDuration = allEvents[3][i].eventTime - previousKF.eventTime;
                        if (num == 0)
                        {
                            EventManager.inst.shakeSequence.Insert(0f, DOTween.To(delegate (float x)
                            {
                                EventManager.inst.shakeMultiplier = x;
                            }, 0f, multiplier, 0f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.shakeSequence.Insert(0f, DOTween.Shake(() => Vector3.zero, delegate (Vector3 x)
                            {
                                EventManager.inst.shakeVector = x;
                            }, AudioManager.inst.CurrentAudioSource.clip.length, strength, vibrato, randomness, true, false));
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraShakeX), x, x, 0f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraShakeY), y, y, 0f).SetEase(EventManager.inst.customInstantEase));
                        }
                        EventManager.inst.shakeSequence.Insert(shakeTime, DOTween.To(delegate (float x)
                        {
                            EventManager.inst.shakeMultiplier = x;
                        }, previousKF.eventValues[0], multiplier, eventDuration).SetEase(allEvents[3][i].curveType.Animation));
                        if (previousKF.eventValues.Length > 1)
                        {
                            EventManager.inst.eventSequence.Insert(shakeTime, DOTween.To(new DOSetter<float>(updateCameraShakeX), previousKF.eventValues[1], x, eventDuration).SetEase(allEvents[3][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(shakeTime, DOTween.To(new DOSetter<float>(updateCameraShakeY), previousKF.eventValues[2], y, eventDuration).SetEase(allEvents[3][i].curveType.Animation));
                        }
                        if (num == allEvents[3].Count - 1)
                        {
                            EventManager.inst.shakeSequence.Insert(shakeTime + eventDuration, DOTween.To(delegate (float x)
                            {
                                EventManager.inst.shakeMultiplier = x;
                            }, multiplier, multiplier, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(shakeTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraShakeX), x, x, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(shakeTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraShakeY), y, y, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                        }
                    }
                    num++;
                }
                //Theme
                num = 0;
                for (int i = 0; i < allEvents[4].Count; i++)
                {
                    if (!allEvents[4][i].active)
                    {
                        allEvents[4][i].active = true;
                        int previousIndex = num - 1;
                        if (previousIndex < 0)
                        {
                            previousIndex = 0;
                        }
                        var previousKF = allEvents[4][previousIndex];
                        float theme = allEvents[4][i].eventValues[0];
                        float previousTime = previousKF.eventTime;
                        float eventDuration = allEvents[4][i].eventTime - previousKF.eventTime;
                        if (num == 0)
                        {
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateTheme), 0f, 1f, 0f).SetEase(EventManager.inst.customInstantEase));
                            //EventManager.inst.themeSequence.Insert(0f, DOTween.To(delegate (float x)
                            //{
                            //    EventManager.inst.LastTheme = Mathf.RoundToInt(x);
                            //}, 0f, 0f, 0f).SetEase(EventManager.inst.customInstantEase));
                            //EventManager.inst.themeSequence.Insert(0f, DOTween.To(delegate (float x)
                            //{
                            //    EventManager.inst.NewTheme = Mathf.RoundToInt(x);
                            //}, theme, theme, 0f).SetEase(EventManager.inst.customInstantEase));
                        }
                        if (num < allEvents[4].Count)
                        {
                            EventManager.inst.eventSequence.Insert(previousTime + 0.00001f, DOTween.To(new DOSetter<float>(updateTheme), 0f, 1f, eventDuration).SetEase(allEvents[4][i].curveType.Animation));
                            //EventManager.inst.themeSequence.Insert(previousTime, DOTween.To(delegate (float x)
                            //{
                            //    EventManager.inst.LastTheme = Mathf.RoundToInt(x);
                            //}, previousKF.eventValues[0], previousKF.eventValues[0], eventDuration).SetEase(EventManager.inst.customInstantEase));
                            //EventManager.inst.themeSequence.Insert(previousTime, DOTween.To(delegate (float x)
                            //{
                            //    EventManager.inst.NewTheme = Mathf.RoundToInt(x);
                            //}, theme, theme, eventDuration).SetEase(EventManager.inst.customInstantEase));
                        }
                        if (num == allEvents[4].Count - 1)
                        {
                            EventManager.inst.eventSequence.Insert(previousTime + eventDuration + 0.00001f, DOTween.To(new DOSetter<float>(updateTheme), 1f, 1f, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            //EventManager.inst.themeSequence.Insert(previousTime + eventDuration, DOTween.To(delegate (float x)
                            //{
                            //    EventManager.inst.LastTheme = Mathf.RoundToInt(x);
                            //}, theme, theme, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            //EventManager.inst.themeSequence.Insert(previousTime + eventDuration, DOTween.To(delegate (float x)
                            //{
                            //    EventManager.inst.NewTheme = Mathf.RoundToInt(x);
                            //}, theme, theme, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                        }
                    }
                    num++;
                }
                //Chromatic
                num = 0;
                for (int i = 0; i < allEvents[5].Count; i++)
                {
                    if (!allEvents[5][i].active)
                    {
                        allEvents[5][i].active = true;
                        int previousIndex = num - 1;
                        if (previousIndex < 0)
                        {
                            previousIndex = 0;
                        }
                        var previousKF = allEvents[5][previousIndex];
                        float intensity = allEvents[5][i].eventValues[0];
                        if (float.IsNaN(intensity))
                        {
                            intensity = 0f;
                        }
                        float previousTime = previousKF.eventTime;
                        float eventDuration = allEvents[5][i].eventTime - previousKF.eventTime;
                        if (num == 0)
                        {
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraChromatic), intensity, intensity, 0f).SetEase(EventManager.inst.customInstantEase));
                        }
                        EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraChromatic), previousKF.eventValues[0], intensity, eventDuration).SetEase(allEvents[5][i].curveType.Animation));
                        if (num == allEvents[5].Count - 1)
                        {
                            EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraChromatic), intensity, intensity, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                        }
                    }
                    num++;
                }
                //Bloom
                num = 0;
                for (int i = 0; i < allEvents[6].Count; i++)
                {
                    if (!allEvents[6][i].active)
                    {
                        allEvents[6][i].active = true;
                        int previousIndex = num - 1;
                        if (previousIndex < 0)
                        {
                            previousIndex = 0;
                        }
                        DataManager.GameData.EventKeyframe previousKF = allEvents[6][previousIndex];
                        float bloomIntensity = allEvents[6][i].eventValues[0];
                        float diffusion = 7f;
                        float threshold = 1f;
                        float ratio = 0f;
                        float bloomColor = -1f;

                        if (allEvents[6][i].eventValues.Length > 1)
                        {
                            diffusion = allEvents[6][i].eventValues[1];
                            threshold = allEvents[6][i].eventValues[2];
                            ratio = allEvents[6][i].eventValues[3];
                            bloomColor = allEvents[6][i].eventValues[4];
                        }

                        if (float.IsNaN(bloomIntensity))
                        {
                            bloomIntensity = 0f;
                        }
                        if (float.IsNaN(diffusion))
                        {
                            diffusion = 7f;
                        }
                        if (float.IsNaN(threshold))
                        {
                            threshold = 1f;
                        }
                        if (float.IsNaN(ratio))
                        {
                            ratio = 0f;
                        }
                        if (float.IsNaN(bloomColor))
                        {
                            bloomColor = -1f;
                        }
                        float previousTime = previousKF.eventTime;
                        float eventDuration = allEvents[6][i].eventTime - previousKF.eventTime;
                        if (num == 0)
                        {
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraBloom), bloomIntensity, bloomIntensity, 0f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraBloomDiffusion), diffusion, diffusion, 0f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraBloomThreshold), threshold, threshold, 0f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraBloomAnamorphicRatio), ratio, ratio, 0f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraBloomColor), 0f, 1f, 0f).SetEase(EventManager.inst.customInstantEase));
                        }
                        EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraBloom), previousKF.eventValues[0], bloomIntensity, eventDuration).SetEase(allEvents[6][i].curveType.Animation));
                        if (previousKF.eventValues.Length > 1)
                        {
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraBloomDiffusion), previousKF.eventValues[1], diffusion, eventDuration).SetEase(allEvents[6][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraBloomThreshold), previousKF.eventValues[2], threshold, eventDuration).SetEase(allEvents[6][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraBloomAnamorphicRatio), previousKF.eventValues[3], ratio, eventDuration).SetEase(allEvents[6][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraBloomColor), 0f, 1f, eventDuration).SetEase(allEvents[6][i].curveType.Animation));
                        }
                        if (num == allEvents[6].Count - 1)
                        {
                            EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraBloom), bloomIntensity, bloomIntensity, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraBloomDiffusion), diffusion, diffusion, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraBloomThreshold), threshold, threshold, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraBloomAnamorphicRatio), ratio, ratio, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraBloomColor), 1f, 1f, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                        }
                    }
                    num++;
                }
                //Vignette
                num = 0;
                for (int i = 0; i < allEvents[7].Count; i++)
                {
                    if (!allEvents[7][i].active)
                    {
                        allEvents[7][i].active = true;
                        int previousIndex = num - 1;
                        if (previousIndex < 0)
                        {
                            previousIndex = 0;
                        }
                        DataManager.GameData.EventKeyframe previousKF = allEvents[7][previousIndex];
                        float intensity = allEvents[7][i].eventValues[0];
                        float smoothness = allEvents[7][i].eventValues[1];
                        float rounded = allEvents[7][i].eventValues[2];
                        float roundness = allEvents[7][i].eventValues[3];
                        float centerX = allEvents[7][i].eventValues[4];
                        float centerY = allEvents[7][i].eventValues[5];
                        float color = 0f;

                        if (allEvents[7][i].eventValues.Length > 6)
                        {
                            color = allEvents[7][i].eventValues[6];
                        }

                        if (float.IsNaN(intensity))
                        {
                            intensity = 0f;
                        }
                        if (float.IsNaN(smoothness))
                        {
                            smoothness = 0f;
                        }
                        if (float.IsNaN(rounded))
                        {
                            rounded = 0f;
                        }
                        if (float.IsNaN(roundness))
                        {
                            roundness = 0f;
                        }
                        if (float.IsNaN(centerX))
                        {
                            centerX = 0f;
                        }
                        if (float.IsNaN(centerY))
                        {
                            centerY = 0f;
                        }
                        if (float.IsNaN(color))
                        {
                            color = 0f;
                        }
                        float previousTime = previousKF.eventTime;
                        float eventDuration = allEvents[7][i].eventTime - previousKF.eventTime;
                        if (num == 0)
                        {
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraVignette), intensity, intensity, 0f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraVignetteSmoothness), smoothness, smoothness, 0f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraVignetteRounded), rounded, rounded, 0f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraVignetteRoundness), roundness, roundness, 0f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraVignetteCenterX), centerX, centerX, 0f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraVignetteCenterY), centerY, centerY, 0f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraVignetteColor), 0f, 1f, 0f).SetEase(EventManager.inst.customInstantEase));
                        }
                        if (num < allEvents[7].Count)
                        {
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraVignette), previousKF.eventValues[0], intensity, eventDuration).SetEase(allEvents[7][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraVignetteSmoothness), previousKF.eventValues[1], smoothness, eventDuration).SetEase(allEvents[7][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraVignetteRounded), previousKF.eventValues[2], rounded, eventDuration).SetEase(allEvents[7][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraVignetteRoundness), previousKF.eventValues[3], roundness, eventDuration).SetEase(allEvents[7][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraVignetteCenterX), previousKF.eventValues[4], centerX, eventDuration).SetEase(allEvents[7][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraVignetteCenterY), previousKF.eventValues[5], centerY, eventDuration).SetEase(allEvents[7][i].curveType.Animation));
                            if (previousKF.eventValues.Length > 6)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraVignetteColor), 0f, 1f, eventDuration).SetEase(allEvents[7][i].curveType.Animation));
                            }
                        }
                        if (num == allEvents[7].Count - 1)
                        {
                            EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraVignette), intensity, intensity, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraVignetteSmoothness), smoothness, smoothness, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraVignetteRounded), rounded, rounded, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraVignetteRoundness), roundness, roundness, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraVignetteCenterX), centerX, centerX, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraVignetteCenterY), centerY, centerY, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraVignetteColor), 1f, 1f, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                        }
                    }
                    num++;
                }
                //Lens
                num = 0;
                for (int i = 0; i < allEvents[8].Count; i++)
                {
                    if (!allEvents[8][i].active)
                    {
                        allEvents[8][i].active = true;
                        int previousIndex = num - 1;
                        if (previousIndex < 0)
                        {
                            previousIndex = 0;
                        }
                        DataManager.GameData.EventKeyframe previousKF = allEvents[8][previousIndex];
                        float intensity = allEvents[8][i].eventValues[0];

                        float centerX = 0f;
                        float centerY = 0f;
                        float intensityX = 1f;
                        float intensityY = 1f;
                        float scale = 1f;

                        if (allEvents[8][i].eventValues.Length > 1)
                        {
                            centerX = allEvents[8][i].eventValues[1];
                            centerY = allEvents[8][i].eventValues[2];
                            intensityX = allEvents[8][i].eventValues[3];
                            intensityY = allEvents[8][i].eventValues[4];
                            scale = allEvents[8][i].eventValues[5];
                        }

                        if (float.IsNaN(intensity))
                        {
                            intensity = 0f;
                        }
                        float previousTime = previousKF.eventTime;
                        float eventDuration = allEvents[8][i].eventTime - previousKF.eventTime;
                        if (num == 0)
                        {
                            EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraLens), intensity, intensity, 0.001f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraLensCenterX), centerX, centerX, 0.001f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraLensCenterY), centerY, centerY, 0.001f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraLensIntensityX), intensityX, intensityX, 0.001f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraLensIntensityY), intensityY, intensityY, 0.001f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraLensScale), scale, scale, 0.001f).SetEase(EventManager.inst.customInstantEase));
                        }
                        else
                        {
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraLens), previousKF.eventValues[0], intensity, eventDuration).SetEase(allEvents[8][i].curveType.Animation));
                            if (previousKF.eventValues.Length > 1)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraLensCenterX), previousKF.eventValues[1], centerX, eventDuration).SetEase(allEvents[8][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraLensCenterY), previousKF.eventValues[2], centerY, eventDuration).SetEase(allEvents[8][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraLensIntensityX), previousKF.eventValues[3], intensityX, eventDuration).SetEase(allEvents[8][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraLensIntensityY), previousKF.eventValues[4], intensityY, eventDuration).SetEase(allEvents[8][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraLensScale), previousKF.eventValues[5], scale, eventDuration).SetEase(allEvents[8][i].curveType.Animation));
                            }
                        }
                    }
                    num++;
                }
                //Grain
                num = 0;
                for (int i = 0; i < allEvents[9].Count; i++)
                {
                    if (!allEvents[9][i].active)
                    {
                        allEvents[9][i].active = true;
                        int previousIndex = num - 1;
                        if (previousIndex < 0)
                        {
                            previousIndex = 0;
                        }
                        var previousKF = allEvents[9][previousIndex];
                        float intensity = allEvents[9][i].eventValues[0];
                        float colored = allEvents[9][i].eventValues[1];
                        float size = allEvents[9][i].eventValues[2];
                        if (float.IsNaN(intensity))
                        {
                            intensity = 0f;
                        }
                        if (float.IsNaN(colored))
                        {
                            colored = 0f;
                        }
                        if (float.IsNaN(size))
                        {
                            size = 0f;
                        }
                        float previousTime = previousKF.eventTime;
                        float eventDuration = allEvents[9][i].eventTime - previousKF.eventTime;
                        if (num == 0)
                        {
                            EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraGrain), intensity, intensity, 0.001f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraGrainColored), colored, colored, 0.001f).SetEase(EventManager.inst.customInstantEase));
                            EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraGrainSize), size, size, 0.001f).SetEase(EventManager.inst.customInstantEase));
                        }
                        else
                        {
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraGrain), previousKF.eventValues[0], intensity, eventDuration).SetEase(allEvents[9][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraGrainColored), previousKF.eventValues[1], colored, eventDuration).SetEase(allEvents[9][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraGrainSize), previousKF.eventValues[2], size, eventDuration).SetEase(allEvents[9][i].curveType.Animation));
                        }
                    }
                    num++;
                }
                //ColorGrading
                num = 0;
                if (allEvents.Count > 10 && allEvents[10].Count > 0)
                    for (int i = 0; i < allEvents[10].Count; i++)
                    {
                        if (!allEvents[10][i].active)
                        {
                            allEvents[10][i].active = true;
                            int previousIndex = num - 1;
                            if (previousIndex < 0)
                            {
                                previousIndex = 0;
                            }
                            var previousKF = allEvents[10][previousIndex];
                            float hueShift = allEvents[10][i].eventValues[0];
                            float contrast = allEvents[10][i].eventValues[1];
                            float gammaX = allEvents[10][i].eventValues[2];
                            float gammaY = allEvents[10][i].eventValues[3];
                            float gammaZ = allEvents[10][i].eventValues[4];
                            float gammaW = allEvents[10][i].eventValues[5];
                            float saturation = allEvents[10][i].eventValues[6];
                            float temperature = allEvents[10][i].eventValues[7];
                            float tint = allEvents[10][i].eventValues[8];
                            if (float.IsNaN(hueShift))
                            {
                                hueShift = 0f;
                            }
                            if (float.IsNaN(contrast))
                            {
                                contrast = 0f;
                            }
                            if (float.IsNaN(gammaX))
                            {
                                gammaX = 0f;
                            }
                            if (float.IsNaN(gammaY))
                            {
                                gammaY = 0f;
                            }
                            if (float.IsNaN(gammaZ))
                            {
                                gammaZ = 0f;
                            }
                            if (float.IsNaN(gammaW))
                            {
                                gammaW = 0f;
                            }
                            if (float.IsNaN(saturation))
                            {
                                saturation = 0f;
                            }
                            if (float.IsNaN(temperature))
                            {
                                temperature = 0f;
                            }
                            if (float.IsNaN(tint))
                            {
                                tint = 0f;
                            }
                            float previousTime = previousKF.eventTime;
                            float eventDuration = allEvents[10][i].eventTime - previousKF.eventTime;
                            if (num == 0)
                            {
                                EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraHueShift), hueShift, hueShift, 0.001f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraContrast), contrast, contrast, 0.001f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraGammaX), gammaX, gammaX, 0.001f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraGammaY), gammaY, gammaY, 0.001f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraGammaZ), gammaZ, gammaZ, 0.001f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraGammaW), gammaW, gammaW, 0.001f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraSaturation), saturation, saturation, 0.001f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraTemperature), temperature, temperature, 0.001f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraTint), tint, tint, 0.001f).SetEase(EventManager.inst.customInstantEase));
                            }
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraHueShift), previousKF.eventValues[0], hueShift, eventDuration).SetEase(allEvents[10][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraContrast), previousKF.eventValues[1], contrast, eventDuration).SetEase(allEvents[10][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraGammaX), previousKF.eventValues[2], gammaX, eventDuration).SetEase(allEvents[10][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraGammaY), previousKF.eventValues[3], gammaY, eventDuration).SetEase(allEvents[10][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraGammaZ), previousKF.eventValues[4], gammaZ, eventDuration).SetEase(allEvents[10][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraGammaW), previousKF.eventValues[5], gammaW, eventDuration).SetEase(allEvents[10][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraSaturation), previousKF.eventValues[6], saturation, eventDuration).SetEase(allEvents[10][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraTemperature), previousKF.eventValues[7], temperature, eventDuration).SetEase(allEvents[10][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraTint), previousKF.eventValues[8], tint, eventDuration).SetEase(allEvents[10][i].curveType.Animation));

                            if (num == allEvents[10].Count - 1)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraHueShift), hueShift, hueShift, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraContrast), contrast, contrast, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraGammaX), gammaX, gammaX, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraGammaY), gammaY, gammaY, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraGammaZ), gammaZ, gammaZ, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraGammaW), gammaW, gammaW, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraSaturation), saturation, saturation, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraTemperature), temperature, temperature, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraTint), tint, tint, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            }
                        }
                        num++;
                    }
                //Ripples
                num = 0;
                if (allEvents.Count > 11 && allEvents[11].Count > 0)
                    for (int i = 0; i < allEvents[11].Count; i++)
                    {
                        if (!allEvents[11][i].active)
                        {
                            allEvents[11][i].active = true;
                            int previousIndex = num - 1;
                            if (previousIndex < 0)
                            {
                                previousIndex = 0;
                            }
                            var previousKF = allEvents[11][previousIndex];
                            float strength = allEvents[11][i].eventValues[0];
                            float speed = allEvents[11][i].eventValues[1];
                            float distance = allEvents[11][i].eventValues[2];
                            float height = allEvents[11][i].eventValues[3];
                            float width = allEvents[11][i].eventValues[4];
                            if (float.IsNaN(strength))
                            {
                                strength = 0f;
                            }
                            if (float.IsNaN(speed))
                            {
                                speed = 5f;
                            }
                            if (float.IsNaN(distance))
                            {
                                distance = 3f;
                            }
                            if (float.IsNaN(height))
                            {
                                height = 1f;
                            }
                            if (float.IsNaN(width))
                            {
                                width = 1.5f;
                            }
                            float previousTime = previousKF.eventTime;
                            float eventDuration = allEvents[11][i].eventTime - previousKF.eventTime;
                            if (num == 0)
                            {
                                EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraRippleStrength), strength, strength, 0.001f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraRippleSpeed), speed, speed, 0.001f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraRippleDistance), distance, distance, 0.001f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraRippleHeight), height, height, 0.001f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraRippleWidth), width, width, 0.001f).SetEase(EventManager.inst.customInstantEase));
                            }

                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraRippleStrength), previousKF.eventValues[0], strength, eventDuration).SetEase(allEvents[11][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraRippleSpeed), previousKF.eventValues[1], speed, eventDuration).SetEase(allEvents[11][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraRippleDistance), previousKF.eventValues[2], distance, eventDuration).SetEase(allEvents[11][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraRippleHeight), previousKF.eventValues[3], height, eventDuration).SetEase(allEvents[11][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraRippleWidth), previousKF.eventValues[4], width, eventDuration).SetEase(allEvents[11][i].curveType.Animation));

                            if (num == allEvents[11].Count - 1)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraRippleStrength), strength, strength, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraRippleSpeed), speed, speed, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraRippleDistance), distance, distance, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraRippleHeight), height, height, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraRippleWidth), width, width, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            }
                        }
                        num++;
                    }
                //RadialBlur
                num = 0;
                if (allEvents.Count > 12 && allEvents[12].Count > 0)
                    for (int i = 0; i < allEvents[12].Count; i++)
                    {
                        if (!allEvents[12][i].active)
                        {
                            allEvents[12][i].active = true;
                            int previousIndex = num - 1;
                            if (previousIndex < 0)
                            {
                                previousIndex = 0;
                            }
                            var previousKF = allEvents[12][previousIndex];
                            float intensity = allEvents[12][i].eventValues[0];
                            float iterations = allEvents[12][i].eventValues[1];
                            if (float.IsNaN(intensity))
                            {
                                intensity = 0f;
                            }
                            if (float.IsNaN(iterations))
                            {
                                intensity = 0f;
                            }
                            float previousTime = previousKF.eventTime;
                            float eventDuration = allEvents[12][i].eventTime - previousKF.eventTime;
                            if (num == 0)
                            {
                                EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraRadialBlurIntensity), intensity, intensity, 0.001f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(-0.001f, DOTween.To(new DOSetter<float>(updateCameraRadialBlurIterations), iterations, iterations, 0.001f).SetEase(EventManager.inst.customInstantEase));
                            }

                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraRadialBlurIntensity), previousKF.eventValues[0], intensity, eventDuration).SetEase(allEvents[12][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraRadialBlurIterations), previousKF.eventValues[1], iterations, eventDuration).SetEase(allEvents[12][i].curveType.Animation));

                            if (num == allEvents[12].Count - 1)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraRadialBlurIntensity), intensity, intensity, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraRadialBlurIterations), iterations, iterations, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            }
                        }
                        num++;
                    }
                //ColorSplit
                num = 0;
                if (allEvents.Count > 13 && allEvents[13].Count > 0)
                    for (int i = 0; i < allEvents[13].Count; i++)
                    {
                        if (!allEvents[13][i].active)
                        {
                            allEvents[13][i].active = true;
                            int previousIndex = num - 1;
                            if (previousIndex < 0)
                            {
                                previousIndex = 0;
                            }
                            var previousKF = allEvents[13][previousIndex];
                            float intensity = allEvents[13][i].eventValues[0];
                            if (float.IsNaN(intensity))
                            {
                                intensity = 0f;
                            }
                            float previousTime = previousKF.eventTime;
                            float eventDuration = allEvents[13][i].eventTime - previousKF.eventTime;
                            if (num == 0)
                            {
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraColorSplit), intensity, intensity, 0f).SetEase(EventManager.inst.customInstantEase));
                            }

                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraColorSplit), previousKF.eventValues[0], intensity, eventDuration).SetEase(allEvents[13][i].curveType.Animation));

                            if (num == allEvents[13].Count - 1)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraColorSplit), intensity, intensity, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            }
                        }
                        num++;
                    }
                //Camera Offset
                num = 0;
                if (allEvents.Count > 14 && allEvents[14].Count > 0)
                    for (int i = 0; i < allEvents[14].Count; i++)
                    {
                        if (!allEvents[14][i].active)
                        {
                            allEvents[14][i].active = true;
                            int previousIndex = num - 1;
                            if (previousIndex < 0)
                            {
                                previousIndex = 0;
                            }
                            var previousKF = allEvents[14][previousIndex];
                            float x = allEvents[14][i].eventValues[0];
                            float y = allEvents[14][i].eventValues[1];
                            if (float.IsNaN(x))
                            {
                                x = 0f;
                            }
                            if (float.IsNaN(y))
                            {
                                y = 0f;
                            }
                            float previousTime = previousKF.eventTime;
                            float eventDuration = allEvents[14][i].eventTime - previousKF.eventTime;
                            if (num == 0)
                            {
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraOffsetX), x, x, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraOffsetY), y, y, 0f).SetEase(EventManager.inst.customInstantEase));
                            }

                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraOffsetX), previousKF.eventValues[0], x, eventDuration).SetEase(allEvents[14][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraOffsetY), previousKF.eventValues[1], y, eventDuration).SetEase(allEvents[14][i].curveType.Animation));

                            if (num == allEvents[14].Count - 1)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraOffsetX), x, x, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraOffsetY), y, y, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            }
                        }
                        num++;
                    }
                //Gradient
                num = 0;
                if (allEvents.Count > 15 && allEvents[15].Count > 0)
                    for (int i = 0; i < allEvents[15].Count; i++)
                    {
                        if (!allEvents[15][i].active)
                        {
                            allEvents[15][i].active = true;
                            int previousIndex = num - 1;
                            if (previousIndex < 0)
                            {
                                previousIndex = 0;
                            }
                            var previousKF = allEvents[15][previousIndex];
                            float x = allEvents[15][i].eventValues[0];
                            float y = allEvents[15][i].eventValues[1];
                            float z = allEvents[15][i].eventValues[4];
                            if (float.IsNaN(x))
                            {
                                x = 0f;
                            }
                            if (float.IsNaN(y))
                            {
                                y = 0f;
                            }
                            float previousTime = previousKF.eventTime;
                            float eventDuration = allEvents[15][i].eventTime - previousKF.eventTime;
                            if (num == 0)
                            {
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraGradientIntensity), x, x, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraGradientRotation), y, y, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraGradientColor1), 0f, 1f, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraGradientColor2), 0f, 1f, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraGradientMode), z, z, 0f).SetEase(EventManager.inst.customInstantEase));
                            }

                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraGradientIntensity), previousKF.eventValues[0], x, eventDuration).SetEase(allEvents[15][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraGradientRotation), previousKF.eventValues[1], y, eventDuration).SetEase(allEvents[15][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraGradientColor1), 0f, 1f, eventDuration).SetEase(allEvents[15][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraGradientColor2), 0f, 1f, eventDuration).SetEase(allEvents[15][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraGradientMode), previousKF.eventValues[4], z, eventDuration).SetEase(allEvents[15][i].curveType.Animation));

                            if (num == allEvents[15].Count - 1)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraGradientIntensity), x, x, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraGradientRotation), y, y, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraGradientColor1), 1f, 1f, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraGradientColor2), 1f, 1f, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraGradientMode), z, z, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            }
                        }
                        num++;
                    }
                //DoubleVision
                num = 0;
                if (allEvents.Count > 16 && allEvents[16].Count > 0)
                    for (int i = 0; i < allEvents[16].Count; i++)
                    {
                        if (!allEvents[16][i].active)
                        {
                            allEvents[16][i].active = true;
                            int previousIndex = num - 1;
                            if (previousIndex < 0)
                            {
                                previousIndex = 0;
                            }
                            var previousKF = allEvents[16][previousIndex];
                            float x = allEvents[16][i].eventValues[0];
                            if (float.IsNaN(x))
                            {
                                x = 0f;
                            }
                            float previousTime = previousKF.eventTime;
                            float eventDuration = allEvents[16][i].eventTime - previousKF.eventTime;
                            if (num == 0)
                            {
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraDoubleVision), x, x, 0f).SetEase(EventManager.inst.customInstantEase));
                            }

                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraDoubleVision), previousKF.eventValues[0], x, eventDuration).SetEase(allEvents[16][i].curveType.Animation));

                            if (num == allEvents[16].Count - 1)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraDoubleVision), x, x, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            }
                        }
                        num++;
                    }
                //Scanlines
                num = 0;
                if (allEvents.Count > 17 && allEvents[17].Count > 0)
                    for (int i = 0; i < allEvents[17].Count; i++)
                    {
                        if (!allEvents[17][i].active)
                        {
                            allEvents[17][i].active = true;
                            int previousIndex = num - 1;
                            if (previousIndex < 0)
                            {
                                previousIndex = 0;
                            }
                            var previousKF = allEvents[17][previousIndex];
                            float x = allEvents[17][i].eventValues[0];
                            float y = allEvents[17][i].eventValues[1];
                            float z = allEvents[17][i].eventValues[2];
                            if (float.IsNaN(x))
                            {
                                x = 0f;
                            }
                            if (float.IsNaN(y))
                            {
                                y = 0f;
                            }
                            if (float.IsNaN(z))
                            {
                                z = 0f;
                            }
                            float previousTime = previousKF.eventTime;
                            float eventDuration = allEvents[17][i].eventTime - previousKF.eventTime;
                            if (num == 0)
                            {
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraScanLinesIntensity), x, x, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraScanLinesAmount), y, y, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraScanLinesSpeed), z, z, 0f).SetEase(EventManager.inst.customInstantEase));
                            }

                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraScanLinesIntensity), previousKF.eventValues[0], x, eventDuration).SetEase(allEvents[17][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraScanLinesAmount), previousKF.eventValues[1], y, eventDuration).SetEase(allEvents[17][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraScanLinesSpeed), previousKF.eventValues[2], z, eventDuration).SetEase(allEvents[17][i].curveType.Animation));

                            if (num == allEvents[17].Count - 1)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraScanLinesIntensity), x, x, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraScanLinesAmount), y, y, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraScanLinesSpeed), z, z, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            }
                        }
                        num++;
                    }
                //Blur
                num = 0;
                if (allEvents.Count > 18 && allEvents[18].Count > 0)
                    for (int i = 0; i < allEvents[18].Count; i++)
                    {
                        if (!allEvents[18][i].active)
                        {
                            allEvents[18][i].active = true;
                            int previousIndex = num - 1;
                            if (previousIndex < 0)
                            {
                                previousIndex = 0;
                            }
                            var previousKF = allEvents[18][previousIndex];
                            float x = allEvents[18][i].eventValues[0];
                            float y = allEvents[18][i].eventValues[1];
                            if (float.IsNaN(x))
                            {
                                x = 0f;
                            }
                            if (float.IsNaN(y))
                            {
                                y = 0f;
                            }
                            float previousTime = previousKF.eventTime;
                            float eventDuration = allEvents[18][i].eventTime - previousKF.eventTime;
                            if (num == 0)
                            {
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraBlurAmount), x, x, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraBlurIterations), y, y, 0f).SetEase(EventManager.inst.customInstantEase));
                            }

                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraBlurAmount), previousKF.eventValues[0], x, eventDuration).SetEase(allEvents[18][i].curveType.Animation));
                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraBlurIterations), previousKF.eventValues[1], y, eventDuration).SetEase(allEvents[18][i].curveType.Animation));

                            if (num == allEvents[18].Count - 1)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraBlurAmount), x, x, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraBlurIterations), y, y, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            }
                        }
                        num++;
                    }
                //Pixelize
                num = 0;
                if (allEvents.Count > 19 && allEvents[19].Count > 0)
                    for (int i = 0; i < allEvents[19].Count; i++)
                    {
                        if (!allEvents[19][i].active)
                        {
                            allEvents[19][i].active = true;
                            int previousIndex = num - 1;
                            if (previousIndex < 0)
                            {
                                previousIndex = 0;
                            }
                            var previousKF = allEvents[19][previousIndex];
                            float x = allEvents[19][i].eventValues[0];
                            if (float.IsNaN(x))
                            {
                                x = 0f;
                            }
                            float previousTime = previousKF.eventTime;
                            float eventDuration = allEvents[19][i].eventTime - previousKF.eventTime;
                            if (num == 0)
                            {
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraPixelize), x, x, 0f).SetEase(EventManager.inst.customInstantEase));
                            }

                            EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraPixelize), previousKF.eventValues[0], x, eventDuration).SetEase(allEvents[19][i].curveType.Animation));

                            if (num == allEvents[19].Count - 1)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraPixelize), x, x, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            }
                        }
                        num++;
                    }
                //BG
                num = 0;
                if (allEvents.Count > 20 && allEvents[20].Count > 0)
                    for (int i = 0; i < allEvents[20].Count; i++)
                    {
                        if (!allEvents[20][i].active)
                        {
                            allEvents[20][i].active = true;
                            int previousIndex = num - 1;
                            if (previousIndex < 0)
                            {
                                previousIndex = 0;
                            }
                            var previousKF = allEvents[20][previousIndex];
                            float previousTime = previousKF.eventTime;
                            float eventDuration = allEvents[20][i].eventTime - previousKF.eventTime;
                            if (num == 0)
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraBGColor), 0f, 1f, 0f).SetEase(EventManager.inst.customInstantEase));
                            if (num < allEvents[20].Count)
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraBGColor), 0f, 1f, eventDuration).SetEase(allEvents[20][i].curveType.Animation));
                            if (num == allEvents[20].Count - 1)
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraBGColor), 1f, 1f, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                        }
                        num++;
                    }
                //Overlay
                num = 0;
                if (allEvents.Count > 21 && allEvents[21].Count > 0)
                    for (int i = 0; i < allEvents[21].Count; i++)
                    {
                        if (!allEvents[21][i].active)
                        {
                            allEvents[21][i].active = true;
                            int previousIndex = num - 1;
                            if (previousIndex < 0)
                            {
                                previousIndex = 0;
                            }
                            var previousKF = allEvents[21][previousIndex];
                            float y = allEvents[21][i].eventValues[1];
                            if (float.IsNaN(y))
                            {
                                y = 0f;
                            }
                            float previousTime = previousKF.eventTime;
                            float eventDuration = allEvents[21][i].eventTime - previousKF.eventTime;
                            if (num == 0)
                            {
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraOverlayColor), 0f, 1f, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateCameraOverlayAlpha), y, y, 0f).SetEase(EventManager.inst.customInstantEase));
                            }
                            if (num < allEvents[21].Count)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraOverlayColor), 0f, 1f, eventDuration).SetEase(allEvents[21][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateCameraOverlayAlpha), previousKF.eventValues[1], y, eventDuration).SetEase(allEvents[21][i].curveType.Animation));
                            }
                            if (num == allEvents[21].Count - 1)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraOverlayColor), 1f, 1f, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateCameraOverlayAlpha), y, y, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            }
                        }
                        num++;
                    }
                //Timeline
                num = 0;
                if (allEvents.Count > 22 && allEvents[22].Count > 0)
                    for (int i = 0; i < allEvents[22].Count; i++)
                    {
                        if (!allEvents[22][i].active)
                        {
                            allEvents[22][i].active = true;
                            int previousIndex = num - 1;
                            if (previousIndex < 0)
                            {
                                previousIndex = 0;
                            }
                            var previousKF = allEvents[22][previousIndex];
                            float act = allEvents[22][i].eventValues[0];
                            float posX = allEvents[22][i].eventValues[1];
                            float posY = allEvents[22][i].eventValues[2];
                            float scaX = allEvents[22][i].eventValues[3];
                            float scaY = allEvents[22][i].eventValues[4];
                            float rot = allEvents[22][i].eventValues[5];
                            if (float.IsNaN(act))
                            {
                                act = 0f;
                            }
                            if (float.IsNaN(posX))
                            {
                                posX = 0f;
                            }
                            if (float.IsNaN(posY))
                            {
                                posY = 0f;
                            }
                            if (float.IsNaN(scaX))
                            {
                                scaX = 0f;
                            }
                            if (float.IsNaN(scaY))
                            {
                                scaY = 0f;
                            }
                            if (float.IsNaN(rot))
                            {
                                rot = 0f;
                            }
                            float previousTime = previousKF.eventTime;
                            float eventDuration = allEvents[22][i].eventTime - previousKF.eventTime;
                            if (num == 0)
                            {
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateTimelineActive), act, act, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateTimelinePosX), posX, posX, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateTimelinePosY), posY, posY, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateTimelineScaX), scaX, scaX, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateTimelineScaY), scaY, scaY, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateTimelineRot), rot, rot, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateTimelineColor), 0f, 1f, 0f).SetEase(EventManager.inst.customInstantEase));
                            }
                            if (num < allEvents[22].Count)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateTimelineActive), previousKF.eventValues[0], act, eventDuration).SetEase(allEvents[22][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateTimelinePosX), previousKF.eventValues[1], posX, eventDuration).SetEase(allEvents[22][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateTimelinePosY), previousKF.eventValues[2], posY, eventDuration).SetEase(allEvents[22][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateTimelineScaX), previousKF.eventValues[3], scaX, eventDuration).SetEase(allEvents[22][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateTimelineScaY), previousKF.eventValues[4], scaY, eventDuration).SetEase(allEvents[22][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateTimelineRot), previousKF.eventValues[5], rot, eventDuration).SetEase(allEvents[22][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateTimelineColor), 0f, 1f, eventDuration).SetEase(allEvents[22][i].curveType.Animation));
                            }
                            if (num == allEvents[22].Count - 1)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateTimelineActive), act, act, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateTimelinePosX), posX, posX, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateTimelinePosY), posY, posY, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateTimelineScaX), scaX, scaX, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateTimelineScaY), scaY, scaY, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateTimelineRot), rot, rot, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateTimelineColor), 1f, 1f, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            }
                        }
                        num++;
                    }
                //Player
                num = 0;
                if (allEvents.Count > 23 && allEvents[23].Count > 0)
                    for (int i = 0; i < allEvents[23].Count; i++)
                    {
                        if (!allEvents[23][i].active)
                        {
                            allEvents[23][i].active = true;
                            int previousIndex = num - 1;
                            if (previousIndex < 0)
                            {
                                previousIndex = 0;
                            }
                            var previousKF = allEvents[23][previousIndex];
                            float act = allEvents[23][i].eventValues[0];
                            float move = allEvents[23][i].eventValues[1];
                            float velocity = allEvents[23][i].eventValues[2];
                            float rotation = allEvents[23][i].eventValues[3];
                            if (float.IsNaN(act))
                            {
                                act = 0f;
                            }
                            if (float.IsNaN(move))
                            {
                                move = 0f;
                            }
                            if (float.IsNaN(velocity))
                            {
                                velocity = 0f;
                            }
                            if (float.IsNaN(rotation))
                            {
                                rotation = 0f;
                            }
                            float previousTime = previousKF.eventTime;
                            float eventDuration = allEvents[23][i].eventTime - previousKF.eventTime;
                            if (num == 0)
                            {
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updatePlayerActive), act, act, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updatePlayerMoveable), move, move, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updatePlayerVelocity), velocity, velocity, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updatePlayerRotation), rotation, rotation, 0f).SetEase(EventManager.inst.customInstantEase));
                            }
                            if (num < allEvents[23].Count)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updatePlayerActive), previousKF.eventValues[0], act, eventDuration).SetEase(allEvents[23][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updatePlayerMoveable), previousKF.eventValues[1], move, eventDuration).SetEase(allEvents[23][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updatePlayerVelocity), previousKF.eventValues[2], velocity, eventDuration).SetEase(allEvents[23][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updatePlayerRotation), previousKF.eventValues[3], rotation, eventDuration).SetEase(allEvents[23][i].curveType.Animation));
                            }
                            if (num == allEvents[23].Count - 1)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updatePlayerActive), act, act, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updatePlayerMoveable), move, move, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updatePlayerVelocity), velocity, velocity, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updatePlayerRotation), rotation, rotation, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            }
                        }
                        num++;
                    }
                //Follow Player
                num = 0;
                if (allEvents.Count > 24 && allEvents[24].Count > 0)
                    for (int i = 0; i < allEvents[24].Count; i++)
                    {
                        if (!allEvents[24][i].active)
                        {
                            allEvents[24][i].active = true;
                            int previousIndex = num - 1;
                            if (previousIndex < 0)
                            {
                                previousIndex = 0;
                            }
                            var previousKF = allEvents[24][previousIndex];
                            float active = allEvents[24][i].eventValues[0];
                            float move = allEvents[24][i].eventValues[1];
                            float rotate = allEvents[24][i].eventValues[2];
                            float sharpness = allEvents[24][i].eventValues[3];
                            float offset = allEvents[24][i].eventValues[4];

                            float limitLeft = 9999f;
                            float limitRight = -9999f;
                            float limitUp = 9999f;
                            float limitDown = -9999f;
                            float anchor = 1f;

                            if (float.IsNaN(active))
                                active = 0f;
                            if (float.IsNaN(move))
                                move = 0f;
                            if (float.IsNaN(rotate))
                                rotate = 0f;
                            if (float.IsNaN(sharpness))
                                sharpness = 1f;
                            if (float.IsNaN(offset))
                                offset = 0f;

                            if (allEvents[24][i].eventValues.Length > 5)
                                limitLeft = allEvents[24][i].eventValues[5];

                            if (allEvents[24][i].eventValues.Length > 6)
                                limitRight = allEvents[24][i].eventValues[6];

                            if (allEvents[24][i].eventValues.Length > 7)
                                limitUp = allEvents[24][i].eventValues[7];

                            if (allEvents[24][i].eventValues.Length > 8)
                                limitDown = allEvents[24][i].eventValues[8];

                            if (allEvents[24][i].eventValues.Length > 9)
                                anchor = allEvents[24][i].eventValues[9];
                            float previousTime = previousKF.eventTime;
                            float eventDuration = allEvents[24][i].eventTime - previousKF.eventTime;
                            if (num == 0)
                            {
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateDelayTrackerActive), active, active, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateDelayTrackerMove), move, move, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateDelayTrackerRotate), rotate, rotate, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateDelayTrackerSharpness), sharpness, sharpness, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateDelayTrackerOffset), offset, offset, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateDelayTrackerLimitLeft), limitLeft, limitLeft, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateDelayTrackerLimitRight), limitRight, limitRight, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateDelayTrackerLimitUp), limitUp, limitUp, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateDelayTrackerLimitDown), limitDown, limitDown, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateDelayTrackerAnchor), anchor, anchor, 0f).SetEase(EventManager.inst.customInstantEase));
                            }
                            if (num < allEvents[24].Count)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateDelayTrackerActive), previousKF.eventValues[0], active, eventDuration).SetEase(allEvents[24][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateDelayTrackerMove), previousKF.eventValues[1], move, eventDuration).SetEase(allEvents[24][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateDelayTrackerRotate), previousKF.eventValues[2], rotate, eventDuration).SetEase(allEvents[24][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateDelayTrackerSharpness), previousKF.eventValues[3], sharpness, eventDuration).SetEase(allEvents[24][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateDelayTrackerOffset), previousKF.eventValues[4], offset, eventDuration).SetEase(allEvents[24][i].curveType.Animation));

                                if (previousKF.eventValues.Length > 5)
                                    EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateDelayTrackerLimitLeft), previousKF.eventValues[5], limitLeft, 0f).SetEase(allEvents[24][i].curveType.Animation));
                                if (previousKF.eventValues.Length > 6)
                                    EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateDelayTrackerLimitRight), previousKF.eventValues[6], limitRight, 0f).SetEase(allEvents[24][i].curveType.Animation));
                                if (previousKF.eventValues.Length > 7)
                                    EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateDelayTrackerLimitUp), previousKF.eventValues[7], limitUp, 0f).SetEase(allEvents[24][i].curveType.Animation));
                                if (previousKF.eventValues.Length > 8)
                                    EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateDelayTrackerLimitDown), previousKF.eventValues[8], limitDown, 0f).SetEase(allEvents[24][i].curveType.Animation));
                                if (previousKF.eventValues.Length > 9)
                                    EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateDelayTrackerAnchor), previousKF.eventValues[9], anchor, 0f).SetEase(allEvents[24][i].curveType.Animation));
                            }
                            if (num == allEvents[24].Count - 1)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateDelayTrackerActive), active, active, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateDelayTrackerMove), move, move, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateDelayTrackerRotate), rotate, rotate, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateDelayTrackerSharpness), sharpness, sharpness, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateDelayTrackerOffset), offset, offset, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateDelayTrackerLimitLeft), limitLeft, limitLeft, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateDelayTrackerLimitRight), limitRight, limitRight, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateDelayTrackerLimitUp), limitUp, limitUp, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateDelayTrackerLimitDown), limitDown, limitDown, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateDelayTrackerAnchor), anchor, anchor, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            }
                        }
                        num++;
                    }
                //Music
                num = 0;
                if (allEvents.Count > 25 && allEvents[25].Count > 0)
                    for (int i = 0; i < allEvents[25].Count; i++)
                    {
                        if (!allEvents[25][i].active)
                        {
                            allEvents[25][i].active = true;
                            int previousIndex = num - 1;
                            if (previousIndex < 0)
                            {
                                previousIndex = 0;
                            }
                            var previousKF = allEvents[25][previousIndex];
                            float pitch = allEvents[25][i].eventValues[0];
                            float volume = allEvents[25][i].eventValues[1];
                            if (float.IsNaN(pitch))
                            {
                                pitch = 0f;
                            }
                            if (float.IsNaN(volume))
                            {
                                volume = 0f;
                            }
                            float previousTime = previousKF.eventTime;
                            float eventDuration = allEvents[25][i].eventTime - previousKF.eventTime;
                            if (num == 0)
                            {
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateAudioPitch), pitch, pitch, 0f).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(0f, DOTween.To(new DOSetter<float>(updateAudioVolume), volume, volume, 0f).SetEase(EventManager.inst.customInstantEase));
                            }
                            if (num < allEvents[25].Count)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateAudioPitch), previousKF.eventValues[0], pitch, eventDuration).SetEase(allEvents[25][i].curveType.Animation));
                                EventManager.inst.eventSequence.Insert(previousTime, DOTween.To(new DOSetter<float>(updateAudioVolume), previousKF.eventValues[1], volume, eventDuration).SetEase(allEvents[25][i].curveType.Animation));
                            }
                            if (num == allEvents[25].Count - 1)
                            {
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateAudioPitch), pitch, pitch, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                                EventManager.inst.eventSequence.Insert(previousTime + eventDuration, DOTween.To(new DOSetter<float>(updateAudioVolume), volume, volume, AudioManager.inst.CurrentAudioSource.clip.length).SetEase(EventManager.inst.customInstantEase));
                            }
                        }
                        num++;
                    }

                #endregion

                #region Find Colors

                if (allEvents[4].Count > 0)
                    if (allEvents[4].Find((DataManager.GameData.EventKeyframe x) => x.eventTime > AudioManager.inst.CurrentAudioSource.time) != null)
                    {
                        var nextKF = allEvents[4].Find((DataManager.GameData.EventKeyframe x) => x.eventTime > AudioManager.inst.CurrentAudioSource.time);
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

                if (allEvents[6].Count > 0)
                    if (allEvents[6].Find((DataManager.GameData.EventKeyframe x) => x.eventTime > AudioManager.inst.CurrentAudioSource.time) != null)
                    {
                        var nextKF = allEvents[6].Find((DataManager.GameData.EventKeyframe x) => x.eventTime > AudioManager.inst.CurrentAudioSource.time);
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

                if (allEvents[7].Count > 0)
                    if (allEvents[7].Find((DataManager.GameData.EventKeyframe x) => x.eventTime > AudioManager.inst.CurrentAudioSource.time) != null)
                    {
                        var nextKF = allEvents[7].Find((DataManager.GameData.EventKeyframe x) => x.eventTime > AudioManager.inst.CurrentAudioSource.time);
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

                if (allEvents.Count > 15 && allEvents[15].Count > 0)
                    if (allEvents[15].Find((DataManager.GameData.EventKeyframe x) => x.eventTime > AudioManager.inst.CurrentAudioSource.time) != null)
                    {
                        var nextKF = allEvents[15].Find((DataManager.GameData.EventKeyframe x) => x.eventTime > AudioManager.inst.CurrentAudioSource.time);
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

                if (allEvents.Count > 20 && allEvents[20].Count > 0)
                    if (allEvents[20].Find((DataManager.GameData.EventKeyframe x) => x.eventTime > AudioManager.inst.CurrentAudioSource.time) != null)
                    {
                        var nextKF = allEvents[20].Find((DataManager.GameData.EventKeyframe x) => x.eventTime > AudioManager.inst.CurrentAudioSource.time);
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

                if (allEvents.Count > 21 && allEvents[21].Count > 0)
                    if (allEvents[21].Find((DataManager.GameData.EventKeyframe x) => x.eventTime > AudioManager.inst.CurrentAudioSource.time) != null)
                    {
                        var nextKF = allEvents[21].Find((DataManager.GameData.EventKeyframe x) => x.eventTime > AudioManager.inst.CurrentAudioSource.time);
                        if (allEvents[21].IndexOf(nextKF) - 1 > -1)
                        {
                            prevOverlayColor = (int)allEvents[21][allEvents[21].IndexOf(nextKF) - 1].eventValues[0];
                        }
                        else
                        {
                            prevOverlayColor = (int)allEvents[21][0].eventValues[0];
                        }
                        nextOverlayColor = (int)nextKF.eventValues[0];
                    }
                    else
                    {
                        var finalKF = allEvents[21][allEvents[21].Count - 1];

                        int a = allEvents[21].Count - 2;
                        if (a < 0)
                        {
                            a = 0;
                        }
                        prevOverlayColor = (int)allEvents[21][a].eventValues[0];
                        nextOverlayColor = (int)finalKF.eventValues[0];
                    }

                if (allEvents.Count > 22 && allEvents[22].Count > 0)
                    if (allEvents[22].Find((DataManager.GameData.EventKeyframe x) => x.eventTime > AudioManager.inst.CurrentAudioSource.time) != null)
                    {
                        var nextKF = allEvents[22].Find((DataManager.GameData.EventKeyframe x) => x.eventTime > AudioManager.inst.CurrentAudioSource.time);
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

        #region Update Methods

        public float themeLerp;

        public void updateTheme(float _theme)
        {
            themeLerp = _theme;
            DataManager.BeatmapTheme beatmapTheme = DataManager.BeatmapTheme.DeepCopy(GameManager.inst.LiveTheme, false);
            GameManager.inst.LiveTheme.Lerp(DataManager.inst.GetTheme(EventManager.inst.LastTheme), DataManager.inst.GetTheme(EventManager.inst.NewTheme), _theme);
            if (beatmapTheme != GameManager.inst.LiveTheme)
            {
                GameManager.inst.UpdateTheme();
            }
        }

        public void updateShake()
        {
            Vector3 vector = EventManager.inst.shakeVector * EventManager.inst.shakeMultiplier;
            vector.x *= shakeX;
            vector.y *= shakeY;
            vector.z = 0f;
            if (float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z))
            {
                vector = Vector3.zero;
            }
            if (!float.IsNaN(camPosX) && !float.IsNaN(camPosY))
            {
                EventManager.inst.camParent.transform.localPosition = vector + new Vector3(camPosX, camPosY, 0f);
            }
        }
        public void updateEvents(int currentEvent)
        {
            Debug.LogFormat("{0}UPDATING EVENT [{1}]", new object[]
            {
                "EVENT MANAGER\n",
                currentEvent
            });
            EventManager.inst.eventSequence.Kill();
            EventManager.inst.shakeSequence.Kill();
            EventManager.inst.themeSequence.Kill();
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
            Debug.LogFormat("{0}Updating events", EventsCorePlugin.className);
            EventManager.inst.shakeSequence.Kill();
            EventManager.inst.eventSequence.Kill();
            EventManager.inst.themeSequence.Kill();
            EventManager.inst.shakeSequence = null;
            EventManager.inst.eventSequence = null;
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

        public void updateDelayTrackerActive(float _act)
        {
            if ((int)_act == 0)
            {
                delayTracker.active = false;
            }
            if ((int)_act == 1)
            {
                delayTracker.active = true;
            }
        }

        public void updateDelayTrackerMove(float _move)
        {
            if ((int)_move == 0)
            {
                delayTracker.move = false;
            }
            if ((int)_move == 1)
            {
                delayTracker.move = true;
            }
        }

        public void updateDelayTrackerRotate(float _move)
        {
            if ((int)_move == 0)
            {
                delayTracker.rotate = false;
            }
            if ((int)_move == 1)
            {
                delayTracker.rotate = true;
            }
        }

        public void updateDelayTrackerSharpness(float _sharp)
        {
            delayTracker.followSharpness = Mathf.Clamp(_sharp, 0.001f, 1f);
        }

        public void updateDelayTrackerOffset(float _offset)
        {
            delayTracker.offset = _offset;
        }

        public void updateDelayTrackerLimitLeft(float _x)
        {
            delayTracker.limitLeft = _x;
        }

        public void updateDelayTrackerLimitRight(float _x)
        {
            delayTracker.limitRight = _x;
        }

        public void updateDelayTrackerLimitUp(float _x)
        {
            delayTracker.limitUp = _x;
        }

        public void updateDelayTrackerLimitDown(float _x)
        {
            delayTracker.limitDown = _x;
        }

        public void updateDelayTrackerAnchor(float _x)
        {
            delayTracker.anchor = _x;
        }

        public void updateAudioPitch(float _pitch)
        {
            float x = Mathf.Clamp(_pitch, 0.001f, 10f);
            AudioManager.inst.pitch = x * GameManager.inst.getPitch() * pitchOffset;
        }

        public float pitchOffset = 1f;

        public void updateAudioVolume(float _vol)
        {
            float v = Mathf.Clamp(_vol, 0f, 1f);

            audioVolume = v;
        }

        public void updatePlayerActive(float _active)
        {
            var active = false;

            if ((int)_active == 0)
            {
                active = true;
            }
            else if ((int)_active == 1)
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

        public void updatePlayerVelocity(float _velocity)
        {
            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
            {
                if (GameObject.Find(string.Format("Player {0}/Player", i + 1)))
                {
                    var rt = GameObject.Find(string.Format("Player {0}", i + 1)).GetComponentByName("RTPlayer");
                    if (rt != null)
                    {
                        playersCanMove = (bool)rt.GetType().GetProperty("CanMove").GetValue(rt);
                    }
                    else
                    {
                        playersCanMove = GameObject.Find(string.Format("Player {0}", i + 1)).GetComponent<Player>().CanMove;
                    }

                    var player = GameObject.Find(string.Format("Player {0}/Player", i + 1)).transform;
                    if (!playersCanMove)
                        player.GetComponent<Rigidbody2D>().velocity = new Vector2(player.transform.right.x, player.transform.right.y) * _velocity;
                }
            }
        }

        public void updatePlayerRotation(float _rotation)
        {
            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
            {
                if (GameObject.Find(string.Format("Player {0}/Player", i + 1)))
                {
                    var player = GameObject.Find(string.Format("Player {0}/Player", i + 1)).transform;
                    if (!playersCanMove)
                        player.localEulerAngles = new Vector3(0f, 0f, _rotation);
                }
            }
        }

        public void updatePlayerMoveable(float _move)
        {
            if ((int)_move == 0)
            {
                for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                {
                    if (GameObject.Find(string.Format("Player {0}/Player", i + 1)))
                    {
                        playersCanMove = true;
                        var rt = GameObject.Find(string.Format("Player {0}", i + 1)).GetComponentByName("RTPlayer");
                        if (rt != null)
                        {
                            rt.GetType().GetProperty("CanMove").SetValue(rt, true);
                        }
                        else
                        {
                            GameObject.Find(string.Format("Player {0}", i + 1)).GetComponent<Player>().CanMove = true;
                        }
                    }
                }
            }
            else if ((int)_move == 1)
            {
                for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                {
                    if (GameObject.Find(string.Format("Player {0}/Player", i + 1)))
                    {
                        playersCanMove = false;
                        var rt = GameObject.Find(string.Format("Player {0}", i + 1)).GetComponentByName("RTPlayer");
                        if (rt != null)
                        {
                            rt.GetType().GetProperty("CanMove").SetValue(rt, false);
                        }
                        else
                        {
                            GameObject.Find(string.Format("Player {0}", i + 1)).GetComponent<Player>().CanMove = false;
                        }
                    }
                }
            }
        }

        public void updateTimelinePosX(float _x)
        {
            timelinePos.x = _x;
        }

        public void updateTimelinePosY(float _y)
        {
            timelinePos.y = _y;
        }

        public void updateTimelineScaX(float _x)
        {
            timelineSca.x = _x;
        }

        public void updateTimelineScaY(float _y)
        {
            timelineSca.y = _y;
        }

        public void updateTimelineRot(float _x)
        {
            timelineRot = _x;
        }

        public void updateTimelineColor(float _col)
        {
            timelineColor = _col;
        }

        public void LerpTimelineColor()
        {
            Color previous;
            Color next;

            DataManager.BeatmapTheme beatmapTheme = GameManager.inst.LiveTheme;
            if (EditorManager.inst != null && EventEditor.inst.showTheme)
            {
                beatmapTheme = EventEditor.inst.previewTheme;
            }

            if (beatmapTheme.objectColors.Count > prevTimelineColor && prevTimelineColor > -1)
            {
                previous = beatmapTheme.objectColors[prevTimelineColor];
            }
            else
            {
                previous = beatmapTheme.guiColor;
            }
            if (beatmapTheme.objectColors.Count > nextTimelineColor && nextTimelineColor > -1)
            {
                next = beatmapTheme.objectColors[nextTimelineColor];
            }
            else
            {
                next = beatmapTheme.guiColor;
            }

            float num = timelineColor;
            if (float.IsNaN(num) || num < 0f)
            {
                num = 0f;
            }

            EventsCorePlugin.timelineColorToLerp = Color.Lerp(previous, next, num);
        }

        public void updateTimelineActive(float _e)
        {
            var active = false;

            if ((int)_e == 0)
            {
                active = true;
            }
            if ((int)_e == 1)
            {
                active = false;
            }

            var zen = false;
            if (DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) == 0 || EditorManager.inst != null)
            {
                zen = true;
            }

            timelineActive = active && !zen || active && EventsCorePlugin.ShowGUI.Value;
        }

        public void updateCameraOverlayColor(float _ov)
        {
            overlayColor = _ov;
        }

        public void updateCameraOverlayAlpha(float _alpha)
        {
            overlayAlpha = _alpha;
        }

        public void LerpOverlayColor()
        {
            Color previous;
            Color next;

            DataManager.BeatmapTheme beatmapTheme = GameManager.inst.LiveTheme;
            if (EditorManager.inst != null && EventEditor.inst.showTheme)
            {
                beatmapTheme = EventEditor.inst.previewTheme;
            }

            if (beatmapTheme.objectColors.Count > prevOverlayColor && prevOverlayColor > -1)
            {
                previous = beatmapTheme.objectColors[prevOverlayColor];
            }
            else
            {
                previous = Color.black;
            }
            if (beatmapTheme.objectColors.Count > nextOverlayColor && nextOverlayColor > -1)
            {
                next = beatmapTheme.objectColors[nextOverlayColor];
            }
            else
            {
                next = Color.black;
            }

            float num = overlayColor;
            if (float.IsNaN(num) || num < 0f)
            {
                num = 0f;
            }

            EventsCorePlugin.overlayColorToLerp = Color.Lerp(previous, next, num);
        }

        public void updateCameraBGColor(float _bg)
        {
            bgColor = _bg;
        }

        public void LerpBGColor()
        {
            Color previous;
            Color next;

            DataManager.BeatmapTheme beatmapTheme = GameManager.inst.LiveTheme;
            if (EditorManager.inst != null && EventEditor.inst.showTheme)
            {
                beatmapTheme = EventEditor.inst.previewTheme;
            }

            if (beatmapTheme.objectColors.Count > prevBGColor && prevBGColor > -1)
            {
                previous = beatmapTheme.objectColors[prevBGColor];
            }
            else
            {
                previous = beatmapTheme.backgroundColor;
            }
            if (beatmapTheme.objectColors.Count > nextBGColor && nextBGColor > -1)
            {
                next = beatmapTheme.objectColors[nextBGColor];
            }
            else
            {
                next = beatmapTheme.backgroundColor;
            }

            float num = bgColor;
            if (float.IsNaN(num) || num < 0f)
            {
                num = 0f;
            }

            EventsCorePlugin.bgColorToLerp = Color.Lerp(previous, next, num);
        }

        public float camPosX;
        public float camPosY;
        public void updateCameraOffsetX(float _pos)
        {
            camPosX = _pos;
        }
        public void updateCameraOffsetY(float _pos)
        {
            camPosY = _pos;
        }

        public void updateCameraPositionX(float _pos)
        {
            EventManager.inst.camPos.x = _pos;
        }

        public void updateCameraPositionY(float _pos)
        {
            EventManager.inst.camPos.y = _pos;
        }

        public void updateCameraZoom(float _zoom)
        {
            EventManager.inst.camZoom = _zoom;
        }

        public void updateCameraRotation(float _rot)
        {
            EventManager.inst.camRot = _rot;
        }

        public void updateCameraChromatic(float _chroma)
        {
            EventManager.inst.camChroma = _chroma;
        }

        public void updateCameraBloom(float _bloom)
        {
            EventManager.inst.camBloom = _bloom;
        }

        public void updateCameraBloomDiffusion(float _diffusion)
        {
            LSEffectsManager.inst.bloom.diffusion.Override(_diffusion);
        }

        public void updateCameraBloomThreshold(float _threshold)
        {
            LSEffectsManager.inst.bloom.threshold.Override(_threshold);
        }

        public void updateCameraBloomAnamorphicRatio(float _ratio)
        {
            LSEffectsManager.inst.bloom.anamorphicRatio.Override(_ratio);
        }

        public void updateCameraBloomColor(float _x)
        {
            bloomColor = _x;
        }

        public void LerpBloomColor()
        {
            Color previous;
            Color next;
            if (GameManager.inst.LiveTheme.objectColors.Count > prevBloomColor && prevBloomColor > -1)
            {
                previous = GameManager.inst.LiveTheme.objectColors[prevBloomColor];
            }
            else
            {
                previous = Color.white;
            }
            if (GameManager.inst.LiveTheme.objectColors.Count > nextBloomColor && nextBloomColor > -1)
            {
                next = GameManager.inst.LiveTheme.objectColors[nextBloomColor];
            }
            else
            {
                next = Color.white;
            }

            LSEffectsManager.inst.bloom.color.Override(Color.Lerp(previous, next, bloomColor));
        }

        public void updateCameraVignette(float _vignette)
        {
            EventManager.inst.vignetteIntensity = _vignette;
        }

        public void updateCameraVignetteSmoothness(float _vignette)
        {
            EventManager.inst.vignetteSmoothness = _vignette;
        }

        public void updateCameraVignetteRounded(float _vignette)
        {
            EventManager.inst.vignetteRounded = _vignette;
        }

        public void updateCameraVignetteRoundness(float _vignette)
        {
            EventManager.inst.vignetteRoundness = _vignette;
        }

        public void updateCameraVignetteCenterX(float _vignette)
        {
            EventManager.inst.vignetteCenter.x = _vignette;
        }

        public void updateCameraVignetteCenterY(float _vignette)
        {
            EventManager.inst.vignetteCenter.y = _vignette;
        }

        public void updateCameraVignetteColor(float _x)
        {
            vignetteColor = _x;
        }

        public void LerpVignetteColor()
        {
            Color previous;
            Color next;
            if (GameManager.inst.LiveTheme.objectColors.Count > prevVignetteColor && prevVignetteColor >= 0)
            {
                previous = GameManager.inst.LiveTheme.objectColors[prevVignetteColor];
            }
            else
            {
                previous = Color.black;
            }
            if (GameManager.inst.LiveTheme.objectColors.Count > nextVignetteColor && nextVignetteColor >= 0)
            {
                next = GameManager.inst.LiveTheme.objectColors[nextVignetteColor];
            }
            else
            {
                next = Color.black;
            }

            LSEffectsManager.inst.vignette.color.Override(Color.Lerp(previous, next, vignetteColor));
        }

        public void updateCameraLens(float _intensity)
        {
            EventManager.inst.lensDistortIntensity = _intensity;
        }

        public void updateCameraLensCenterX(float _x)
        {
            LSEffectsManager.inst.lensDistort.centerX.Override(_x);
        }

        public void updateCameraLensCenterY(float _y)
        {
            LSEffectsManager.inst.lensDistort.centerY.Override(_y);
        }

        public void updateCameraLensIntensityX(float _x)
        {
            LSEffectsManager.inst.lensDistort.intensityX.Override(_x);
        }

        public void updateCameraLensIntensityY(float _y)
        {
            LSEffectsManager.inst.lensDistort.intensityY.Override(_y);
        }

        public void updateCameraLensScale(float _y)
        {
            LSEffectsManager.inst.lensDistort.scale.Override(_y);
        }

        public void updateCameraGrain(float _intensity)
        {
            EventManager.inst.grainIntensity = _intensity;
        }

        public void updateCameraGrainColored(float _colored)
        {
            EventManager.inst.grainColored = _colored;
        }

        public void updateCameraGrainSize(float _size)
        {
            EventManager.inst.grainSize = _size;
        }

        public void updateCameraShakeX(float _x)
        {
            shakeX = _x;
        }

        public void updateCameraShakeY(float _y)
        {
            shakeY = _y;
        }

        public void updateCameraHueShift(float _hueshift)
        {
            colorGradingHueShift = _hueshift;
        }

        public void updateCameraContrast(float _contrast)
        {
            colorGradingContrast = _contrast;
        }

        public void updateCameraGammaX(float _gamma)
        {
            colorGradingGamma.x = _gamma;
        }

        public void updateCameraGammaY(float _gamma)
        {
            colorGradingGamma.y = _gamma;
        }

        public void updateCameraGammaZ(float _gamma)
        {
            colorGradingGamma.z = _gamma;
        }

        public void updateCameraGammaW(float _gamma)
        {
            colorGradingGamma.w = _gamma;
        }

        public void updateCameraSaturation(float _saturation)
        {
            colorGradingSaturation = _saturation;
        }

        public void updateCameraTemperature(float _temperature)
        {
            colorGradingTemperature = _temperature;
        }

        public void updateCameraTint(float _tint)
        {
            colorGradingTint = _tint;
        }

        public void updateCameraGradientIntensity(float _intensity)
        {
            gradientIntensity = _intensity;
        }

        public void updateCameraGradientColor1(float _x)
        {
            gradientColor1 = _x;
        }

        public void LerpGradientColor1()
        {
            Color previous;
            Color next;
            if (GameManager.inst.LiveTheme.objectColors.Count > prevGradientColor1 && prevGradientColor1 >= 0)
            {
                previous = GameManager.inst.LiveTheme.objectColors[prevGradientColor1];
            }
            else
            {
                previous = new Color(0f, 0.8f, 0.56f, 0.5f);
            }
            if (GameManager.inst.LiveTheme.objectColors.Count > nextGradientColor1 && nextGradientColor1 >= 0)
            {
                next = GameManager.inst.LiveTheme.objectColors[nextGradientColor1];
            }
            else
            {
                next = new Color(0f, 0.8f, 0.56f, 0.5f);
            }

            RTEffectsManager.inst.gradient.color1.Override(Color.Lerp(previous, next, gradientColor1));
        }

        public void updateCameraGradientColor2(float _x)
        {
            gradientColor2 = _x;
        }

        public void LerpGradientColor2()
        {
            Color previous;
            Color next;
            if (GameManager.inst.LiveTheme.objectColors.Count > prevGradientColor2 && prevGradientColor2 >= 0)
            {
                previous = GameManager.inst.LiveTheme.objectColors[prevGradientColor2];
            }
            else
            {
                previous = new Color(0.81f, 0.37f, 1f, 0.5f);
            }
            if (GameManager.inst.LiveTheme.objectColors.Count > nextGradientColor2 && nextGradientColor2 >= 0)
            {
                next = GameManager.inst.LiveTheme.objectColors[nextGradientColor2];
            }
            else
            {
                next = new Color(0.81f, 0.37f, 1f, 0.5f);
            }

            RTEffectsManager.inst.gradient.color2.Override(Color.Lerp(previous, next, gradientColor2));
        }

        public void updateCameraGradientRotation(float _x)
        {
            gradientRotation = _x;
        }

        public void updateCameraGradientMode(float _x)
        {
            RTEffectsManager.inst.gradient.blendMode.Override((SCPE.Gradient.BlendMode)(int)_x);
        }

        public void updateCameraDoubleVision(float _doubleVision)
        {
            doubleVision = _doubleVision;
        }

        public void updateCameraRadialBlurIntensity(float _intensity)
        {
            radialBlurIntensity = _intensity;
        }

        public void updateCameraRadialBlurIterations(float _iterations)
        {
            int x = (int)_iterations;

            if (x <= 1)
            {
                x = 1;
            }

            radialBlurIterations = x;
        }

        public void updateCameraScanLinesIntensity(float _intensity)
        {
            scanLinesIntensity = _intensity;
        }

        public void updateCameraScanLinesAmount(float _amount)
        {
            scanLinesAmount = _amount;
        }

        public void updateCameraScanLinesSpeed(float _speed)
        {
            scanLinesSpeed = _speed;
        }

        public void updateCameraSharpen(float _sharpen)
        {
            sharpen = _sharpen;
        }

        public void updateCameraColorSplit(float _offset)
        {
            colorSplitOffset = _offset;
        }

        public void updateCameraDangerIntensity(float _intensity)
        {
            dangerIntensity = _intensity;
        }

        public void updateCameraDangerColor(Color _color)
        {
            dangerColor = _color;
        }

        public void updateCameraDangerSize(float _size)
        {
            dangerSize = _size;
        }

        public void updateCameraRippleStrength(float _strength)
        {
            ripplesStrength = _strength;
        }
        public void updateCameraRippleSpeed(float _speed)
        {
            ripplesSpeed = _speed;
        }
        public void updateCameraRippleDistance(float _distance)
        {
            float x = _distance;
            if (x <= 0f)
            {
                x = 0.001f;
            }

            ripplesDistance = _distance;
        }
        public void updateCameraRippleHeight(float _height)
        {
            ripplesHeight = _height;
        }
        public void updateCameraRippleWidth(float _width)
        {
            ripplesWidth = _width;
        }

        public void updateCameraBlurAmount(float _blur)
        {
            LSEffectsManager.inst.blur.amount.Override(_blur);
        }

        public void updateCameraBlurIterations(float _iterations)
        {
            LSEffectsManager.inst.blur.iterations.Override((int)_iterations);
        }

        public void updateCameraPixelize(float _pixelize)
        {
            float x = _pixelize;
            if (x >= 1f)
            {
                x = 0.99999f;
            }

            pixel = x;
        }

        #endregion

        #region Variables

        public float audioVolume = 1f;

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

        public float overlayColor;
        public int prevOverlayColor = 18;
        public int nextOverlayColor = 18;
        public float overlayAlpha;

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

        #region Offsets

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
            },
            new List<float>
            {
                0f, // Vignette Intensity
                0f, // Vignette Smoothness
                0f, // Vignette Rounded
                0f, // Vignette Roundness
                0f, // Vignette Center X
                0f, // Vignette Center Y
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
        };

        #endregion

        #region EventSequence

        //public void GotoEventSequence(float _time)
        //{
        //    foreach (var sequence in eventSequences)
        //    {
        //        sequence.sequence.Goto(_time);
        //    }
        //}

        //public void updateEventSequence(int type)
        //{
        //    eventSequences[type].sequence.Kill();
        //    foreach (var keyframe in DataManager.inst.gameData.eventObjects.allEvents[type])
        //    {
        //        keyframe.active = false;
        //    }
        //}

        //public void updateSequencer(int type, int value, float _val)
        //{
        //    switch (type)
        //    {
        //        case 0:
        //            {
        //                if (value == 0)
        //                {
        //                    updateCameraPositionX(_val);
        //                }
        //                if (value == 1)
        //                {
        //                    updateCameraPositionY(_val);
        //                }
        //                break;
        //            }
        //        case 1:
        //            {
        //                updateCameraZoom(_val);
        //                break;
        //            }
        //        case 2:
        //            {
        //                updateCameraRotation(_val);
        //                break;
        //            }
        //        case 3:
        //            {
        //                if (value == 0)
        //                    EventManager.inst.shakeMultiplier = _val;
        //                if (value == 1)
        //                    updateCameraShakeX(_val);
        //                if (value == 2)
        //                    updateCameraShakeY(_val);
        //                break;
        //            }
        //        case 4:
        //            {
        //                updateTheme(_val);
        //                break;
        //            }
        //        case 5:
        //            {
        //                updateCameraChromatic(_val);
        //                break;
        //            }
        //        case 6:
        //            {
        //                if (value == 0)
        //                    updateCameraBloom(_val);
        //                if (value == 1)
        //                    updateCameraBloomDiffusion(_val);
        //                if (value == 2)
        //                    updateCameraBloomThreshold(_val);
        //                if (value == 3)
        //                    updateCameraBloomAnamorphicRatio(_val);
        //                if (value == 4)
        //                    updateCameraBloomColor(_val);
        //                break;
        //            }
        //    }
        //}

        //public List<EventSequence> eventSequences = new List<EventSequence>
        //{
        //    new EventSequence("Move", 0),
        //    new EventSequence("Zoom", 1),
        //    new EventSequence("Rotate", 2),
        //    new EventSequence("Shake", 3),
        //    new EventSequence("Theme", 4),
        //    new EventSequence("Chromatic", 5),
        //    new EventSequence("Bloom", 6),
        //    new EventSequence("Vignette", 7),
        //    new EventSequence("Lens", 8),
        //    new EventSequence("Grain", 9),
        //    new EventSequence("ColorGrading", 10),
        //    new EventSequence("Ripples", 11),
        //    new EventSequence("RadialBlur", 12),
        //    new EventSequence("ColorSplit", 13),
        //    new EventSequence("Offset", 14),
        //    new EventSequence("Gradient", 15),
        //    new EventSequence("DoubleVision", 16),
        //    new EventSequence("ScanLines", 17),
        //    new EventSequence("Blur", 18),
        //    new EventSequence("Pixelize", 19),
        //    new EventSequence("BG", 20),
        //    new EventSequence("Screen Overlay", 21),
        //    new EventSequence("Timeline", 22),
        //    new EventSequence("Player", 23),
        //    new EventSequence("Follow Player", 24),
        //    new EventSequence("Audio", 25),
        //};

        //public Dictionary<string, EventSequence> eventSequencesDictionary
        //{
        //    get
        //    {
        //        var dictionary = new Dictionary<string, EventSequence>();
        //        foreach (var sequence in eventSequences)
        //        {
        //            dictionary.Add(sequence.name, sequence);
        //        }

        //        return dictionary;
        //    }
        //}

        //public class EventSequence
        //{
        //    public EventSequence()
        //    {

        //    }

        //    public EventSequence(string name = "", int type = 0)
        //    {
        //        this.name = name;
        //        this.type = type;
        //        sequence = DOTween.Sequence();
        //    }

        //    public int type;
        //    public string name;

        //    public Sequence sequence;
        //}

        #endregion
    }
}

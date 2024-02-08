using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

namespace EventsCore
{
    [HarmonyPatch(typeof(DataManager.GameData.EventObjects), MethodType.Constructor)]
    public class EventsInstance
    {
        public static void Postfix(DataManager.GameData.EventObjects __instance)
        {
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // Ripples
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // RadialBlur
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // ColorSplit
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // Camera Offset
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // Gradient
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // DoubleVision
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // ScanLines
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // Blur
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // Pixelize
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // BG
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // Overlay
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // Timeline
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // Player
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // Follow Player
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // Music
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // Video BG Parent
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // Video BG
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // Sharpen
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // Bars
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // 3D Rotation
            __instance.allEvents.Add(new List<DataManager.GameData.EventKeyframe>()); // Camera Depth
        }
    }
}

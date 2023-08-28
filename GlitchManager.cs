using System.Reflection;
using System.IO;
using UnityEngine;

using RTFunctions.Functions;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;

namespace EventsCore
{
    public class GlitchManager : MonoBehaviour
    {
        private static DigitalGlitch digitalGlitch;
        private static AnalogGlitch analogGlitch;
        public static GlitchManager inst;

        public void updateCameraDigitalIntensity(float _intensity)
        {
            digitalGlitch.intensity = _intensity;
        }

        public void updateCameraAnalogColorDrift(float _analog)
        {
            analogGlitch.colorDrift = _analog;
        }

        public void updateCameraAnalogHShake(float _analog)
        {
            analogGlitch.horizontalShake = _analog;
        }

        public void updateCameraAnalogJitter(float _analog)
        {
            analogGlitch.scanLineJitter = _analog;
        }

        public void updateCameraAnalogJump(float _analog)
        {
            analogGlitch.verticalJump = _analog;
        }

        private void Awake()
        {
            inst = this;

            kinoglitch = GetAssetBundle(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets", "kinoglitch");
            //var assetBundle = RTFile.GetAssetBundle(RTFile.GetApplicationDirectory() + "BepInEx/plugins/Assets", "kinoglitch");
            //Debug.LogFormat("{0}AssetBundle: {1}", EventsCorePlugin.className, assetBundle);
            //Debug.LogFormat("{0}All Assets: {1}", EventsCorePlugin.className, assetBundle.GetAllAssetNames().Length);
            //for (int i = 0; i < assetBundle.GetAllAssetNames().Length; i++)
            //{
            //    Debug.LogFormat("{0}Current Asset: {1}", EventsCorePlugin.className, assetBundle.GetAllAssetNames()[i]);
            //}
            //var digitalGlitchShader = assetBundle.LoadAsset<Shader>("digitalglitch.shader");
            //var analogGlitchShader = assetBundle.LoadAsset<Shader>("analogglitch.shader");
            //Debug.LogFormat("{0}DigitalGlitch: {1}", EventsCorePlugin.className, digitalGlitchShader);
            //Debug.LogFormat("{0}AnalogGlitch: {1}", EventsCorePlugin.className, analogGlitchShader);

            if (!GetComponent<DigitalGlitch>())
            {
                gameObject.AddComponent<DigitalGlitch>();
            }
            if (!GetComponent<AnalogGlitch>())
            {
                gameObject.AddComponent<AnalogGlitch>();
            }

            digitalGlitch = GetComponent<DigitalGlitch>();
            analogGlitch = GetComponent<AnalogGlitch>();

            var shDG = digitalGlitch.GetType().GetField("_shader", BindingFlags.NonPublic | BindingFlags.Instance);
            var shAG = analogGlitch.GetType().GetField("_shader", BindingFlags.NonPublic | BindingFlags.Instance);

            Debug.LogFormat("{0}DigitalGlitch _shader: {1}", EventsCorePlugin.className, shDG);
            Debug.LogFormat("{0}AnalogGlitch _shader: {1}", EventsCorePlugin.className, shAG);
            
            shDG.SetValue(digitalGlitch, kinoglitch.LoadAsset<Shader>("digitalglitch.shader"));
            shAG.SetValue(analogGlitch, kinoglitch.LoadAsset<Shader>("analogglitch.shader"));

            //var analog = kinoglitch.LoadAsset<Shader>("hidden_kino_glitch_analog.shader")
        }

        public static Shader GetShader(string _shader)
        {
            var assetBundle = GetAssetBundle(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets", "kinoglitch");
            var shaderToLoad = assetBundle.LoadAsset<Shader>(_shader);
            var shader = Instantiate(shaderToLoad);
            assetBundle.Unload(false);
            return shader;
        }

        public static AssetBundle GetAssetBundle(string _filepath, string _bundle)
        {
            return AssetBundle.LoadFromFile(Path.Combine(_filepath, _bundle));
        }


        public static AssetBundle kinoglitch;
    }
}

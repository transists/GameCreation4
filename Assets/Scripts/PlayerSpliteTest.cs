using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerSpliteTest : MonoBehaviour
{
    
    // Start is called before the first frame update
    void Start()
    {
        var lights = FindObjectsOfType<Light2D>(true);
        Debug.Log($"[LightScanner2D] Found {lights.Length} Light2D");

        foreach (var l in lights)
        {
            string type = l.lightType.ToString();
            string name = l.name;

            // バージョン差吸収：lightLayerMask / lightLayers / m_LightLayerMask を反射で読む
            object maskObj = null;
            foreach (var mem in new[] { "lightLayerMask", "lightLayers", "m_LightLayerMask" })
            {
                var p = l.GetType().GetProperty(mem, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null) { maskObj = p.GetValue(l, null); break; }
                var f = l.GetType().GetField(mem, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null) { maskObj = f.GetValue(l); break; }
            }

            string maskText = (maskObj != null) ? maskObj.ToString() : "(mask unreadable)";
            Debug.Log($"[LightScanner2D] {name}  Type={type}  Mask={maskText}", l);
        }

        Debug.LogWarning("[LightScanner2D] Global Light が SR_LitOnly の Light Layer を含んでいないか、ここで確認してください。Spot は含む、Global は外す、が正解です。");
    }




    // Update is called once per frame
    void Update()
    {
        
    }

   
}

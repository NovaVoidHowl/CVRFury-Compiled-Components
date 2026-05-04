using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace uk.novavoidhowl.dev.cvrfury.VRCstub.Common
{
  public static class SharedUIStyles
  {
    public static void ApplySharedStyles(VisualElement root)
    {
      var sheet = LoadEmbeddedStyleSheet();
      if (sheet != null)
      {
        root.styleSheets.Add(sheet);
      }
      else
      {
        // Fallback minimal padding
        root.style.paddingTop = 10;
        root.style.paddingBottom = 10;
        root.style.paddingLeft = 10;
        root.style.paddingRight = 10;
      }
    }

    private static StyleSheet LoadEmbeddedStyleSheet()
    {
      try
      {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "uk.novavoidhowl.dev.cvrfury.VRCstub.Resources.VRCStubUICore.uss";

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
          if (stream == null)
            return null;
          using (var reader = new StreamReader(stream))
          {
            var cssContent = reader.ReadToEnd();
            var assetsPath = "Assets/Temp";
            var tempUssPath = "Assets/Temp/CVRFuryStubs_Shared.uss";
            if (!Directory.Exists(assetsPath))
              Directory.CreateDirectory(assetsPath);
            File.WriteAllText(tempUssPath, cssContent);
            AssetDatabase.ImportAsset(tempUssPath);
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(tempUssPath);
          }
        }
      }
      catch
      {
        return null;
      }
    }
  }
}

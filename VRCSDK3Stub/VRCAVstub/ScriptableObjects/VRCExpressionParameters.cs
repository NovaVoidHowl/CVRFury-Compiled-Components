using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using StubVersion = uk.novavoidhowl.dev.cvrfury.VRCAVstub.Common.StubVersion;

namespace VRC.SDK3.Avatars.ScriptableObjects
{
  public class VRCExpressionParameters : ScriptableObject
  {
    public const int MAX_PARAMETER_COST = 256;
    public Parameter[] parameters;

    [System.Serializable]
    public enum ValueType
    {
      Int = 0,
      Float = 1,
      Bool = 2
    }

    [System.Serializable]
    public class Parameter
    {
      public string name;
      public ValueType valueType;
      public bool saved;
      public float defaultValue;
    }

    [SerializeField]
    private string _stubVersion = null;

    private void OnValidate()
    {
      if (string.IsNullOrEmpty(_stubVersion))
      {
        _stubVersion = uk.novavoidhowl.dev.cvrfury.VRCAVstub.Common.StubVersion.CurrentVersion;
      }
    }

    public string StubVersion
    {
      get { return _stubVersion ?? uk.novavoidhowl.dev.cvrfury.VRCAVstub.Common.StubVersion.CurrentVersion; }
      private set { _stubVersion = value; }
    }

    public static Version GetStubVersion()
    {
      return uk.novavoidhowl.dev.cvrfury.VRCAVstub.Common.StubVersion.AsVersion;
    }
  }

  [CustomEditor(typeof(VRCExpressionParameters))]
  public class VRCExpressionParametersEditorStub : Editor
  {
    public override VisualElement CreateInspectorGUI()
    {
      var root = new VisualElement();

      var versionLabel = new Label($"Stub Version: {((VRCExpressionParameters)target).StubVersion}");
      versionLabel.style.marginBottom = new StyleLength(10);
      root.Add(versionLabel);

      var warningBox = new Box();
      warningBox.style.marginTop = new StyleLength(10);
      warningBox.style.paddingTop = new StyleLength(6);
      warningBox.style.paddingBottom = new StyleLength(6);
      warningBox.style.paddingLeft = new StyleLength(6);
      warningBox.style.paddingRight = new StyleLength(6);
      warningBox.style.backgroundColor = new StyleColor(new Color(1f, 0.8f, 0.8f, 0.3f));

      var warningLabel = new Label(
        "This file needs to be converted for data import to CVRFury, please click the below button to open the converter"
      );
      warningLabel.style.whiteSpace = WhiteSpace.Normal;
      warningBox.Add(warningLabel);

      var convertButton = new Button(() =>
      {
        EditorApplication.ExecuteMenuItem("NVH/CVRFury/Conversion Tools/Convert VRCExpressionParameters");
      })
      {
        text = "Open VRCExpressionParameters Converter"
      };
      convertButton.style.marginTop = new StyleLength(10);

      root.Add(warningBox);
      root.Add(convertButton);

      return root;
    }
  }
}

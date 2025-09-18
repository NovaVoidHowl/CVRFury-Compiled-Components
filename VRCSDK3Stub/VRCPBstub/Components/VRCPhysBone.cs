using VRC.Dynamics;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System;
using StubVersion = uk.novavoidhowl.dev.cvrfury.VRCPBstub.Common.StubVersion;

namespace VRC.SDK3.Dynamics.PhysBone.Components
{
  public class VRCPhysBone : VRCPhysBoneBase 
  {
    [SerializeField]
    private string _stubVersion = null;

    private void OnValidate()
    {
      _stubVersion = uk.novavoidhowl.dev.cvrfury.VRCPBstub.Common.StubVersion.CurrentVersion;
    }

    public string StubVersion
    {
      get { return _stubVersion ?? uk.novavoidhowl.dev.cvrfury.VRCPBstub.Common.StubVersion.CurrentVersion; }
    }
  }

  [CustomEditor(typeof(VRCPhysBone))]
  public class VRCPhysBoneEditorStub : Editor
  {
    public override VisualElement CreateInspectorGUI()
    {
      var root = new VisualElement();

      var warningBox = new Box();
      warningBox.style.marginTop = new StyleLength(10);
      warningBox.style.paddingTop = new StyleLength(6);
      warningBox.style.paddingBottom = new StyleLength(6);
      warningBox.style.paddingLeft = new StyleLength(6);
      warningBox.style.paddingRight = new StyleLength(6);
      warningBox.style.backgroundColor = new StyleColor(new Color(1f, 0.8f, 0.8f, 0.3f));

      var warningLabel = new Label("This component needs to be converted for use in CVR");
      warningLabel.style.whiteSpace = WhiteSpace.Normal;
      warningBox.Add(warningLabel);

      root.Add(warningBox);

      return root;
    }
  }
}

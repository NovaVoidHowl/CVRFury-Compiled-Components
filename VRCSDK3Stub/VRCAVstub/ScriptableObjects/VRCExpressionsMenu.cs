using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace VRC.SDK3.Avatars.ScriptableObjects
{
  public class VRCExpressionsMenu : ScriptableObject
  {
    public const int MAX_CONTROLS = 8;
    public List<Control> controls;

    [Serializable]
    public class Control
    {
      public string name;
      public Texture2D icon;
      public ControlType type;
      public Parameter parameter;
      public float value;
      public Style style;
      public VRCExpressionsMenu subMenu;
      public Parameter[] subParameters;
      public Label[] labels;

      [Serializable]
      public enum ControlType
      {
        Button = 101,
        Toggle = 102,
        SubMenu = 103,
        TwoAxisPuppet = 201,
        FourAxisPuppet = 202,
        RadialPuppet = 203
      }

      [Serializable]
      public enum Style
      {
        Style1 = 0,
        Style2 = 1,
        Style3 = 2,
        Style4 = 3
      }

      [Serializable]
      public struct Label
      {
        public string name;
        public Texture2D icon;
      }

      [Serializable]
      public class Parameter
      {
        public string name;
        public int hash { get; }
      }
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
}

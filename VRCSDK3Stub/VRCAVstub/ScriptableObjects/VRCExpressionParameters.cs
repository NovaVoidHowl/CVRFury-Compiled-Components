using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
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
      _stubVersion = uk.novavoidhowl.dev.cvrfury.VRCAVstub.Common.StubVersion.CurrentVersion;
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

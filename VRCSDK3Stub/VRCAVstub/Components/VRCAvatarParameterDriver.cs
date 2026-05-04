using System;
using System.Collections.Generic;
using UnityEngine;
using StubVersion = uk.novavoidhowl.dev.cvrfury.VRCAVstub.Common.StubVersion;

namespace VRC.SDK3.Avatars.Components
{
  public class VRCAvatarParameterDriver : StateMachineBehaviour
  {
    public enum ChangeType
    {
      Set,
      Add,
      Random,
      Copy
    }

    [Serializable]
    public class Parameter
    {
      public ChangeType type;
      public string name;
      public string source;
      public float value;
      public float valueMin;
      public float valueMax = 1f;

      [Range(0f, 1f)]
      public float chance = 1f;
      public bool convertRange;
      public float sourceMin;
      public float sourceMax;
      public float destMin;
      public float destMax;
      public object sourceParam;
      public object destParam;
    }

    public List<Parameter> parameters = new List<Parameter>();
    public bool localOnly;
    public string debugString;

    [SerializeField]
    private string _stubVersion = null;

    private void OnValidate()
    {
      _stubVersion = uk.novavoidhowl.dev.cvrfury.VRCAVstub.Common.StubVersion.CurrentVersion;
    }

    public string StubVersion
    {
      get { return _stubVersion ?? uk.novavoidhowl.dev.cvrfury.VRCAVstub.Common.StubVersion.CurrentVersion; }
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
      // dummy override code to force unity to allow this script to be added to an animator state
      // we only need this for the data it holds, we don't need it to actually do anything
    }
  }
}

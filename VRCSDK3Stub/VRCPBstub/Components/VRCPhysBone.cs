using VRC.Dynamics;
using UnityEngine;
using UnityEngine.UIElements;
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
  // Editor moved to VRCPBUIAndConverter Editors assembly.
}

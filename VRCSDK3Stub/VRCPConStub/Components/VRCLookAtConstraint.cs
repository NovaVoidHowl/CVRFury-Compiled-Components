using UnityEngine;
using VRC.Dynamics.ManagedTypes;
using System;
using StubVersion = uk.novavoidhowl.dev.cvrfury.VRCPConStub.Common.StubVersion;

namespace VRC.SDK3.Dynamics.Constraint.Components
{
  public sealed class VRCLookAtConstraint : VRCLookAtConstraintBase
  {
    [SerializeField]
    private string _stubVersion = null;

    private void OnValidate()
    {
      _stubVersion = uk.novavoidhowl.dev.cvrfury.VRCPConStub.Common.StubVersion.CurrentVersion;
    }

    public string StubVersion
    {
      get { return _stubVersion ?? uk.novavoidhowl.dev.cvrfury.VRCPConStub.Common.StubVersion.CurrentVersion; }
    }
  }
}

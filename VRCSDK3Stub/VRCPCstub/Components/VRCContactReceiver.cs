using UnityEngine;
using StubVersion = uk.novavoidhowl.dev.cvrfury.VRCPCstub.Common.StubVersion;

namespace VRC.SDK3.Dynamics.Contact.Components
{
  public class VRCContactReceiver : VRC.Dynamics.ContactReceiver
  {
    [SerializeField]
    private string _stubVersion = null;

    private void OnValidate()
    {
      _stubVersion = uk.novavoidhowl.dev.cvrfury.VRCPCstub.Common.StubVersion.CurrentVersion;
    }

    public string StubVersion
    {
      get { return _stubVersion ?? uk.novavoidhowl.dev.cvrfury.VRCPCstub.Common.StubVersion.CurrentVersion; }
    }
  }
}

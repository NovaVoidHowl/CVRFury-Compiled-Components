using System;
using UnityEngine;
using StubVersion = uk.novavoidhowl.dev.cvrfury.VRCPMstub.Common.StubVersion;

namespace VRC.Core
{
  public class PipelineManager : MonoBehaviour
  {
    public string blueprintId;
    public ContentType contentType;

    [Serializable]
    public enum ContentType
    {
      avatar = 0,
      world = 1
    }

    [SerializeField]
    private string _stubVersion = null;

    private void OnValidate()
    {
      _stubVersion = uk.novavoidhowl.dev.cvrfury.VRCPMstub.Common.StubVersion.CurrentVersion;
    }

    public string StubVersion
    {
      get { return _stubVersion ?? uk.novavoidhowl.dev.cvrfury.VRCPMstub.Common.StubVersion.CurrentVersion; }
    }
  }
}

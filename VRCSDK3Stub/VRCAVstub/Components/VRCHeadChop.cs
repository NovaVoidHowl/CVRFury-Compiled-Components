using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using StubVersion = uk.novavoidhowl.dev.cvrfury.VRCAVstub.Common.StubVersion;

namespace VRC.SDK3.Avatars.Components
{
  public class VRCHeadChop : MonoBehaviour
  {
    [Serializable]
    public class HeadChopBone
    {
      public enum ApplyCondition
      {
        AlwaysApply,
        VrOnly,
        NonVrOnly
      }

      [Tooltip("The bone transform to apply scaling to.")]
      public Transform transform;

      [Tooltip(
        "The scale factor to apply to this specific bone, ranging from 0 (bone fully scaled away) to 1"
          + " (bone uses its usual scale)."
      )]
      [Range(0f, 1f)]
      public float scaleFactor;

      [Tooltip("A condition controlling whether this bone will be scaled away.")]
      public ApplyCondition applyCondition;

      public Transform Transform => transform;

      public bool CanApply(bool isUserInVr)
      {
        switch (applyCondition)
        {
          case ApplyCondition.VrOnly:
            return isUserInVr;
          case ApplyCondition.NonVrOnly:
            return !isUserInVr;
          default:
            return true;
        }
      }

      public float GetDesiredScaleFactor()
      {
        return Mathf.Clamp01(scaleFactor);
      }
    }

    public struct HeadChopData
    {
      public float DesiredAppliedScaleFactor;
      public Vector3 OriginalLocalPosition;
      public Vector3 OriginalRootSpacePosition;
      public Vector3 OriginalLocalScale;
    }

    [Tooltip("Lists the bones that will be scaled away for the local player.")]
    public HeadChopBone[] targetBones = Array.Empty<HeadChopBone>();

    [Tooltip(
      "A global scale applied to all bones targeted by this component, ranging from 0 "
        + "(all bones fully scaled away) to 1 (all bones use their individual scale factors)."
    )]
    [Range(0f, 1f)]
    public float globalScaleFactor = 1f;

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

    private const int MaxBoneCount = 32;

    public const int MaxComponentCount = 16;
  }
}

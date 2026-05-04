using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;
using System;
using System.Linq;
using StubVersion = uk.novavoidhowl.dev.cvrfury.VRCAVstub.Common.StubVersion;

namespace VRC.SDK3.Avatars.Components
{
  public class VRCAvatarDescriptor : MonoBehaviour
  {
    public string Name;
    public Quaternion portraitCameraRotationOffset;
    public Vector3 portraitCameraPositionOffset;
    public string unityVersion;

    [HideInInspector]
    public object apiAvatar;
    public string[] VisemeBlendShapes;
    public SkinnedMeshRenderer VisemeSkinnedMesh;
    public string MouthOpenBlendShapeName;
    public Quaternion lipSyncJawClosed;
    public Transform lipSyncJawBone;
    public LipSyncStyle lipSync;
    public bool ScaleIPD;
    public AnimationSet Animations;
    public Vector3 ViewPosition;
    public Quaternion lipSyncJawOpen;

    [System.Serializable]
    public enum LipSyncStyle
    {
      Default = 0,
      JawFlapBone = 1,
      JawFlapBlendShape = 2,
      VisemeBlendShape = 3,
      VisemeParameterOnly = 4
    }

    [System.Serializable]
    public enum AnimationSet
    {
      Male = 0,
      Female = 1,
      None = 2
    }

    [System.Serializable]
    public enum Viseme
    {
      sil = 0,
      PP = 1,
      FF = 2,
      TH = 3,
      DD = 4,
      kk = 5,
      CH = 6,
      SS = 7,
      nn = 8,
      RR = 9,
      aa = 10,
      E = 11,
      ih = 12,
      oh = 13,
      ou = 14,
      Count = 15
    }

    public const float COLLIDER_MAX_SIZE = 6;
    public bool customExpressions;
    public ColliderConfig collider_fingerLittleR;
    public ColliderConfig collider_fingerRingR;
    public ColliderConfig collider_fingerMiddleR;
    public ColliderConfig collider_fingerIndexR;
    public ColliderConfig collider_fingerLittleL;
    public ColliderConfig collider_fingerRingL;
    public ColliderConfig collider_fingerMiddleL;
    public ColliderConfig collider_fingerIndexL;
    public ColliderConfig collider_handL;
    public ColliderConfig collider_handR;
    public ColliderConfig collider_footL;
    public ColliderConfig collider_footR;
    public ColliderConfig collider_torso;
    public ColliderConfig collider_head;
    public bool autoLocomotion;
    public VRCExpressionsMenu expressionsMenu;
    public VRCExpressionParameters expressionParameters;
    public CustomEyeLookSettings customEyeLookSettings;
    public bool customizeAnimationLayers;
    public bool enableEyeLook;
    public CustomAnimLayer[] specialAnimationLayers;
    public ScriptableObject AnimationPreset;

    [HideInInspector]
    public List<DebugHash> animationHashSet;
    public bool autoFootsteps;
    public CustomAnimLayer[] baseAnimationLayers;

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

    [System.Serializable]
    public enum AnimLayerType
    {
      Base = 0,
      Deprecated0 = 1,
      Additive = 2,
      Gesture = 3,
      Action = 4,
      FX = 5,
      Sitting = 6,
      TPose = 7,
      IKPose = 8
    }

    [System.Serializable]
    public enum EyelidType
    {
      None = 0,
      Bones = 1,
      Blendshapes = 2
    }

    [System.Serializable]
    public struct DebugHash
    {
      public int hash;
      public string name;
    }

    [System.Serializable]
    public struct CustomAnimLayer
    {
      public bool isEnabled;
      public AnimLayerType type;
      public RuntimeAnimatorController animatorController;
      public AvatarMask mask;
      public bool isDefault;
    }

    [System.Serializable]
    public struct CustomEyeLookSettings
    {
      public EyeMovements eyeMovement;
      public int[] eyelidsBlendshapes;
      public SkinnedMeshRenderer eyelidsSkinnedMesh;
      public EyelidRotations eyelidsLookingDown;
      public EyelidRotations eyelidsLookingUp;
      public EyelidRotations eyelidsClosed;
      public EyelidRotations eyelidsDefault;
      public Transform lowerRightEyelid;
      public Transform lowerLeftEyelid;
      public Transform upperRightEyelid;
      public EyelidType eyelidType;
      public EyeRotations eyesLookingRight;
      public EyeRotations eyesLookingLeft;
      public EyeRotations eyesLookingDown;
      public EyeRotations eyesLookingUp;
      public EyeRotations eyesLookingStraight;
      public Transform rightEye;
      public Transform leftEye;
      public Transform upperLeftEyelid;

      public class EyeRotations
      {
        public bool linked;
        public Quaternion left;
        public Quaternion right;
      }

      public class EyeMovements
      {
        public float confidence;
        public float excitement;
      }

      public class EyelidRotations
      {
        public EyeRotations upper;
        public EyeRotations lower;
      }
    }

    [System.Serializable]
    public struct ColliderConfig
    {
      public bool isMirrored;
      public State state;
      public Transform transform;
      public float radius;
      public float height;
      public Vector3 position;
      public Quaternion rotation;

      public Vector3 axis { get; }

      public enum State
      {
        Automatic = 0,
        Custom = 1,
        Disabled = 2
      }
    }
  }
}

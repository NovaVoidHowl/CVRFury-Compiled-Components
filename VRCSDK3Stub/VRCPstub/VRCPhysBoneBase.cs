using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRC.Dynamics
{
  public abstract class VRCPhysBoneBase : MonoBehaviour
  {
    public const string PARAM_ISGRABBED = "_IsGrabbed";
    public const string PARAM_ANGLE = "_Angle";
    public const string PARAM_STRETCH = "_Stretch";
    public static Action<VRCPhysBoneBase> OnInitialize;
    public static Func<int, int, bool> OnVerifyCollision;

    [Range(0, 1)]
    public float limitOpacity;
    public bool showGizmos;

    public string parameter;

    public bool isAnimated;
    public AnimationCurve maxStretchCurve;

    [Range(0, 5)]
    public float maxStretch;

    [Range(0, 1)]
    public float grabMovement;

    [Tooltip("Allows players to pose the bones after grabbing.")]
    public bool allowPosing;

    public bool allowGrabbing;
    public Vector3 staticFreezeAxis;
    public bool configHasUpdated;
    public List<Bone> bones;
    public Action OnPoseUpdated;
    public ulong chainId;
    public AnimationCurve limitRotationZCurve;
    public int playerId;
    public bool collidersHaveUpdated;
    public CollisionScene.Shape shape;
    public List<CollisionRecord> collisionRecords;
    public bool param_IsGrabbedValue;
    public float param_AngleValue;
    public float param_StretchValue;
    public IAnimParameterAccess param_IsGrabbed;
    public IAnimParameterAccess param_Angle;
    public IAnimParameterAccess param_Stretch;
    public int maxBoneChainIndex;
    public AnimationCurve limitRotationYCurve;

    [Range(0, 1)]
    public float boneOpacity;

    public Vector3 limitRotation;

    public IntegrationType integrationType;
    public AnimationCurve limitRotationXCurve;

    public List<Transform> ignoreTransforms;

    public Vector3 endpointPosition;

    public MultiChildType multiChildType;

    [Range(0, 1)]
    public float pull;
    public AnimationCurve pullCurve;

    [Range(0, 1)]
    public float spring;
    public AnimationCurve springCurve;

    [Range(0, 1)]
    public float stiffness;
    public AnimationCurve stiffnessCurve;

    [Range(-1, 1)]
    public float gravity;
    public AnimationCurve gravityCurve;

    [Range(0, 1)]
    public float gravityFalloff;

    public Transform rootTransform;

    public ImmobileType immobileType;
    public AnimationCurve gravityFalloffCurve;
    public AnimationCurve maxAngleZCurve;

    [Range(0, 90)]
    public float maxAngleZ;

    [Range(0, 180)]
    public float maxAngleX;

    public LimitType limitType;

    public List<VRCPhysBoneColliderBase> colliders;
    public AnimationCurve maxAngleXCurve;

    public float radius;

    public bool allowCollision;
    public AnimationCurve immobileCurve;

    [Range(0, 1)]
    public float immobile;
    public AnimationCurve radiusCurve;

    [Serializable]
    public enum IntegrationType
    {
      Simplified = 0,
      Advanced = 1
    }

    [Serializable]
    public enum MultiChildType
    {
      Ignore = 0,
      First = 1,
      Average = 2
    }

    [Serializable]
    public enum ImmobileType
    {
      AllMotion = 0,
      World = 1
    }

    [Serializable]
    public enum LimitType
    {
      None = 0,
      Angle = 1,
      Hinge = 2,
      Polar = 3
    }

    [Serializable]
    public struct Bone
    {
      public Transform transform;
      public int parentIndex;
      public int childIndex;
      public int boneChainIndex;
      public int childCount;
      public Vector3 averageChildPos;
      public Vector3 restPosition;
      public Quaternion restRotation;
      public Vector3 restScale;
      public bool sphereCollision;

      public bool isEndBone { get; }
    }

    [Serializable]
    public struct CollisionRecord
    {
      public CollisionScene.Shape shape;
      public VRCPhysBoneColliderBase collider;
    }

    public class IAnimParameterAccess
    {
      bool boolVal { get; set; }
      int intVal { get; set; }
      float floatVal { get; set; }
    }
  }
}

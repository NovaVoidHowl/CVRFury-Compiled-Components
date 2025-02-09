using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;
using System;
using System.Linq;

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

  [CustomEditor(typeof(VRCAvatarDescriptor))]
  public class VRCAvatarDescriptorEditorStub : Editor
  {
    public override VisualElement CreateInspectorGUI()
    {
      var root = new VisualElement();

      var warningBox = new Box();
      warningBox.style.marginTop = new StyleLength(10);
      warningBox.style.paddingTop = new StyleLength(6);
      warningBox.style.paddingBottom = new StyleLength(6);
      warningBox.style.paddingLeft = new StyleLength(6);
      warningBox.style.paddingRight = new StyleLength(6);
      warningBox.style.backgroundColor = new StyleColor(new Color(1f, 0.8f, 0.8f, 0.3f));

      var warningLabel = new Label(
        "This component needs to be converted for use in CVR, please click the below button to run the conversion"
      );
      warningLabel.style.whiteSpace = WhiteSpace.Normal;
      warningBox.Add(warningLabel);

      var convertButton = new Button(() =>
      {
        var vrcAvatar = (VRCAvatarDescriptor)target;
        var avatar = vrcAvatar.gameObject;

        // Find required component types
        var cvrAvatarType = AppDomain.CurrentDomain
          .GetAssemblies()
          .SelectMany(a => a.GetTypes())
          .FirstOrDefault(t => t.Name == "CVRAvatar");
        var colliderInfoType = AppDomain.CurrentDomain
          .GetAssemblies()
          .SelectMany(a => a.GetTypes())
          .FirstOrDefault(t => t.Name == "CVRFuryAvatarColliderInfoUnit");

        if (cvrAvatarType != null && colliderInfoType != null)
        {
          // Add components
          var cvrComponent = avatar.AddComponent(cvrAvatarType);
          var colliderComponent = avatar.AddComponent(colliderInfoType);

          // Copy view position and body mesh
          var viewPositionField = cvrAvatarType.GetField("viewPosition");
          if (viewPositionField != null)
          {
            viewPositionField.SetValue(cvrComponent, vrcAvatar.ViewPosition);
          }
          else
          {
            var viewPositionProperty = cvrAvatarType.GetProperty("viewPosition");
            if (viewPositionProperty != null)
            {
              viewPositionProperty.SetValue(cvrComponent, vrcAvatar.ViewPosition, null);
            }
          }

          // Copy body mesh
          var bodyMeshField = cvrAvatarType.GetField("bodyMesh");
          if (bodyMeshField != null)
          {
            bodyMeshField.SetValue(cvrComponent, vrcAvatar.VisemeSkinnedMesh);
          }
          else
          {
            var bodyMeshProperty = cvrAvatarType.GetProperty("bodyMesh");
            if (bodyMeshProperty != null)
            {
              bodyMeshProperty.SetValue(cvrComponent, vrcAvatar.VisemeSkinnedMesh, null);
            }
          }

          // Copy eye blink settings if available
          if (
            vrcAvatar.customEyeLookSettings.eyelidsBlendshapes != null
            && vrcAvatar.customEyeLookSettings.eyelidsBlendshapes.Length > 0
            && vrcAvatar.VisemeSkinnedMesh != null
          )
          {
            var blinkIndex = vrcAvatar.customEyeLookSettings.eyelidsBlendshapes[0];
            var blinkBlendshapeName = vrcAvatar.VisemeSkinnedMesh.sharedMesh.GetBlendShapeName(blinkIndex);

            var useBlinkField = cvrAvatarType.GetField("useBlinkBlendshapes");
            if (useBlinkField != null)
            {
              useBlinkField.SetValue(cvrComponent, true);
            }

            var blinkArrayField = cvrAvatarType.GetField("blinkBlendshape");
            if (blinkArrayField != null)
            {
              // Create array with single value if it's a string array
              var blinkArray = Array.CreateInstance(typeof(string), 4);
              blinkArray.SetValue(blinkBlendshapeName, 0);
              // add the other 3 empty slots
              blinkArray.SetValue("", 1);
              blinkArray.SetValue("", 2);
              blinkArray.SetValue("", 3);
              blinkArrayField.SetValue(cvrComponent, blinkArray);
            }
          }

          // Set viseme lipsync if available
          if (vrcAvatar.VisemeBlendShapes != null && vrcAvatar.VisemeBlendShapes.Length > 0)
          {
            var useVisemeField = cvrAvatarType.GetField("useVisemeLipsync");
            if (useVisemeField != null)
            {
              useVisemeField.SetValue(cvrComponent, true);

              // If we have exactly 15 visemes, copy them over
              if (vrcAvatar.VisemeBlendShapes.Length == 15)
              {
                var visemeArrayField = cvrAvatarType.GetField("visemeBlendshapes");
                if (visemeArrayField != null)
                {
                  var visemeArray = Array.CreateInstance(typeof(string), 15);
                  for (int i = 0; i < 15; i++)
                  {
                    visemeArray.SetValue(vrcAvatar.VisemeBlendShapes[i], i);
                  }
                  visemeArrayField.SetValue(cvrComponent, visemeArray);
                }
              }
            }
          }

          // Copy collider configs
          var sourceFields = typeof(VRCAvatarDescriptor)
            .GetFields()
            .Where(f => f.FieldType == typeof(VRCAvatarDescriptor.ColliderConfig));

          foreach (var sourceField in sourceFields)
          {
            var targetField = colliderInfoType.GetField(sourceField.Name);
            if (targetField != null)
            {
              // Get source value
              var sourceValue = (VRCAvatarDescriptor.ColliderConfig)sourceField.GetValue(vrcAvatar);

              // Create target ColliderConfig type
              var targetConfigType = targetField.FieldType;
              var targetValue = Activator.CreateInstance(targetConfigType);

              // Copy properties
              foreach (var prop in typeof(VRCAvatarDescriptor.ColliderConfig).GetFields())
              {
                var targetProp = targetConfigType.GetField(prop.Name);
                if (targetProp != null)
                {
                  var value = prop.GetValue(sourceValue);

                  // Special handling for enum types
                  if (prop.FieldType.IsEnum && targetProp.FieldType.IsEnum)
                  {
                    // Convert enum to its underlying value then create new enum of target type
                    int enumValue = (int)value;
                    value = Enum.ToObject(targetProp.FieldType, enumValue);
                  }

                  targetProp.SetValue(targetValue, value);
                }
              }

              // Set the converted value
              targetField.SetValue(colliderComponent, targetValue);
            }
          }

          // Show warning about voice position
          EditorUtility.DisplayDialog(
            "Avatar Component Conversion Notice",
            "The conversion process is complete, but please note that the voice position must be set manually as this data is not available in the VRChat avatar component.",
            "OK"
          );

          // Remove this component after everything is copied
          DestroyImmediate(vrcAvatar);
        }
        else
        {
          Debug.LogError("Could not find required component types. Make sure CVR SDK is imported.");
        }
      })
      {
        text = "Convert to Chillout VR Avatar Descriptor"
      };
      convertButton.style.marginTop = new StyleLength(10);

      root.Add(warningBox);
      root.Add(convertButton);

      return root;
    }
  }
}

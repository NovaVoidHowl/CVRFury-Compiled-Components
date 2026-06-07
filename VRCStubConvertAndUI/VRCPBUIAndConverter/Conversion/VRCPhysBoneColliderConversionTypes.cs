using System.Collections.Generic;
using UnityEngine;

namespace uk.novavoidhowl.dev.cvrfury.compiled.vrccolliders
{
  public enum ConversionMessageSeverity
  {
    Info,
    Warning,
    Error
  }

  public sealed class ConversionMessage
  {
    public ConversionMessageSeverity Severity;
    public string Code;
    public string Text;

    public ConversionMessage(ConversionMessageSeverity severity, string code, string text)
    {
      Severity = severity;
      Code = code;
      Text = text;
    }
  }

  [System.Flags]
  public enum PhysBoneColliderTarget
  {
    None = 0,
    DynamicBone = 1,
    MagicaCloth1 = 2,
    MagicaCloth2 = 4
  }

  public sealed class VRCPhysBoneColliderConversionOptions
  {
    public bool RegisterUndo = true;

    public static VRCPhysBoneColliderConversionOptions ForInspector()
    {
      return new VRCPhysBoneColliderConversionOptions();
    }

    public static VRCPhysBoneColliderConversionOptions ForBatch()
    {
      return new VRCPhysBoneColliderConversionOptions();
    }
  }

  public sealed class VRCPhysBoneColliderConversionAvailability
  {
    public bool CanConvertDynamicBone;
    public bool CanConvertMagicaCloth1;
    public bool CanConvertMagicaCloth2;
    public readonly List<ConversionMessage> Messages = new List<ConversionMessage>();
  }

  public sealed class VRCPhysBoneColliderConversionGuidance
  {
    public readonly List<ConversionMessage> Messages = new List<ConversionMessage>();
  }

  public sealed class VRCPhysBoneColliderConversionResult
  {
    public bool Success;
    public string SummaryTitle;
    public string SummaryMessage;
    public Transform EffectiveRoot;
    public readonly List<string> ConvertedTargets = new List<string>();
    public readonly List<GameObject> CreatedOrUpdatedObjects = new List<GameObject>();
    public readonly List<ConversionMessage> Messages = new List<ConversionMessage>();

    public static VRCPhysBoneColliderConversionResult Failed(string title, string message, string code)
    {
      var result = new VRCPhysBoneColliderConversionResult
      {
        Success = false,
        SummaryTitle = title,
        SummaryMessage = message
      };
      result.Messages.Add(new ConversionMessage(ConversionMessageSeverity.Error, code, message));
      return result;
    }
  }
}

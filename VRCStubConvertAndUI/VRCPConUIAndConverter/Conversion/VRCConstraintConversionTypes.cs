using System.Collections.Generic;
using UnityEngine;

namespace uk.novavoidhowl.dev.cvrfury.compiled.vrcconstraints
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

  public enum VRCConstraintKind
  {
    Aim,
    LookAt,
    Parent,
    Position,
    Rotation,
    Scale
  }

  public sealed class VRCConstraintConversionOptions
  {
    public bool RemoveSourceComponent = true;
    public bool RegisterUndo = true;

    public static VRCConstraintConversionOptions ForInspector()
    {
      return new VRCConstraintConversionOptions();
    }

    public static VRCConstraintConversionOptions ForBatch()
    {
      return new VRCConstraintConversionOptions();
    }
  }

  public sealed class VRCConstraintConversionAvailability
  {
    public bool IsAvailable;
    public VRCConstraintKind Kind;
    public readonly List<ConversionMessage> Messages = new List<ConversionMessage>();
  }

  public sealed class VRCConstraintConversionGuidance
  {
    public VRCConstraintKind Kind;
    public readonly List<ConversionMessage> Messages = new List<ConversionMessage>();
  }

  public sealed class VRCConstraintConversionResult
  {
    public bool Success;
    public bool SourceRemoved;
    public VRCConstraintKind Kind;
    public GameObject SourceGameObject;
    public Component CreatedConstraint;
    public string SummaryTitle;
    public string SummaryMessage;
    public readonly List<ConversionMessage> Messages = new List<ConversionMessage>();

    public static VRCConstraintConversionResult Failed(string title, string message, string code)
    {
      var result = new VRCConstraintConversionResult
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

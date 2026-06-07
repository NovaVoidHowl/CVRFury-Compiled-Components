using System;
using System.Collections.Generic;
using UnityEngine;

namespace uk.novavoidhowl.dev.cvrfury.compiled.vrccontacts
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

  public sealed class VRCContactConversionOptions
  {
    public bool RemoveSourceComponent = true;
    public bool RegisterUndo;

    public static VRCContactConversionOptions ForInspector()
    {
      return new VRCContactConversionOptions();
    }

    public static VRCContactConversionOptions ForBatch()
    {
      return new VRCContactConversionOptions { RegisterUndo = true };
    }
  }

  public sealed class VRCContactConversionAvailability
  {
    public bool IsAvailable;
    public readonly List<ConversionMessage> Messages = new List<ConversionMessage>();
  }

  public sealed class VRCContactConversionGuidance
  {
    public readonly List<ConversionMessage> Messages = new List<ConversionMessage>();
  }

  public sealed class VRCContactConversionResult
  {
    public bool Success;
    public bool SourceRemoved;
    public string SummaryTitle;
    public string SummaryMessage;
    public readonly List<GameObject> CreatedObjects = new List<GameObject>();
    public readonly List<ConversionMessage> Messages = new List<ConversionMessage>();

    public static VRCContactConversionResult Failed(string title, string message, string code)
    {
      var result = new VRCContactConversionResult
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

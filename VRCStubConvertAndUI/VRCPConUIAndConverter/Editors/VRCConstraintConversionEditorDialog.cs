using System.Text;
using UnityEditor;
using uk.novavoidhowl.dev.cvrfury.compiled.vrcconstraints;

namespace VRC.SDK3.Dynamics.Constraint.Editors
{
  internal static class VRCConstraintConversionEditorDialog
  {
    public static void Show(VRCConstraintConversionResult result)
    {
      var message = new StringBuilder(result.SummaryMessage);

      foreach (var conversionMessage in result.Messages)
      {
        if (conversionMessage.Severity == ConversionMessageSeverity.Info)
          continue;

        message.AppendLine();
        message.AppendLine(conversionMessage.Severity + ": " + conversionMessage.Text);
      }

      EditorUtility.DisplayDialog(result.SummaryTitle, message.ToString(), "OK");
    }
  }
}

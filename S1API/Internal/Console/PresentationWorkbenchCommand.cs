using System;
using System.Collections.Generic;
using S1API.Logging;
using S1API.Rendering;

namespace S1API.Internal.Console
{
    internal sealed class PresentationWorkbenchCommand :
        global::S1API.Console.BaseConsoleCommand
    {
        private static readonly Log Logger = new Log("PresentationWorkbench");

        public PresentationWorkbenchCommand()
        {
        }

        public override string CommandWord => "presentation_workbench";

        public override string CommandDescription =>
            "Open the local icon and equippable presentation authoring workbench.";

        public override string ExampleUsage =>
            "presentation_workbench <namespaced-id|list|close>";

        public override void ExecuteCommand(List<string> args)
        {
            if (args == null || args.Count == 0)
            {
                Logger.Msg($"Usage: {ExampleUsage}");
                return;
            }

            string action = args[0]?.Trim() ?? string.Empty;
            if (action.Equals("close", StringComparison.OrdinalIgnoreCase))
            {
                PresentationWorkbench.Close();
                Logger.Msg("Presentation workbench closed.");
                return;
            }

            if (action.Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                IReadOnlyList<string> ids =
                    PresentationWorkbenchRegistry.GetRegisteredIds();
                Logger.Msg(
                    ids.Count == 0
                        ? "No explicit presentation workbench definitions are registered. " +
                          "Product presentation profile IDs can still be opened directly."
                        : "Presentation workbench IDs: " +
                          string.Join(", ", ids));
                return;
            }

            if (!PresentationWorkbench.Open(action))
            {
                Logger.Warning(
                    $"Could not open presentation workbench '{action}'.");
            }
        }
    }
}

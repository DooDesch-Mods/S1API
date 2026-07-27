using System;
using System.Collections.Generic;
using S1API.Console;
using S1API.Logging;

namespace S1API.Internal.NPCWorkbench
{
    internal static class NPCWorkbenchRuntime
    {
        private static readonly Log Logger = new Log("NPCWorkbench");
        private static NPCWorkbenchView? _view;

        internal static bool IsOpen => _view != null;

        internal static void Open()
        {
            if (_view != null)
                return;

            try
            {
                _view = NPCWorkbenchView.Create();
            }
            catch (Exception ex)
            {
                Logger.Error($"Unable to open the NPC workbench: {ex}");
                _view?.Dispose();
                _view = null;
            }
        }

        internal static void Tick()
        {
            _view?.Tick();
        }

        internal static void Close()
        {
            _view?.Dispose();
            _view = null;
        }

        internal static void NotifyClosed(NPCWorkbenchView view)
        {
            if (ReferenceEquals(_view, view))
                _view = null;
        }
    }

    internal sealed class NPCWorkbenchCommand : BaseConsoleCommand
    {
        public NPCWorkbenchCommand()
        {
        }

        public override string CommandWord => "npcworkbench";
        public override string CommandDescription =>
            "Opens the local-only S1API NPC appearance editor and preview.";
        public override string ExampleUsage => "npcworkbench";

        public override void ExecuteCommand(List<string> args)
        {
            NPCWorkbenchRuntime.Open();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using S1API.Items.Buildable;
using S1API.Logging;
using S1API.Rendering;
using UnityEngine;

namespace S1API.Internal.Building
{
    /// <summary>
    /// Defers furniture thumbnail rendering until the gameplay scene provides the native icon rig.
    /// Definitions remain available during pre-load so native save restoration can resolve them.
    /// </summary>
    internal static class FurnitureIconRuntime
    {
        private static readonly Log Logger = new Log("FurnitureIconRuntime");
        private static readonly object Gate = new object();
        private static readonly Queue<Request> Pending = new Queue<Request>();
        private static bool _processing;

        internal static void Queue(
            BuildableItemDefinition definition,
            Transform model,
            int resolution)
        {
            bool startProcessor = false;
            lock (Gate)
            {
                Pending.Enqueue(new Request(definition, model, resolution));
                if (!_processing)
                {
                    _processing = true;
                    startProcessor = true;
                }
            }

            if (startProcessor)
                MelonCoroutines.Start(ProcessQueue());
        }

        private static IEnumerator ProcessQueue()
        {
            while (true)
            {
                Request request;
                lock (Gate)
                {
                    if (Pending.Count == 0)
                    {
                        _processing = false;
                        yield break;
                    }

                    request = Pending.Dequeue();
                }

                const int readinessFrames = 600;
                int readinessFrame = 0;
                while (!IconFactory.IsItemIconGeneratorReady && readinessFrame < readinessFrames)
                {
                    readinessFrame++;
                    yield return null;
                }

                if (!IconFactory.IsItemIconGeneratorReady)
                {
                    Logger.Warning(
                        $"Could not generate furniture icon for '{request.Definition.ID}': " +
                        "the native item-icon rendering rig did not become ready.");
                    continue;
                }

                yield return new WaitForEndOfFrame();
                GameObject iconModel = Object.Instantiate(request.Model.gameObject);
                iconModel.name = $"{request.Model.name}_IconPreview";
                Sprite? icon;
                try
                {
                    icon = IconFactory.GenerateIconSprite(
                        iconModel.transform,
                        request.Resolution);
                }
                finally
                {
                    Object.Destroy(iconModel);
                }
                if (icon == null)
                {
                    Logger.Warning(
                        $"Could not generate furniture icon for '{request.Definition.ID}': " +
                        "the native renderer returned no sprite.");
                    continue;
                }

                request.Definition.Icon = icon;
            }
        }

        private sealed class Request
        {
            internal Request(
                BuildableItemDefinition definition,
                Transform model,
                int resolution)
            {
                Definition = definition;
                Model = model;
                Resolution = resolution;
            }

            internal BuildableItemDefinition Definition { get; }
            internal Transform Model { get; }
            internal int Resolution { get; }
        }
    }
}

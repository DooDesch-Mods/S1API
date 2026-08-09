using System;
using S1API.Internal.Building;
using UnityEngine;

namespace S1API.Items.Buildable
{
    /// <summary>
    /// Composes a custom model into a native furniture definition, placement prefab, and inventory prefab.
    /// </summary>
    public sealed class FurnitureDefinitionBuilder
    {
        private string? _id;
        private string? _name;
        private string? _description;
        private GameObject? _model;
        private FurniturePlacementMode _placementMode = FurniturePlacementMode.Grid;
        private int _footprintWidth = 1;
        private int _footprintDepth = 1;
        private FurnitureSurfaceType _surfaceTypes = FurnitureSurfaceType.Wall;
        private bool _allowSurfaceRotation = true;
        private BuildSoundType _buildSound = BuildSoundType.Wood;
        private int _stackLimit = 10;
        private float _purchasePrice = 10f;
        private float _resellMultiplier = 0.5f;
        private Sprite? _icon;
        private bool _generateIcon = true;
        private int _generatedIconResolution = 512;

        internal FurnitureDefinitionBuilder()
        {
        }

        /// <summary>Sets the stable registry ID and player-facing text.</summary>
        public FurnitureDefinitionBuilder WithBasicInfo(string id, string name, string description)
        {
            _id = id;
            _name = name;
            _description = description;
            return this;
        }

        /// <summary>
        /// Sets the model used for the placed object, placement ghost, stored item, and generated icon.
        /// The supplied object is cloned and is never modified by S1API.
        /// </summary>
        public FurnitureDefinitionBuilder WithModel(GameObject model)
        {
            _model = model != null ? model : throw new ArgumentNullException(nameof(model));
            return this;
        }

        /// <summary>Chooses the native placement family.</summary>
        public FurnitureDefinitionBuilder WithPlacement(FurniturePlacementMode placementMode)
        {
            if (!Enum.IsDefined(typeof(FurniturePlacementMode), placementMode))
                throw new ArgumentOutOfRangeException(nameof(placementMode));

            _placementMode = placementMode;
            return this;
        }

        /// <summary>Sets the floor-grid footprint in 0.5 metre tiles.</summary>
        public FurnitureDefinitionBuilder WithFootprint(int width, int depth)
        {
            if (width < 1)
                throw new ArgumentOutOfRangeException(nameof(width), "Footprint width must be at least one tile.");
            if (depth < 1)
                throw new ArgumentOutOfRangeException(nameof(depth), "Footprint depth must be at least one tile.");

            _footprintWidth = width;
            _footprintDepth = depth;
            return this;
        }

        /// <summary>Configures valid surfaces and rotation for surface-placed furniture.</summary>
        public FurnitureDefinitionBuilder WithSurfacePlacement(
            FurnitureSurfaceType surfaceTypes,
            bool allowRotation = true)
        {
            if (surfaceTypes == FurnitureSurfaceType.None ||
                (surfaceTypes & ~FurnitureSurfaceType.All) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(surfaceTypes));
            }

            _surfaceTypes = surfaceTypes;
            _allowSurfaceRotation = allowRotation;
            return this;
        }

        /// <summary>Sets the sound family used when placement completes.</summary>
        public FurnitureDefinitionBuilder WithBuildSound(BuildSoundType buildSound)
        {
            _buildSound = buildSound;
            return this;
        }

        /// <summary>Sets the purchase and resale values.</summary>
        public FurnitureDefinitionBuilder WithPricing(float basePurchasePrice, float resellMultiplier = 0.5f)
        {
            _purchasePrice = Mathf.Max(0f, basePurchasePrice);
            _resellMultiplier = Mathf.Clamp01(resellMultiplier);
            return this;
        }

        /// <summary>Sets the inventory stack limit.</summary>
        public FurnitureDefinitionBuilder WithStackLimit(int stackLimit)
        {
            _stackLimit = Mathf.Clamp(stackLimit, 1, 999);
            return this;
        }

        /// <summary>Uses an existing inventory icon.</summary>
        public FurnitureDefinitionBuilder WithIcon(Sprite icon)
        {
            _icon = icon != null ? icon : throw new ArgumentNullException(nameof(icon));
            _generateIcon = false;
            return this;
        }

        /// <summary>
        /// Queues inventory-icon generation from the supplied model. The definition registers immediately,
        /// and S1API replaces its fallback icon when the gameplay rendering rig becomes available.
        /// </summary>
        public FurnitureDefinitionBuilder WithGeneratedIcon(int resolution = 512)
        {
            if (resolution < 64 || resolution > 2048)
                throw new ArgumentOutOfRangeException(nameof(resolution), "Icon resolution must be between 64 and 2048 pixels.");

            _generateIcon = true;
            _icon = null;
            _generatedIconResolution = resolution;
            return this;
        }

        /// <summary>
        /// Creates and registers the complete native buildable definition.
        /// Call this after the game's item registry and vanilla furniture definitions are available.
        /// Every multiplayer peer must perform the same registration.
        /// </summary>
        public BuildableItemDefinition Build()
        {
            Validate();

            FurnitureComposition composition = FurniturePrefabComposer.Compose(
                _id!,
                _model!,
                _placementMode,
                _footprintWidth,
                _footprintDepth,
                _surfaceTypes,
                _allowSurfaceRotation);

            var builder = new BuildableItemDefinitionBuilder(composition.TemplateDefinition)
                .WithBasicInfo(_id!, _name!, _description!, ItemCategory.Furniture)
                .WithBuildSound(_buildSound)
                .WithPricing(_purchasePrice, _resellMultiplier)
                .WithStackLimit(_stackLimit)
                .WithBuiltItem(composition.BuiltItem)
                .WithStoredItem(composition.StoredItem.gameObject)
                .WithEquippable(composition.Equippable);
            if (_icon != null)
                builder.WithIcon(_icon);

            BuildableItemDefinition definition = builder.Build();
            Transform? visual = composition.BuiltItem.transform.Find(
                BuildableGhostRuntime.FurnitureVisualName);
            if (visual == null)
                throw new InvalidOperationException("Composed furniture has no placement visual source.");

            BuildableGhostRuntime.RegisterVisualSource(
                _id!,
                visual.gameObject,
                BuildableGhostRuntime.FurnitureGhostVisualName,
                replaceExistingVisual: true);
            if (_generateIcon)
            {
                FurnitureIconRuntime.Queue(definition, visual, _generatedIconResolution);
            }

            return definition;
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(_id))
                throw new InvalidOperationException("Furniture ID must be configured before Build().");
            if (string.IsNullOrWhiteSpace(_name))
                throw new InvalidOperationException("Furniture name must be configured before Build().");
            if (_description == null)
                throw new InvalidOperationException("Furniture description must be configured before Build().");
            if (_model == null)
                throw new InvalidOperationException("Furniture model must be configured before Build().");
        }
    }
}

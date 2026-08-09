using S1API.Items.Buildable;
using UnityEngine;

namespace S1API.Tests.Items;

internal static class FurnitureApiCompileFixture
{
    internal static FurnitureDefinitionBuilder Configure(GameObject model, Sprite icon)
    {
        return FurnitureCreator.CreateBuilder()
            .WithBasicInfo("example.mod:sofa-chair", "Sofa Chair", "A compact chair.")
            .WithModel(model)
            .WithPlacement(FurniturePlacementMode.Grid)
            .WithFootprint(2, 2)
            .WithBuildSound(BuildSoundType.Wood)
            .WithPricing(175f)
            .WithStackLimit(4)
            .WithIcon(icon);
    }
}

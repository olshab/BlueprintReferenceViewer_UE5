using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.Utils;

namespace BlueprintReferenceViewer_UE5.Extensions.Components
{
    class UActorSpawner : UActorComponent
    {
        public string? Visualization;
        public List<string> ActivatedSceneElements = [];
        public List<string> DectivatedSceneElement = [];

        public UActorSpawner(UObject Object, string ComponentClass)
            : base(Object, ComponentClass)
        {
            var VisualizationProperty = Object.Properties
                .FirstOrDefault(x => x.Name.Text == "Visualization")?.Tag as SoftObjectProperty;
            if (VisualizationProperty?.Value is not null)
            {
                Visualization = VisualizationProperty.Value.AssetPathName.Text.SubstringBeforeLast('.');
            }

            var ActivatedSceneElementProperty = Object.Properties
                .FirstOrDefault(x => x.Name.Text == "ActivatedSceneElement")?.Tag as ArrayProperty;
            if (ActivatedSceneElementProperty?.Value is not null)
            {
                foreach (var ActorSpawnerProperty in ActivatedSceneElementProperty.Value.Properties.Cast<StructProperty>())
                {
                    var ActorSpawnerPropertyStruct = ActorSpawnerProperty.Value?.StructType as FStructFallback;
                    if (ActorSpawnerPropertyStruct is not null)
                    {
                        var SceneElementProperty = ActorSpawnerPropertyStruct.Properties
                            .FirstOrDefault(x => x.Name.Text == "SceneElement")?.Tag as SoftObjectProperty;
                        if (SceneElementProperty?.Value is not null)
                        {
                            ActivatedSceneElements.Add(SceneElementProperty.Value.AssetPathName.Text.SubstringBeforeLast('.'));
                        }
                    }
                }
            }

            var DeactivatedSceneElementProperty = Object.Properties
                .FirstOrDefault(x => x.Name.Text == "DeactivatedSceneElement")?.Tag as ArrayProperty;
            if (DeactivatedSceneElementProperty?.Value is not null)
            {
                foreach (var ActorSpawnerProperty in DeactivatedSceneElementProperty.Value.Properties.Cast<StructProperty>())
                {
                    var ActorSpawnerPropertyStruct = ActorSpawnerProperty.Value?.StructType as FStructFallback;
                    if (ActorSpawnerPropertyStruct is not null)
                    {
                        var SceneElementProperty = ActorSpawnerPropertyStruct.Properties
                            .FirstOrDefault(x => x.Name.Text == "SceneElement")?.Tag as SoftObjectProperty;
                        if (SceneElementProperty?.Value is not null)
                        {
                            DectivatedSceneElement.Add(SceneElementProperty.Value.AssetPathName.Text.SubstringBeforeLast('.'));
                        }
                    }
                }
            }
        }
    }
}

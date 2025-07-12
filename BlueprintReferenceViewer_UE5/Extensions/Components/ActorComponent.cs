using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects.Properties;

namespace BlueprintReferenceViewer_UE5.Extensions.Components
{
    public class UActorComponent
    {
        public readonly string ClassName;

        protected readonly UObject _object;

        public static UActorComponent CreateComponent(UObject Object, string ComponentClass)
        {
            return ComponentClass switch
            {
                "/Script/Engine.ChildActorComponent" => new UChildActorComponent(Object, ComponentClass),
                "/Script/DeadByDaylight.ActorSpawner" => new UActorSpawner(Object, ComponentClass),

                _ => new UActorComponent(Object, ComponentClass)
            };
        }

        public UActorComponent(UObject Object, string ComponentClass)
        {
            _object = Object;
            ClassName = ComponentClass;
        }

        public bool HasAnyTags(string[] Tags)
        {
            var ComponentTagsProperty = _object.Properties
                .FirstOrDefault(x => x.Name.ToString() == "ComponentTags")?.Tag as ArrayProperty;
            if (ComponentTagsProperty is null)
            {
                return false;
            }

            foreach (var ElementProperty in ComponentTagsProperty.Value!.Properties)
            {
                var TagProperty = ElementProperty as NameProperty;
                if (Tags.Contains(TagProperty!.Value.ToString()))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

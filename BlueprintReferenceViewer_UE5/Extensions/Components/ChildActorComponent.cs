using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects.Properties;

namespace BlueprintReferenceViewer_UE5.Extensions.Components
{
    public class UChildActorComponent : UActorComponent
    {
        public string? ChildActorClass;

        public UChildActorComponent(UObject Object, string ComponentClass)
            : base(Object, ComponentClass)
        {
            var ChildActorClassProperty = _object.Properties
                .FirstOrDefault(x => x.Name.Text == "ChildActorClass")?.Tag as ObjectProperty;
            if (ChildActorClassProperty is not null && ChildActorClassProperty?.Value is not null)
            {
                var ResolvedChildActorClass = ChildActorClassProperty.Value.ResolvedObject;
                ChildActorClass = ResolvedChildActorClass?.Package.Name;
            }
        }
    }
}

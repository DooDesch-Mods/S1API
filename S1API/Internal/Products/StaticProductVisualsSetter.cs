#if IL2CPPMELON
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using S1Product = Il2CppScheduleOne.Product;
#elif MONOMELON
using S1Product = ScheduleOne.Product;
#endif

using System;
using MelonLoader;
using UnityEngine;

namespace S1API.Internal.Products
{
    /// <summary>
    /// INTERNAL: Activates a profile-supplied static visual without native-family casts.
    /// </summary>
#if IL2CPPMELON
    [RegisterTypeInIl2Cpp]
#endif
    internal sealed class StaticProductVisualsSetter : S1Product.ProductVisualsSetter
    {
#if IL2CPPMELON
        public StaticProductVisualsSetter(IntPtr pointer)
            : base(pointer)
        {
        }

        public StaticProductVisualsSetter()
            : base(ClassInjector.DerivedConstructorPointer<StaticProductVisualsSetter>())
        {
            ClassInjector.DerivedConstructorBody(this);
        }
#endif

        /// <inheritdoc />
        public override void ApplyVisuals(S1Product.ProductDefinition productDefinition)
        {
            if (VisualsContainer != null)
                VisualsContainer.gameObject.SetActive(true);
        }
    }
}

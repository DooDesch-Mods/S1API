# Custom Product Lifecycle Smoke

This opt-in smoke validates the internal mod-owned product lifecycle seam without
using the native family creation RPCs.

The probe:

1. creates a disposable save;
2. registers one cloned definition through `CustomProductDefinitionRegistry`;
3. repeats the same registration to prove idempotency;
4. assigns and saves a non-default product price;
5. returns to the menu and auto-loads the same save twice without registering again;
6. verifies pre-load item-registry restoration, one `AllProducts` entry, one product
   name, the restored price, and zero matching vanilla `createdProducts` entries.

Run it from the repository root:

```powershell
.\tests\Smoke\Run-CustomProductLifecycleSmoke.ps1 -Runtime MonoMelon
.\tests\Smoke\Run-CustomProductLifecycleSmoke.ps1 -Runtime Il2CppMelon
```

The runner creates an isolated linked install and writes results and logs beneath
its run-specific output directory. It does not modify the source game install or
any user save.

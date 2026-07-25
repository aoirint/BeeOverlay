# InputUtils Integration

## Target

- LethalCompany InputUtils v0.7.13
- NuGet package: `Rune580.Mods.LethalCompany.InputUtils` v0.7.13
- Package source commit:
  `a63a7ae46606440be47a68f255fa639a74f66a4e`
- NuGet package SHA-256:
  `0255E2BE2B4E48FB53F16EE796B3E3719F8D3BF3C6C35EDBE406C30FA9B57A65`
- License: LGPL-3.0-or-later
- BepInEx plugin GUID: `com.rune580.LethalCompanyInputUtils`
- Thunderstore dependency:
  `Rune580-LethalCompany_InputUtils-0.7.13`

## Evidence

- [InputUtils developer documentation](https://github.com/Rune580/LethalCompanyInputUtils)
- [InputUtils v0.7.13 on NuGet](https://www.nuget.org/packages/Rune580.Mods.LethalCompany.InputUtils/0.7.13)
- [CruiserJumpPractice InputUtils integration](https://github.com/aoirint/CruiserJumpPractice/tree/927683083657078ed8666b36fc581b9ce46eb94a/CruiserJumpPractice/Interop/InputUtils)

## Integration contract

InputUtils discovers actions declared by one instantiated `LcInputActions`
subclass. An `[InputAction]` property supplies the default keyboard binding and
the name displayed in the keybind menu. BeeOverlay polls the resulting
`InputAction.triggered` value during its existing HUD update and treats a
missing action object as not triggered.

BeeOverlay declares InputUtils as a hard BepInEx dependency because target
selection has no non-InputUtils input path. The project reference is
compile-only because the installed InputUtils mod supplies its assembly at
runtime; BeeOverlay's package does not redistribute InputUtils files. The
Thunderstore manifest declares the matching runtime package so mod-manager
installations receive it.

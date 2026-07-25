#nullable enable

extern alias LethalCompany;
extern alias LethalCompanyInputUtils;

using LethalCompany::UnityEngine.InputSystem;
using LethalCompanyInputUtils::LethalCompanyInputUtils.Api;
using LethalCompanyInputUtils::LethalCompanyInputUtils.BindingPathEnums;

namespace BeeOverlay.Interop.InputUtils;

/// <summary>
/// Declares the keybindings that InputUtils registers for BeeOverlay.
/// </summary>
internal sealed class InputUtilsActions : LcInputActions
{
    [InputAction(KeyboardControl.B, Name = "Select Next Bee")]
    public InputAction? CycleWorldGuideTargetKey { get; set; }
}

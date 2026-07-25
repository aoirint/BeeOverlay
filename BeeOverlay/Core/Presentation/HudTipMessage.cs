#nullable enable

namespace BeeOverlay.Core.Presentation;

/// <summary>
/// Defines game-styled, transient messages selected by Core before Interop displays them.
/// </summary>
internal sealed class HudTipMessage
{
    private const string DefaultHeaderText = "BeeOverlay";

    public static readonly HudTipMessage SelectNoBee = new(
        "select_no_bee",
        DefaultHeaderText,
        "No bee found to select."
    );

    public static readonly HudTipMessage SelectedBeeRemoved = new(
        "selected_bee_removed",
        DefaultHeaderText,
        "Selected bee was removed."
    );

    private HudTipMessage(string token, string headerText, string bodyText)
    {
        Token = token;
        HeaderText = headerText;
        BodyText = bodyText;
    }

    public string Token { get; }

    public string HeaderText { get; }

    public string BodyText { get; }
}

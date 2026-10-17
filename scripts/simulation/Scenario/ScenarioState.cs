namespace TiefBerg.Scenario;

// Szenariozustand, getrennt vom Weltzustand gespeichert (Abschnitt 11, 12): Die Welt kennt das Szenario nicht,
// daher kann ein Szenario gewechselt werden, ohne die Welt anzufassen.
// TODO: Tutorial-Fortschritt, Ziele, gescriptete Dialoge des Kobolds (Abschnitt 11)
public sealed record ScenarioState(string ScenarioId)
{
    // Szenario 1 (Abschnitt 11); bis zur Szenario-Auswahl das einzige
    public const string ExpeditionId = "expedition";
}

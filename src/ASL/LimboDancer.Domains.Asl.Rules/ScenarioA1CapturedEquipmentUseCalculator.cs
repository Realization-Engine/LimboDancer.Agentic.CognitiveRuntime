using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A25.75, A25.77 (p. 97), E1.76 (p. 225), RN1.12-.17: Finnish equipment-use exceptions.</summary>
public static class ScenarioA1CapturedEquipmentUseCalculator
{
    public static CapturedEquipmentUseVerdict Evaluate(CapturedEquipmentUseFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        DateOnly? date = null;
        if (facts.ScenarioDate != "not-applicable")
        {
            if (!DateOnly.TryParseExact(facts.ScenarioDate + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return Unknown(facts);
            }

            date = parsed;
        }

        if (facts.Personnel == false || facts.WeaponKind == "other" ||
            (facts.UserNationality is not null && facts.UserNationality is not ("finnish" or "russian" or "german")) ||
            (facts.UserNationality is "german" or "russian" && facts.WeaponNationality is not null && facts.WeaponNationality != "finnish"))
        {
            return NotApplicable(facts);
        }

        if (facts.UserNationality is null || facts.WeaponKind is null)
        {
            return Unknown(facts);
        }

        if (facts.WeaponKind is not ("lmg" or "mmg" or "hmg" or "hmg50" or "psk"))
        {
            return Unknown(facts);
        }

        if ((facts.UserNationality == "german" && facts.WeaponKind != "psk") ||
            (facts.UserNationality == "russian" && facts.WeaponKind == "psk") ||
            (facts.UserNationality == "finnish" && facts.WeaponKind != "psk" && facts.WeaponNationality is not null && facts.WeaponNationality != "russian"))
        {
            return NotApplicable(facts);
        }

        if (facts.Personnel is null)
        {
            return Unknown(facts);
        }

        if (facts.UserNationality == "finnish" && facts.WeaponKind == "psk")
        {
            if (date is null)
            {
                return Unknown(facts);
            }

            if (date < new DateOnly(1944, 7, 1) || date > new DateOnly(1945, 12, 1))
            {
                const string refusal = "Finnish Personnel may use PSK only in scenarios set in July 1944 through 1945 (A25.77).";
                return new("refused-use", null, false, facts.PhysicallyCaptured, refusal, refusal, "A25.77");
            }

            if (facts.WeaponNationality is not null && facts.WeaponNationality is not ("german" or "finnish"))
            {
                return Treatment(facts, true, "No Finnish PSK class has a Captured Use exemption for this weapon nationality (A25.75, A25.77).");
            }

            if (facts.PrintedClass is null)
            {
                return Unknown(facts);
            }

            if (facts.PrintedClass is "second-line" or "green" or "conscript")
            {
                return Treatment(facts, true, "Finnish 2nd-Line/Green/Conscript Personnel use every PSK with Captured Use penalties (A25.77).", "A25.77");
            }

            if (facts.PrintedClass is not ("elite" or "first-line" or "other"))
            {
                return Unknown(facts);
            }

            if (facts.WeaponNationality is null)
            {
                return Unknown(facts);
            }

            return Treatment(facts, facts.WeaponNationality is not ("german" or "finnish"), "Finnish PSK date/class restriction and reciprocal German-color exemption (A25.75, A25.77).");
        }

        if (facts.WeaponNationality is null)
        {
            return Unknown(facts);
        }

        if (facts.UserNationality == "german" && facts.WeaponKind == "psk" && facts.WeaponNationality == "finnish")
        {
            return Treatment(facts, false, "German Personnel use Finnish-color PSK without Captured Use penalties (A25.75).");
        }

        if (facts.WeaponKind != "psk" && facts.UserNationality == "finnish" && facts.WeaponNationality == "russian")
        {
            if (facts.WeaponKind == "lmg" && date is null)
            {
                return Unknown(facts);
            }

            var penalty = facts.WeaponKind == "hmg50" || (facts.WeaponKind == "lmg" && date!.Value.Year == 1939);
            return Treatment(facts, penalty, "Finnish Personnel use Russian MG without Captured Use or E1.76 penalties, except LMG in 1939 and .50-cal HMG (A25.75).");
        }

        if (facts.WeaponKind != "psk" && facts.UserNationality == "russian" && facts.WeaponNationality == "finnish")
        {
            if (facts.OriginMark is not ("r" or "none"))
            {
                return Unknown(facts);
            }

            return Treatment(facts, facts.OriginMark != "r", "The reciprocal MG exemption requires the Finnish MG's (r) mark (A25.75, E1.76).");
        }

        return NotApplicable(facts);
    }

    private static CapturedEquipmentUseVerdict Unknown(CapturedEquipmentUseFacts facts) =>
        new("not-evaluated", null, null, facts.PhysicallyCaptured, null, "A material equipment fact is unavailable or invalid (A25.75, A25.77).");

    private static CapturedEquipmentUseVerdict NotApplicable(CapturedEquipmentUseFacts facts) =>
        new("not-applicable", null, null, facts.PhysicallyCaptured, null, "No exception is decided here; ordinary captured use, qualification and repair rules remain separate (A25.75).");

    private static CapturedEquipmentUseVerdict Treatment(CapturedEquipmentUseFacts facts, bool penalties, string reason, string rule = "A25.75") =>
        new(penalties ? "captured-use" : "exempt", penalties, facts.WeaponKind != "psk" && penalties, facts.PhysicallyCaptured, null, reason, rule);
}

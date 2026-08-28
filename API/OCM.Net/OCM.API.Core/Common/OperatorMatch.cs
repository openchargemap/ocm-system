using OCM.API.Common.Model;

namespace OCM.API.Common
{
    /// <summary>
    /// How likely it is that an existing operator is the same as the one being submitted.
    /// </summary>
    public enum OperatorMatchType
    {
        /// <summary>
        /// An operator already uses this exact title (name plus country code). Titles are unique, so this cannot be overridden.
        /// </summary>
        DuplicateTitle,

        /// <summary>
        /// A similar operator already exists for the same country. Often a duplicate, but the editor can confirm it is a separate operator.
        /// </summary>
        PossibleDuplicate,

        /// <summary>
        /// The same operator appears to already be listed for another country. Expected for international networks, so this is advisory only.
        /// </summary>
        OtherCountry
    }

    /// <summary>
    /// An existing operator which may be the same as the details being submitted, with the reason it was matched.
    /// </summary>
    public class OperatorMatch
    {
        public OperatorInfo Operator { get; set; }

        public OperatorMatchType MatchType { get; set; }

        /// <summary>
        /// Why this operator was matched, e.g. "has a similar name and the same website (ionity.eu)".
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// True when the editor has to confirm this is a separate operator before the submission is accepted.
        /// </summary>
        public bool RequiresConfirmation => MatchType == OperatorMatchType.PossibleDuplicate;
    }
}

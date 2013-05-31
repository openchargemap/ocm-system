using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class OperatorInfo
    {
        public static Model.OperatorInfo FromDataModel(Core.Data.Operator source)
        {
            if (source == null) return null;

            return new Model.OperatorInfo
            {
                ID = source.ID,
                Title = source.Title,
                Comments = source.Comments,
                WebsiteURL = source.WebsiteURL,
                BookingURL = source.BookingURL,
                IsPrivateIndividual = source.IsPrivateIndividual,
                PhonePrimaryContact = source.PhonePrimaryContact,
                PhoneSecondaryContact = source.PhoneSecondaryContact,
                ContactEmail = source.ContactEmail,
                FaultReportEmail = source.FaultReportEmail,
                AddressInfo = (source!=null? AddressInfo.FromDataModel(source.AddressInfo): null),
                IsRestrictedEdit = source.IsRestrictedEdit
            };
        }
    }
}
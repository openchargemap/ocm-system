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
                ID = source.Id,
                Title = source.Title,
                Comments = source.Comments,
                WebsiteURL = source.WebsiteUrl,
                BookingURL = source.BookingUrl,
                IsPrivateIndividual = source.IsPrivateIndividual,
                PhonePrimaryContact = source.PhonePrimaryContact,
                PhoneSecondaryContact = source.PhoneSecondaryContact,
                ContactEmail = source.ContactEmail,
                FaultReportEmail = source.FaultReportEmail,
                AddressInfo = (source!=null? AddressInfo.FromDataModel(source.AddressInfo, true): null),
                IsRestrictedEdit = source.IsRestrictedEdit
            };
        }

        public static List<Model.OperatorInfo> FromDataModel(IEnumerable<Core.Data.Operator> source)
        {
            if (source == null) return null;
            var list = new List<Model.OperatorInfo>();
            foreach (var o in source)
            {
                list.Add(FromDataModel(o));
            }

            return list;
        }
    }
}
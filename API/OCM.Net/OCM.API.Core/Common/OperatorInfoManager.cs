using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.API.Common
{
    public class OperatorInfoManager : ManagerBase
    {
        public OperatorInfo GetOperatorInfo(int id)
        {
            var operatorInfo = DataModel.Operators.FirstOrDefault(o => o.ID == id);

            return Model.Extensions.OperatorInfo.FromDataModel(operatorInfo);
        }

        public OperatorInfo UpdateOperatorInfo(OperatorInfo update)
        {
            var operatorInfo = new OCM.Core.Data.Operator();
            if (update.ID > 1)
            {
                //existing operator
                operatorInfo = DataModel.Operators.FirstOrDefault(o => o.ID == update.ID);
            }


            operatorInfo.Title = update.Title;
            operatorInfo.WebsiteURL = update.WebsiteURL;
            operatorInfo.Comments = update.Comments;
            operatorInfo.PhonePrimaryContact = update.PhonePrimaryContact;
            operatorInfo.PhoneSecondaryContact = update.PhoneSecondaryContact;
            operatorInfo.IsPrivateIndividual = update.IsPrivateIndividual;
            operatorInfo.IsRestrictedEdit = update.IsRestrictedEdit;
            operatorInfo.BookingURL = update.BookingURL;
            operatorInfo.ContactEmail = update.ContactEmail;
            operatorInfo.FaultReportEmail = update.FaultReportEmail;

            if (operatorInfo.ID == 0)
            {
                //add new
                DataModel.Operators.Add(operatorInfo);
            }

            DataModel.SaveChanges();

            update = Model.Extensions.OperatorInfo.FromDataModel(operatorInfo);

            return update;

        }

        public List<OperatorInfo> GetOperators()
        {
            var operators = new List<Model.OperatorInfo>();
            foreach (var source in DataModel.Operators)
            {
                operators.Add(Model.Extensions.OperatorInfo.FromDataModel(source));
            }

            return operators;
        }
    }
}

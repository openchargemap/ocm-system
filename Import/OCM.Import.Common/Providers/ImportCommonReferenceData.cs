using OCM.API.Common.Model;
using System.Linq;

namespace OCM.Import.Providers
{
    public class ImportCommonReferenceData
    {
        public SubmissionStatusType SubmissionStatus_ImportedAndPublished { get; set; }
        public StatusType Status_Unknown { get; set; }
        public StatusType Status_Operational { get; set; }
        public UsageType UsageType_Public { get; set; }
        public UsageType UsageType_Private { get; set; }
        public UsageType UsageType_PrivateForStaffAndVisitors { get; set; }
        public OperatorInfo Operator_Unknown { get; set; }

        public ImportCommonReferenceData(CoreReferenceData coreRefData)
        {
            SubmissionStatus_ImportedAndPublished = coreRefData.SubmissionStatusTypes.First(s => s.ID == 100);//imported and published
            Status_Unknown = coreRefData.StatusTypes.First(os => os.ID == 0);
            Status_Operational = coreRefData.StatusTypes.First(os => os.ID == 50);

            UsageType_Public = coreRefData.UsageTypes.First(u => u.ID == 1);
            UsageType_Private = coreRefData.UsageTypes.First(u => u.ID == 2);
            UsageType_PrivateForStaffAndVisitors = coreRefData.UsageTypes.First(u => u.ID == 6);

            Operator_Unknown = coreRefData.Operators.First(opUnknown => opUnknown.ID == 1);
        }
    }
}

using System;

namespace OCM.API.Common
{
    public class ManagerBase : IDisposable
    {
        protected OCM.Core.Data.OCMEntities dataModel = null;

        public OCM.Core.Data.OCMEntities DataModel
        {
            get { return dataModel; }
        }

        public ManagerBase()
        {
            dataModel = new Core.Data.OCMEntities();
        }

        public void Dispose()
        {
            dataModel.Dispose();
        }

    }
}
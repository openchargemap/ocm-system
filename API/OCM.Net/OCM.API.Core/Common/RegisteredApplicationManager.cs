using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OCM.API.Common.Model;
using OCM.API.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OCM.API.Common
{
    public class RegisteredApplicationManager : ManagerBase
    {

        public Model.RegisteredApplication GetRegisteredApplication(int id, int? userId)
        {
            var app = dataModel.RegisteredApplications.FirstOrDefault(a => a.Id == id && (userId == null || userId != null && a.UserId == userId));
            return OCM.API.Common.Model.Extensions.RegisteredApplication.FromDataModel(app, true);
        }

        public async Task<PaginatedCollection<RegisteredApplication>> Search(string sortOrder, string keyword, int pageIndex = 1, int pageSize = 50, int? ownerId = null)
        {
            var list = new List<RegisteredApplication>();
            var appList = dataModel.RegisteredApplications.AsQueryable();

            if (ownerId != null)
            {
                appList = appList.Where(a => a.UserId == ownerId);
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                appList = appList.Where(u => u.Title.Contains(keyword) || u.Description.Contains(keyword));
            }

            if (!string.IsNullOrEmpty(sortOrder))
            {
                if (sortOrder == "datecreated_asc")
                {
                    appList = appList.OrderBy(u => u.DateCreated);
                }
                else if (sortOrder == "datecreated_desc")
                {
                    appList = appList.OrderBy(u => u.DateCreated);
                }
                else if (sortOrder == "datelastused_desc")
                {
                    appList = appList.OrderBy(u => u.DateApikeyLastUsed);
                }

            }
            else
            {
                appList = appList.OrderByDescending(u => u.Title);
            }

            var count = await appList.CountAsync();
            var items = await appList.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

            list.AddRange(Model.Extensions.RegisteredApplication.FromDataModel(items, true));

            return new PaginatedCollection<RegisteredApplication>(list, count, pageIndex, pageSize);
        }

        public List<RegisteredApplicationUser> GetUserAuthorizedApplications(int userId)
        {
            var list = dataModel.RegisteredApplicationUsers.Where(u => u.UserId == userId);
            return OCM.API.Common.Model.Extensions.RegisteredApplicationUser.FromDataModel(list);
        }

        public RegisteredApplication UpdateRegisteredApplication(RegisteredApplication update, int? userId)
        {
            Core.Data.RegisteredApplication item = new Core.Data.RegisteredApplication();

            if (update.ID > 0)
            {
                item = dataModel.RegisteredApplications.FirstOrDefault(a => a.Id == update.ID &&( userId == null || (userId!=null && update.UserID==userId)));
 
            } else
            {
                item.DateCreated = DateTime.UtcNow;
                item.IsEnabled = true;
                item.PrimaryApikey = Guid.NewGuid().ToString().ToLower();
                item.AppId = Guid.NewGuid().ToString().ToLower();
                item.SharedSecret = Guid.NewGuid().ToString().ToLower();
                item.UserId = (int)userId;
            }

            item.Title = update.Title;
            item.WebsiteUrl = update.WebsiteURL;
            item.Description = update.Description;
            item.IsPublicListing = update.IsPublicListing;

            if (userId == null)
            {
                item.IsEnabled = update.IsEnabled;
                item.IsWriteEnabled = update.IsWriteEnabled;
            }

            if (item.Id == 0)
            {
                dataModel.RegisteredApplications.Add(item);
            }

            dataModel.SaveChanges();

            return OCM.API.Common.Model.Extensions.RegisteredApplication.FromDataModel(item);
        }
    }
}